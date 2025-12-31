# Jellydash Plugin

This folder contains the Jellydash server-side plugin for Jellyfin.

The goal of this plugin is to provide Jellydash with:

- A reliable history of what users actually watched (spans with start/end positions, not just “played” flags).
- Additional technical context per span (transcoding, bitrate, client/device) so the Jellydash UI can mirror the live `CurrentActivityCard` view for historical items.
- Plugin-only HTTP endpoints that expose this history with a Jellydash-friendly shape.

Jellyfin itself does not keep rich, queryable “watch span” history for all sessions. This plugin hooks into Jellyfin’s event system and maintains its own lightweight history store.

## To run locally

### Prerequisites

- .NET SDK 9.x installed (for building Jellyfin and the plugin).
- FFmpeg installed and available on your PATH (required by Jellyfin).
- The .NET portable Jellyfin application downloaded from [jellyfin.org](https://jellyfin.org/downloads/dotnet)
- [VS Code with the C# (.NET) extension](https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.csharp).

### VS Code setup

This repo includes a VS Code setup to build, deploy, and debug the plugin against a local Jellyfin checkout.

Configuration lives in `.vscode/settings.json` and `.vscode/tasks.json`:

- Settings:
	- `jellyfinDir`: path to the Jellyfin server checkout (defaults to `${workspaceFolder}/jellyfin`).
	- `jellyfinWindowsDataDir` / `jellyfinLinuxDataDir` / `jellyfinOsxDataDir`: Jellyfin data directories where plugins are loaded from.
	- `pluginName`: should remain `Jellyfin.Plugin.Jellydash`.

- Tasks:
	- `build-and-copy` (chain task):
		- Runs `clean` → `build` → `make-plugin-dir` → `copy-dll`.
		- `build` runs `dotnet publish` on `plugin/${pluginName}.sln` in Debug.
		- `copy-dll` copies the published plugin files into the configured Jellyfin data `plugins/${pluginName}` folder for your platform.

### Debugging Jellydash with Jellyfin

1. Ensure the portable Jellyfin application is present in `jellyfin/` in the root of the repo
2. Open this repo in VS Code.
3. Ensure `.vscode/settings.json` points to your Jellyfin checkout and data directory.
4. Use the `Launch` debug configuration from `.vscode/launch.json`:
	 - This starts `jellyfin.dll` from `${config:jellyfinDir}` under the debugger.
5. Once Jellyfin is running, log into the [web UI](http://localhost:8096/web/) and verify that:
	 - The "Jellydash" plugin is enabled under **Dashboard → Plugins**.
	 - The Jellydash configuration page is available and the `/Jellydash/ping` and `/Jellydash/history` endpoints respond.

The Flutter dashboard can then be pointed at this local Jellyfin instance (including the Jellydash plugin endpoints) for end‑to‑end development.

## Running tests

To run the plugin unit tests, run:

```bash
dotnet test Jellyfin.Plugin.Jellydash.sln
```

This will build the plugin and execute the `Jellyfin.Plugin.Jellydash.Tests` xUnit test project.

## Architecture Overview

High‑level pieces:

- **History models**
	- `Models/HistoryEntry.cs` defines a single contiguous playback or download span for a user and item.
	- Fields are chosen to map cleanly onto the Flutter `Session` / `CurrentActivityCard` UI: user + avatar, item title/series/season/episode/year, client + device, span start/end, positions and percentages, bitrate, and transcoding flags/codecs.

- **Event consumers (playback tracking)**
	- `Events/PlaybackHistoryLogger.cs` implements:
		- `IEventConsumer<PlaybackStartEventArgs>`
		- `IEventConsumer<PlaybackStopEventArgs>`
	- On **playback start**:
		- Filters to supported item types (`Movie`, `Episode`).
		- Extracts `BaseItemDto` + `PlaybackStartEventArgs` data (user, item, series/season/episode, runtime ticks, starting position, client, device, and transcoding info from the session).
		- Creates a transient in‑memory `HistorySeed` keyed by play session (playSessionId if available; otherwise `sessionId:itemId`).
	- On **playback stop**:
		- Looks up and clears the matching `HistorySeed` (or synthesizes one if start was missed).
		- Uses the stop position and runtime ticks to compute `StartPositionTicks`, `EndPositionTicks`, `StartPercentage`, and `EndPercentage`.
		- Builds a `HistoryEntry` that includes all UI‑relevant fields plus bitrate/direct‑stream/transcode details.
		- Persists the span through the history repository (see below).

‑ **History storage**
	- `Services/HistoryRepository.cs` is a SQLite‑backed store for `HistoryEntry` objects.
	- Storage format:
		- Dedicated database file `jellydash.db` under the Jellyfin data path: `Data/plugins/Jellydash/jellydash.db`.
		- Schema is managed via versioned SQL migration scripts in `Migrations/` (e.g. `001_Initial.sql`).
	- Key operations:
		- `AppendAsync(HistoryEntry)` — append a new span (used by `PlaybackHistoryLogger`).
		- `GetPageAsync(int limit, long? beforeId, DateTime? beforeEndUtc, CancellationToken)` — paged, most‑recent‑first query used by `/Jellydash/history`.
		- `GetRecentAsync(DateTime cutoffUtc)` — convenience query for entries since a given time.
		- `DeleteOlderThanAsync(DateTime cutoffUtc)` — delete rows older than the cutoff (used by the scheduled cleanup task).
	- Concurrency:
		- Uses a static `SemaphoreSlim` guard to ensure DB access is serialized across threads.
	- Migrations:
		- On startup, reads `PRAGMA user_version` from `jellydash.db` and applies any `.sql` migration scripts with a higher version number, in order, updating `user_version` after each script.

- **Service registration / event wiring**
	- `Services/JellydashServiceRegistrator.cs` implements `MediaBrowser.Controller.Plugins.IPluginServiceRegistrator`.
	- In `RegisterServices` the plugin registers:
		- `IEventConsumer<PlaybackStartEventArgs>` → `PlaybackHistoryLogger`
		- `IEventConsumer<PlaybackStopEventArgs>` → `PlaybackHistoryLogger`
	- Jellyfin discovers `IPluginServiceRegistrator` implementations in plugin assemblies at startup and calls `RegisterServices`, so no manual server configuration is required. Once registered, Jellyfin’s `EventManager` will resolve and call `PlaybackHistoryLogger` whenever playback starts or stops.

- **Plugin configuration / retention**
	- `PluginConfiguration` includes settings such as:
		- `HistoryRetentionDays` — how many days of history to keep when retention is enabled (default: 30).
		- `EnableRetention` — whether automatic cleanup is enabled; when disabled, history is not pruned.
		- `TrackDownloads` — whether download activity is tracked alongside playback.
	- The configuration UI (`Configuration/configPage.html`) exposes these settings:
		- "Enable retention period" checkbox that also enables/disables the days input.
		- A numeric "History retention (days)" field.
		- A "Track download activity" checkbox.

- **HTTP endpoints**
	- `Controllers/JellydashController.cs` exposes:
		- `GET /Jellydash/ping` — simple health‑check endpoint returning `"pong"`.
		- `GET /Jellydash/history` — returns a page of recent `HistoryEntry` objects using cursor‑based pagination:
			- Query parameters: `limit` (max 100, default 20) and optional `cursor`.
			- Response shape: `{ "items": HistoryEntry[], "nextCursor": string | null }`.
			- Cursors are opaque Base64 tokens derived from the last entry's `EndUtc` and database id.
	- Responses return the raw `HistoryEntry` model; the Flutter client is responsible for shaping this into its own `Session`/card models.

- **Scheduled cleanup**
	- `ScheduledTasks/JellydashHistoryCleanupTask.cs` implements `IScheduledTask` and:
		- Runs daily by default around 03:00 server time (via `TaskTriggerInfo`).
		- Checks `EnableRetention` and exits early when retention is disabled.
		- Uses `HistoryRetentionDays` to compute `cutoffUtc` and calls `HistoryRepository.DeleteOlderThanAsync` to prune old rows from `jellydash.db`.
	- This keeps the history table from growing without bound while respecting the configured retention policy (or leaving all history intact when retention is disabled).

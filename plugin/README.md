# Jellydash Plugin

This folder contains the Jellydash server-side plugin for Jellyfin.

The goal of this plugin is to provide Jellydash with:

- A reliable history of what users actually watched (playback spans with start/end positions, not just “played” flags”).
- Additional technical context per span (transcoding, bitrate, client/device) so the Jellydash UI can mirror the live `CurrentActivityCard` view for historical items.
- Plugin-only HTTP endpoints that expose this history with a Jellydash-friendly shape.

Jellyfin itself does not keep rich, queryable “watch span” history for all sessions. This plugin hooks into Jellyfin’s event system and maintains its own lightweight history store, optimized for the Jellydash Flutter dashboard.

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
	 - The Jellydash configuration page is available and the `/Jellydash/activity` endpoint responds.

The Flutter dashboard can then be pointed at this local Jellyfin instance (including the Jellydash plugin endpoints) for end‑to‑end development.

## Running tests

To run the plugin unit tests, run:

```bash
dotnet test Jellyfin.Plugin.Jellydash.sln
```

This will build the plugin and execute the `Jellyfin.Plugin.Jellydash.Tests` xUnit test project.

## Architecture Overview

High‑level pieces:

- **Playback history model**
	- `Models/PlaybackEntry.cs` defines a single contiguous playback span for a user and item as it is stored in SQLite.
	- Each span is keyed by a generated `PlaybackId`, which is a GUID derived from the Jellyfin session ID, playlist item ID, and item ID. Doesn't ensure uniqueness, but is the best available. Once a PlaybackEntry is marked as completed it will no longer be updated to minimise the impact of the non-uniqueness.
	- `Models/PlaybackEntryDto.cs` and related DTOs (for identity, user, client, timing, streams, and transcoding) define the API shape returned to the Jellydash UI.
	- Fields are chosen to map cleanly onto the Flutter `Session` / `CurrentActivityCard` UI: user id + name, item title/series/season/episode/year, client + device, span start/end, positions and percentages, bitrate, and detailed stream/transcoding information.
	- Timing and completion semantics are handled in the model and DTOs, with percentage calculations done in the DTO layer (not stored in the DB). Completion is determined using a 95% watched threshold.
	- Dapper type handlers for `Guid` and `Collection<string>` are registered to ensure correct mapping between C# models and the SQLite schema.

- **Event consumers (playback tracking)**
	- `Events/PlaybackTracker.cs` implements:
		- `IEventConsumer<PlaybackStartEventArgs>`
		- `IEventConsumer<PlaybackProgressEventArgs>`
		- `IEventConsumer<PlaybackStopEventArgs>`
	- On **playback start**:
		- Filters to supported item types (`Movie`, `Episode`).
		- Generates a non-unique `PlaybackId` from the session ID, playlist item ID, and item ID.
		- Builds and persists a new `PlaybackEntry` for the session, marking it as in-progress.
	- On **playback progress**:
		- Looks up the existing `PlaybackEntry` by `PlaybackId` or creates a new one if the start event was missed.
		- Updates the entry with the latest position and persists it.
	- On **playback stop**:
		- Looks up the existing `PlaybackEntry` by `PlaybackId` or creates a new one if needed.
		- Marks the entry as completed and persists it.
	- All playback tracking state is now persisted in SQLite; there is no in-memory session state.

- **Playback storage**
	- `Services/PlaybackEntryRepository.cs` is a SQLite‑backed store for `PlaybackEntry` records.
	- Storage format:
		- Dedicated database file `jellydash.db` under the Jellyfin data path, in `plugins/Jellydash/jellydash.db`.
		- Schema is managed via versioned SQL migration scripts in `Migrations/` (e.g. `001_Initial.sql`), with a single `PlaybackEntries` table.
	- Key operations:
		- `AppendAsync(PlaybackEntry)` — simple append helper (used primarily in tests).
		- `Upsert(PlaybackEntry)` — insert or update a row keyed by `PlaybackId`.
		- `GetRecentlyIncompletedByPlaybackIdAsync(Guid playbackId, ...)` — lookup the current in-progress row for a given playback instance.
		- `GetRecentlyCompletedByPlaybackIdAsync(Guid playbackId, ...)` — lookup the completed row for a given playback instance.
		- `GetPageAsync(int limit, int? beforeId, DateTime? beforeEndUtc, CancellationToken)` — paged, most‑recent‑first query used by the `/Jellydash/history` endpoint. This now filters to `IsCompleted = 1` so only completed spans appear in history.
		- `DeleteOlderThanAsync(DateTime cutoffUtc, CancellationToken)` — delete rows with `EndUtc < cutoffUtc` (completed or not), used by the scheduled cleanup task.
	- Concurrency:
		- Uses a static `SemaphoreSlim` guard to ensure DB access is serialized across threads.
	- Migrations:
		- On startup, reads `PRAGMA user_version` from `jellydash.db` and applies any `.sql` migration scripts with a higher version number, in order, updating `user_version` after each script.
	- Dapper type handlers for `Guid` and `Collection<string>` are registered at startup to ensure correct mapping between C# and SQLite.

- **Service registration / event wiring**
	- `Services/JellydashServiceRegistrator.cs` implements `MediaBrowser.Controller.Plugins.IPluginServiceRegistrator`.
	- In `RegisterServices` the plugin registers:
		- `IEventConsumer<PlaybackStartEventArgs>` → `ActivityTracker`
		- `IEventConsumer<PlaybackStopEventArgs>` → `ActivityTracker`
	- Jellyfin discovers `IPluginServiceRegistrator` implementations in plugin assemblies at startup and calls `RegisterServices`, so no manual server configuration is required. Once registered, Jellyfin’s `EventManager` will resolve and call `ActivityTracker` whenever playback starts or stops.

- **Plugin configuration / retention**
	- `PluginConfiguration` includes settings such as:
		- `RetentionDays` — how many days of playback history to keep when retention is enabled (default: 30).
		- `EnableRetention` — whether automatic cleanup is enabled; when disabled, playback history is not pruned.
	- The configuration UI (`Configuration/configPage.html`) exposes these settings:
		- "Enable retention period" checkbox that also enables/disables the days input.
		- A numeric "History retention (days)" field.

- **HTTP endpoints**
	- `Controllers/JellydashController.cs` exposes:
		- `GET /Jellydash/history` — returns a page of recent playback history entries using cursor‑based pagination:
			- Query parameters: `limit` (max 100, default 20) and optional `cursor`.
			- Response shape: `{ "items": PlaybackEntryDto[], "nextCursor": string | null }`.
			- Cursors are opaque Base64 tokens derived from the last entry's `EndUtc` and database id.
			- Only completed spans are included (`IsCompleted = true`); ongoing entries for active sessions are intentionally excluded from this endpoint and should be accessed via dedicated APIs that use `GetOngoingAsync`.
	- Responses return the Jellydash‑specific `PlaybackEntryDto` model; the Flutter client can use this directly to drive its `Session`/card models.

- **Scheduled cleanup**
	- `ScheduledTasks/JellydashCleanupTask.cs` implements `IScheduledTask` and:
		- Runs daily by default around 03:00 server time (via `TaskTriggerInfo`).
		- Checks `EnableRetention` and exits early when retention is disabled.
		- Uses `RetentionDays` to compute `cutoffUtc` and calls `PlaybackEntryRepository.DeleteOlderThanAsync` to prune old rows from `jellydash.db`.
	- This keeps the playback history table from growing without bound while respecting the configured retention policy (or leaving all history intact when retention is disabled).

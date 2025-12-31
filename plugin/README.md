# Jellydash Plugin

This folder contains the Jellydash server-side plugin for Jellyfin.

The goal of this plugin is to provide Jellydash with:

- A reliable history of what users actually watched (spans with start/end positions, not just “played” flags).
- Additional technical context per span (transcoding, bitrate, client/device) so the Jellydash UI can mirror the live `CurrentActivityCard` view for historical items.
- Plugin-only HTTP endpoints that expose this history with a Jellydash-friendly shape.

Jellyfin itself does not keep rich, queryable “watch span” history for all sessions. This plugin hooks into Jellyfin’s event system and maintains its own lightweight history store.

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
		- `GetRecentAsync(DateTime cutoffUtc)` — query all entries with `EndUtc >= cutoffUtc` (used by `/Jellydash/history`).
		- `DeleteOlderThanAsync(DateTime cutoffUtc)` — delete rows older than the cutoff (intended for the scheduled cleanup task).
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
		- History retention window (e.g. keep the last N days of spans).
		- Feature toggles for which activities are tracked (e.g. playback vs download).
	- The planned scheduled task (see below) will read these settings to decide what to prune.

- **HTTP endpoints**
	- `Controllers/JellydashController.cs` exposes:
		- `GET /Jellydash/ping` — simple health‑check endpoint returning `"pong"`.
		- `GET /Jellydash/history` — returns recent `HistoryEntry` objects for the configured retention window, using `HistoryRepository.GetRecentAsync` with a cutoff computed from `PluginConfiguration.HistoryRetentionValue`/`HistoryRetentionUnit`.
	- Responses return the raw `HistoryEntry` model; the Flutter client is responsible for shaping this into its own `Session`/card models.

- **Planned scheduled cleanup** (not yet implemented)
	- An `IScheduledTask` will:
		- Run periodically (e.g. daily).
		- Read the plugin configuration retention window.
		- Call `HistoryRepository.DeleteOlderThanAsync` with the calculated cutoff.
	- This keeps `history.jsonl` from growing without bound.

- **Flutter client integration (planned)**
	- The Flutter app will add a Jellydash‑plugin API service that:
		- Calls `/Jellydash/History`.
		- Maps the response into a `Session`‑like Dart model used by widgets in `lib/widgets/current_activity_card.dart` and new “recent history” widgets.
	- The goal is to allow the history UI to reuse most of the semantics and styling from the existing `CurrentActivityCard` (title/subtitle, remaining time, bitrate, transcoding badges, user avatar, etc.).

## To run locally

FFmpeg installed
Portable .NET installation for Jellyfin in the root of repository

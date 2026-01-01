# Jellydash

Jellydash is a lightweight dashboard for [Jellyfin](https://jellyfin.org/) that shows a
real‑time view of who is watching what on your server. It is designed to be a quick,
glanceable overview of current and recent activity, suitable for always‑on displays,
desktop, tablet, and mobile.

## Features

- Live list of active Jellyfin sessions with per‑user cards.
- Shows title, season/episode or year, remaining time, and estimated completion time.
- Visual indicators for transcoding (separate video/audio badges) and stream bitrate.
- User avatar circle with fallback to user initial when no image is available.
- Responsive layout that adapts card columns to available width.
- Light and dark themes using standard Flutter theming.

## How It Works

Jellydash talks directly to your Jellyfin server using its public HTTP API. The Flutter
app fetches session information from Jellyfin, maps it into `Session` models, and
renders them using widgets such as `CurrentActivities` and `CurrentActivityCard`.
For richer history data, Jellydash can use the bundled Jellydash Jellyfin plugin,
which exposes a `/Jellydash/history` endpoint with cursor‑based pagination that returns
playback history entries in a Jellydash‑specific DTO shape.

## Prerequisites

To build and run Jellydash you need:

- A running Jellyfin server you can reach over HTTP/HTTPS.
- Flutter SDK (3.x or newer) installed and on your `PATH`.
- A device or emulator (web, mobile, or desktop) supported by Flutter.
- (Optional, recommended) The Jellydash Jellyfin plugin built and deployed to your
	 Jellyfin server so the dashboard can query `/Jellydash/history` for historical
	 playback entries.

## Getting Started

1. **Clone the repository**

	```bash
	git clone https://github.com/johman10/jellydash.git
	cd jellydash
	```

2. **Fetch dependencies**

	```bash
	flutter pub get
	```

3. **Run the app** (choose one target):

	```bash
	# Web
	flutter run -d chrome

	# MacOS / Windows / Linux (desktop)
	flutter run -d macos

	# Mobile
	flutter run
	```

4. **Configure Jellyfin connection**

	- Open the **Settings** screen from the app drawer.
	- Enter your Jellyfin base URL (for example, `https://jellyfin.example.com`).
	- Provide any required API key or authentication details as prompted.
	- Save settings; the dashboard will begin loading active sessions.

## Optional .env configuration

To help with faster debugging Jellydash also support build time environment variables. It should be located in `assets/env/.env`. If the file is found it will use the values from the file, if not it will aim for sensible defaults and requires manual configuration from the user.

## Development

- Run all tests:

  ```bash
  flutter test
  ```

- Run the Jellydash Jellyfin plugin unit tests (from the `plugin/` folder):

	```bash
	dotnet test Jellyfin.Plugin.Jellydash.sln
	```

- Key widgets to be aware of:
  - `lib/widgets/current_activities.dart` – responsive layout of activity cards.
  - `lib/widgets/current_activity_card.dart` – individual session card UI.

When adding new features, prefer small, focused widgets and add matching tests in the
`test/widgets` directory to cover layout and key text/indicator behaviour.

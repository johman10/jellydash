---
applyTo: "**"
---

# Jellydash AI Coding Agent Instructions

## Project Overview
Jellydash is a dashboard for [Jellyfin](https://jellyfin.org/), providing a real-time view of current and recent activity via direct API integration. The project is designed for quick insights into media usage.

## Architecture & Key Patterns
- **Single-Page Dashboard**: The app is structured as a dashboard UI, focused on displaying live and recent Jellyfin activity.
- **Direct Jellyfin API Integration**: All data is fetched directly from the Jellyfin API. No intermediate backend is present.
- **Platform Target**: Intended for web, mobile, and desktop (use Flutter for new code; follow Dart conventions).
- **Responsiveness**: UI must adapt to various screen sizes and support both light and dark modes.
 - **Activity Cards**: Current activity is rendered via `CurrentActivityCard` inside `CurrentActivities` using responsive Wrap-based columns (no implicit scrolling in these widgets; scrolling is handled by higher-level layouts).
 - **Session Semantics**: Session UI should respect `Session` fields (e.g., `season`/`episode` vs `year`, `isPaused`, `progress`/`duration`, `bitrate`) and avoid showing placeholder or "null"-like values to the user.

## Conventions & Workflows
- **Naming**: Use common flutter conventions (e.g., PascalCase for classes, camelCase for variables).
- **Error Handling**: Ensure to show user-friendly messages for any failure that's relevant for the user.
- **API Usage**: Always use the Jellyfin API directly.
- **UI**: Use Flutter built-in widgets and follow accessibility (WCAG 2.1) guidelines. Ensure touch and keyboard usability. Ensure dark modes are available for the user to select.
- **Testing**: Write unit tests for new features. Use Flutter's test framework.
	- Prefer focused widget tests for dashboard widgets (e.g., `CurrentActivityCard`, `CurrentActivities`) that verify layout decisions, overflow behavior, and key text/indicator content.
	- When a test fails due to artificial layout constraints, prefer adjusting the test's surface size rather than adding scroll containers or otherwise changing production layout solely to satisfy the test.
- **Containerization**: If updating dependencies or adding system requirements, update the Dockerfile.
- **Coding style**: Optimize for readability and maintainability over cleverness. Keep functions small and focused on a single task. Use descriptive names for variables and functions. Avoid deep nesting by breaking code into smaller functions.

## Key Files & Directories
- `.github/copilot-instructions.md`: AI agent instructions (this file)
- `README.md`: Project purpose, high-level usage, and Jellyfin integration notes

## Examples
- To add a new dashboard widget, create a Flutter widget in PascalCase, fetch data from the Jellyfin API, and document the endpoint in a comment.
- When handling API errors, log the error and display a concise message to the user.
 - When updating activity-related UI (e.g., remaining time, pause overlays, bitrate display), keep `Session` semantics consistent and update the corresponding tests in `test/widgets/current_activity_card_test.dart` and `test/widgets/current_activities_test.dart`.

## Notes
- No backend server: all logic is client-side.
- Keep instructions concise and actionable for future maintainers and AI agents.

# US-017: Settings, status, and diagnostics UI

## User story

**As a** self-hosting administrator or profile owner, **I want** settings, status, and diagnostics separated from the daily scan flow, **so that** routine scanning stays simple while configuration and troubleshooting remain discoverable.

## Acceptance criteria

- Settings provides grouped entry points for Profile, Scanner defaults, Paperless, Notifications, and Advanced options.
- Scanner defaults allow selecting default scanner, source, color mode, and resolution, validate choices against known or freshly loaded capabilities before saving, and allow reset to factory defaults.
- Paperless defaults show the effective configuration source, support connection checks and metadata loading, and require unavailable metadata defaults to be corrected before saving.
- Profile and authentication settings surface the configured profile mode, explain the anonymous shared-profile trade-off, and provide sign-in, sign-out, or migration actions required by the active mode.
- Profile-stored Paperless tokens are represented as saved secrets that can be replaced or deleted but not revealed, while deployment-provided settings are clearly marked as administrator-managed and not persisted to the profile.
- Notification settings show whether browser notifications are unavailable, denied, disabled, or active for the current tab, and explain the HTTPS/localhost and open-tab limitations.
- Status shows application version or commit, health, SQLite, temporary storage, Data Protection keys, selected scanner, Paperless configuration presence, last successful checks when available, and deployment hints without leaking API tokens, document contents, private metadata, or unnecessary filenames.
- User-facing errors across Settings and Status include a copyable diagnostic ID when available so administrators can correlate UI reports with container logs.

## UI concept references

- Settings overview and grouped settings areas.
- Scanner and Paperless defaults screens.
- Profile mode, anonymous mode, authenticated mode, and migration concepts.
- Per-profile Paperless configuration and token replacement.
- Notification states.
- Status and diagnosis area with safe diagnostic IDs.

## Out of scope

- Implementing a full log viewer, secret manager, or Paperless administration UI.
- Managing users, roles, or Paperless accounts beyond Scan Bridge profile configuration.

## Dependencies

- US-008
- US-009
- US-010
- US-011
- US-012
- US-013

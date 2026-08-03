# US-012: Per-profile service configuration

## User story

**As an** authenticated user, **I want** to maintain my Paperless-ngx connection and personal defaults in my profile, **so that** I do not have to provide account configuration whenever the container starts.

## Acceptance criteria

- Each authenticated profile can store its own Paperless base URL and API token as well as scan and upload defaults.
- API tokens are encrypted at rest with persisted ASP.NET Core Data Protection keys, are never returned to the browser after saving, and can be replaced or deleted explicitly.
- A settings screen validates URL policy, connectivity, authentication, permissions, and metadata choices before activating configuration.
- Profile configuration takes precedence over optional deployment-wide fallback configuration, with the effective source clearly shown.
- Scans, uploads, metadata caches, logs, downloads, and temporary documents are isolated by authenticated profile.
- Token rotation, invalid credentials, inaccessible Paperless instances, account deletion, database migration, backup, and restore have documented recovery behavior.
- Automated tests prove that one user cannot read, use, overwrite, infer, or download another user's configuration or documents.

## Out of scope

- Sharing Paperless tokens between users, synchronizing profiles between Scan Bridge installations, and managing Paperless users.

## Dependencies

- US-011

# US-012: Per-profile service configuration

## User story

**As a** profile owner or single-profile deployer, **I want** Paperless-ngx connection settings to come from either my profile or deployment configuration, **so that** shared homes can use separate API tokens while single-user homes can run anonymously with Compose-provided credentials.

## Acceptance criteria

- Each authenticated profile can store its own Paperless base URL and API token as well as scan and upload defaults; anonymous mode has exactly one anonymous profile whose preferences can use deployment-provided Paperless credentials.
- Profile-stored API tokens are encrypted at rest with persisted ASP.NET Core Data Protection keys, are never returned to the browser after saving, and can be replaced or deleted explicitly. Deployment-provided anonymous or fallback tokens are read from configuration at startup and are never persisted back to SQLite.
- A settings screen validates URL policy, connectivity, authentication, permissions, and metadata choices before activating configuration.
- Profile configuration takes precedence over optional deployment-wide fallback configuration, with the effective source clearly shown. Deployers can preconfigure a default Paperless URL, let profiles override that URL when permitted, and choose whether anonymous mode uses the configured token without prompting for login.
- Scans, uploads, metadata caches, logs, downloads, and temporary documents are isolated by authenticated profile; anonymous mode keeps the same isolation boundary around the single shared anonymous profile and documents that there is no per-person separation.
- Token rotation, invalid credentials, inaccessible Paperless instances, account deletion, database migration, backup, and restore have documented recovery behavior.
- Automated tests prove that one authenticated user cannot read, use, overwrite, infer, or download another user's configuration or documents, and that anonymous mode cannot accidentally mix anonymous state with authenticated profiles when modes are changed.

## Out of scope

- Sharing profile-stored Paperless tokens between authenticated users, synchronizing profiles between Scan Bridge installations, managing Paperless users, and supporting more than one anonymous profile per deployment.

## Dependencies

- US-011

## Completion evidence

- Profile URL/token precedence, validation, explicit replacement/deletion, and non-disclosure are covered by `ProfileServiceConfigurationTests` and the settings component.
- SQLite/Data Protection integration proves encrypted-at-rest, profile-isolated tokens and persisted keys; session ownership guards document routes across profiles.
- Deployment fallbacks remain options-only, anonymous mode resolves one stable anonymous profile with read-only deployment-provided Paperless values, and operational recovery is documented in the README.
- Representative browser behavior is implemented mobile-first with Bootstrap controls. No scanner hardware behavior changed, so HP hardware verification is not applicable.

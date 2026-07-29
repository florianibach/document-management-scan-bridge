# US-008: Reuse profiles and defaults

## User story

**As a** repeat user, **I want** my common scan and upload settings remembered, **so that** routine documents require fewer selections.

## Acceptance criteria

- A local user profile can store supported scan settings and upload metadata defaults in SQLite.
- The user can view, change, and reset defaults from a mobile-friendly settings screen.
- A new scan session receives a snapshot of defaults and does not change when the profile is edited later.
- Invalid or no-longer-supported scanner and Paperless-ngx choices are identified and require correction rather than being used silently.
- Database initialization and migrations are reproducible and preserve existing valid settings.
- Persistence behavior is integration-tested, including restart, update, reset, and migration scenarios.

## Out of scope

- Multi-user identity, tenant isolation, external identity providers, and cross-device profile synchronization.

## Dependencies

- US-002 and US-007

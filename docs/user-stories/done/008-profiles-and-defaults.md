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

## Completion evidence

- SQLite migration `20260803000000_AddProfileDefaults` introduces the single local profile without changing existing scanner rows; restart, update, reset, and migration behavior are integration-tested.
- The application profile service validates scanner existence, source, resolution, and supported scan values before persistence; unit tests cover stale choices.
- The mobile-first `/settings` screen edits and resets scan and Paperless metadata defaults and checks current Paperless choices before saving.
- The scan page loads a validated snapshot once when its browser circuit starts. Later profile edits therefore affect only new scan sessions.
- Login, multiple users, external identity providers, profile synchronization, and per-user Paperless secrets remain explicitly deferred to US-011 and US-012.

## Definition of Done record

- Acceptance criteria, validation, persistence recovery, focused test coverage, documentation, build and operational checks are recorded in the pull request.
- Cancellation and retry behavior are unchanged because profile reads and SQLite writes are short request-scoped operations; validation failures retain the previous valid row.
- Scanner hardware behavior, PDF generation, temporary-document cleanup, and Paperless upload transport are unaffected, so no new target-printer verification is required.
- Review found no credentials, document content, disabled tests, binary artifacts, or new third-party dependencies.

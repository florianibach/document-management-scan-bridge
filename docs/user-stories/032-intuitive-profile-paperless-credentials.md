# US-032: Intuitive profile Paperless credentials

## User story

**As an** authenticated profile owner, **I want** understandable Paperless URL and token controls with explicit administrator-managed fallback behavior, **so that** I can manage my own credentials without ambiguous checkboxes or accidental exposure of a shared deployment token.

## Acceptance criteria

- An authenticated profile's own Paperless token remains encrypted in SQLite with ASP.NET Core Data Protection. On opening the protected Paperless settings page, the server decrypts that token for the authenticated profile owner and loads it into a password input that supports an explicit Show/Hide action.
- Authorization and isolation ensure that only the owning authenticated profile can retrieve, view, validate, replace, or remove its token. The token is never placed in a URL, log, diagnostic payload, browser storage, or ordinary text input.
- The settings UI and README clearly warn that anyone with access to the profile owner's active signed-in browser session can use Show to view the profile token. The UI replaces `Replace token with the value above`, `Delete stored profile token`, and `Use deployment token as fallback` with understandable actions for saving/checking an edited profile token and removing the stored profile token.
- The deployment token is never displayed or otherwise delivered to the browser, never copied into SQLite, and is not implied to be the profile owner's token. UI state may describe whether an administrator-managed identity is available without exposing its secret.
- If an authenticated profile has no saved Paperless URL, the form is initially populated from `PAPERLESS_URL`. When profile URL overrides are allowed, the user can edit and save that value as a profile URL; when they are not allowed, the deployment URL is read-only and clearly identified as administrator-managed deployment configuration.
- A typed, Compose-exposed deployment option controls whether authenticated profiles without their own token may use `PAPERLESS_TOKEN`. Its secure default is disabled, and individual profile owners cannot enable the fallback from the UI.
- When authenticated-profile fallback is enabled, the UI describes it as a shared, administrator-managed Paperless identity rather than as anonymous access. It explains that uploads use the same Paperless account and that Paperless assignment and visibility depend on that shared account.
- Effective token priority is consistent for connection checks, metadata retrieval, and uploads: the authenticated profile's token first, then the deployment token only when authenticated-profile fallback is enabled. Anonymous mode continues to use its shared deployment configuration without exposing the deployment token.
- If `PAPERLESS_TOKEN` is configured while authenticated-profile fallback is disabled, startup logs a clear, secret-free warning. This condition does not make the application unhealthy because the deployment token can intentionally be reserved for anonymous mode.
- Compose and README documentation cover the typed fallback option, its secure default, token priority, anonymous-mode behavior, session-access warning, deployment URL behavior, secret handling, startup warning, backup/restore implications, and mode changes.
- Unit, persistence, component, mode-switch, and isolation tests cover token resolution priority, disabled/enabled fallback, secret-free startup warning and healthy status, encryption at rest, owner-only decryption, browser and log non-disclosure, URL prepopulation/read-only behavior, the replacement controls, anonymous behavior, and transitions between anonymous and authenticated modes.

## Out of scope

- Displaying the deployment token, copying it into a profile, or allowing a profile owner to enable deployment-token fallback.
- Sharing one profile-stored token with another authenticated profile or adding profile-administrator roles.
- Storing profile tokens in browser storage or introducing client-side key management.
- Managing Paperless users, permissions, ownership, correspondents, or visibility rules from Scan Bridge.

## Dependencies

- US-011
- US-012
- US-017
- US-030

## Superseded behavior

- This story deliberately revises US-012's and US-017's requirement that a saved profile token cannot be returned to or revealed in the browser. The new owner-only, protected settings behavior above is authoritative while preserving encryption at rest and all cross-profile isolation requirements.
- Deployment-token fallback for authenticated profiles becomes an operator-controlled deployment policy with a secure disabled default, rather than a per-profile checkbox. Anonymous mode retains its existing shared deployment configuration.

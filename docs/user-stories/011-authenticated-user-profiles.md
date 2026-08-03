# US-011: Authenticated user profiles

## User story

**As a** user of a shared Scan Bridge, **I want** to sign in with my existing identity, **so that** my profile and confidential configuration are separated from other users.

## Acceptance criteria

- An administrator can configure at least one OpenID Connect provider (initially Google or Microsoft Entra ID) through deployer-controlled settings without embedding client secrets in the image.
- Anonymous users cannot access scans, documents, settings, or Paperless credentials; health and sign-in endpoints remain operable.
- A stable provider subject and issuer are mapped to an internal user record without using an email address as the immutable identity key.
- Sign-in, sign-out, access denial, expired sessions, provider errors, and account removal have useful mobile-friendly behavior.
- Antiforgery, secure cookie, forwarded-header, redirect-URI, and data-protection behavior is documented and tested for the reverse-proxy deployment.
- Existing local defaults are assigned through an explicit one-time migration choice rather than silently attached to the first account.
- Authorization and isolation are integration-tested with two identities.

## Out of scope

- Password storage in Scan Bridge, custom identity-provider hosting, and organization-wide role management.
- Storing Paperless credentials; that follows in US-012.

## Dependencies

- US-008 and US-009

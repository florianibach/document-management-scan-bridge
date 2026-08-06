# US-011: Authenticated user profiles

## User story

**As a** self-hosting administrator and shared Scan Bridge user, **I want** to choose between authenticated profiles and an anonymous single-household profile, **so that** multi-user homes can separate accounts while single-user homes can run without a login screen.

## Acceptance criteria

- An administrator can configure the profile mode through deployer-controlled settings, with supported modes for OpenID Connect-authenticated profiles and an anonymous single-profile mode that requires no login.
- In authenticated mode, anonymous users cannot access scans, documents, settings, or Paperless credentials; health and sign-in endpoints remain operable. In anonymous mode, the single anonymous profile is the active profile for all visitors and this trade-off is explicit in the UI and documentation.
- In authenticated mode, a stable provider subject and issuer are mapped to an internal user record without using an email address as the immutable identity key. In anonymous mode, a stable deployment-local anonymous subject is mapped to one internal profile.
- Sign-in, sign-out, access denial, expired sessions, provider errors, and account removal have useful mobile-friendly behavior.
- Antiforgery, secure cookie, forwarded-header, redirect-URI, and data-protection behavior is documented and tested for the reverse-proxy deployment.
- Existing local defaults are assigned through an explicit one-time migration choice: move them to the anonymous profile, assign them to a selected authenticated user, or reset them.
- Authorization and isolation are integration-tested with two authenticated identities, and anonymous mode is integration-tested to use exactly one shared deployment-local profile.

## Provider setup documentation requirements

The implementation must include a README section for setting up at least one supported OpenID Connect provider. For Google, document the administrator flow at a level that is useful without embedding deployment secrets:

1. Create or choose a Google Cloud project and configure the OAuth consent screen for the household or trusted users.
2. Create an OAuth 2.0 Web application client, add the public Scan Bridge origin, and register the exact callback path used by the application, for example `https://scan.example.test/signin-oidc`.
3. Store the generated client ID and client secret outside the image, preferably in `.env` or the deployment secret manager, and map them to the documented Compose variables.
4. Configure allowed issuers/scopes, forwarded headers, cookie security, external HTTPS URL, and redirect URI behavior for the reverse proxy.
5. Describe sign-in testing, sign-out testing, denied-user behavior, secret rotation, provider removal, and recovery if the configured provider is unavailable.

The same section should explain the anonymous profile mode side by side with OIDC mode so single-user deployments can choose the no-login path intentionally.

## Out of scope

- Password storage in Scan Bridge, custom identity-provider hosting, and organization-wide role management.
- Storing Paperless credentials; that follows in US-012.

## Dependencies

- US-008 and US-009

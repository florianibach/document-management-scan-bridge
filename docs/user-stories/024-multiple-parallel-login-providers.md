# US-024: Multiple parallel login providers

## User story

**As a** member of a household whose users have different identity providers, **I want** Scan Bridge to offer every configured login provider, **so that** each person can sign in with their preferred account.

## Acceptance criteria

- Operators can configure more than one supported OpenID Connect provider at the same time, with a stable internal key, user-facing display name, authority, client ID, client secret, callback path, and provider-specific logout behavior for each provider.
- When multiple providers are enabled, the sign-in screen presents an accessible provider choice; when exactly one is enabled, the existing direct or single-provider sign-in experience remains simple.
- Each login challenge and callback is routed to the selected provider without leaking one provider's credentials or state into another provider's flow.
- Profile identity includes the issuing provider and stable subject so accounts from different providers remain separate even when their email address or display name matches.
- Sign-out always clears the local Scan Bridge session and applies only the active provider's supported remote sign-out behavior.
- A failure or temporary outage of one provider does not prevent users from selecting another configured provider, and errors identify the affected provider without exposing secrets.
- Configuration startup rejects duplicate keys, callback collisions, missing required values, insecure authorities outside documented development exceptions, and ambiguous legacy single-provider settings.
- Existing single-provider configuration has a documented, backward-compatible migration path, and secrets remain environment-managed and absent from logs and browser responses.
- Authorization and isolation tests cover at least two providers, equal email addresses across providers, provider selection tampering, callback correlation, sign-out, and one-provider outage.

## Out of scope

- Linking or merging identities across providers.
- Adding password authentication or hosting an identity provider inside Scan Bridge.
- Automatically migrating two existing profiles into one account.

## Dependencies

- US-011
- US-020

# US-034: Safe runtime defaults without Compose variables

## User story

**As a** self-hosting operator, **I want** the container and application to retain documented defaults when Compose omits optional environment variables, **so that** simplifying or replacing the supplied Compose file does not produce invalid or surprising behavior.

## Acceptance criteria

- Every operator-facing variable currently substituted by `compose.yaml` has an equivalent typed application or container-runtime default, so deleting that variable's entry from the service `environment` section preserves the documented default behavior.
- Defaults are maintained from one authoritative application/runtime definition wherever practical; Compose may expose or document them but is not the only layer that makes the application safe to start.
- Configuration precedence is explicit and tested: a valid environment value overrides the runtime default, while omission uses the runtime default. An explicitly supplied invalid value fails validation with an actionable, secret-free diagnostic rather than silently becoming the default.
- Empty values have documented semantics per setting. Secret and identity values such as the Paperless token, OIDC client secret, scanner device ID, and remote sign-out URL remain unset by default; no placeholder credential or environment-specific identifier is invented.
- Fixed container paths and other values currently present directly in the Compose environment have safe runtime defaults where the image relies on them, including persistence, temporary storage, data-protection keys, and generated SANE configuration.
- The application remains startable with an intentionally minimal service definition containing the required image/build, host networking, volumes, health check, and no optional environment entries.
- The image's health check and application listener agree on the default HTTP port even when `APPLICATION_HTTP_PORT` is absent from the container environment.
- README configuration tables identify the runtime default, omission behavior, empty-value behavior, supported override key, and which settings are secrets. Compose documentation no longer implies that its substitutions are the sole source of defaults.
- Automated tests compare omitted and explicitly defaulted configuration, cover valid overrides and invalid explicit values, and prevent drift between documented, typed, container, and Compose defaults.
- Container validation builds the image and starts both the supplied Compose configuration and a minimal no-optional-environment configuration, then verifies readiness and the effective non-secret configuration.

## Out of scope

- Providing non-empty default credentials, tokens, private URLs, scanner identifiers, or identity-provider registration values.
- Removing supported environment-variable overrides or introducing a remote configuration service.
- Making malformed explicit configuration silently acceptable.

## Dependencies

- US-009
- US-023
- US-030

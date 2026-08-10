# US-023: Configurable application HTTP port

## User story

**As a** self-hosting operator using host networking, **I want** to configure Scan Bridge's HTTP listening port through an environment variable, **so that** I can avoid conflicts with other services on the host.

## Acceptance criteria

- The supported Compose deployment exposes an operator-facing environment variable for the application HTTP port, with `8080` retained as the backward-compatible default.
- The configured value changes the application's internal HTTP listening endpoint used with host networking; no Docker port publishing is introduced.
- Startup validates that the port is an integer in the valid TCP port range and fails with an actionable message when it is invalid or unavailable.
- Health-check configuration and documented local URLs use the configured port rather than assuming `8080`.
- README and example environment documentation describe the variable, default, host-network behavior, restart procedure, conflict diagnosis, and validation commands.
- Automated configuration tests cover the default, a valid override, malformed and out-of-range values, and generated Compose configuration.

## Out of scope

- Configuring HTTPS certificates or reverse-proxy listener ports.
- Supporting Docker port publishing alongside the existing host-network deployment.

## Dependencies

- US-009

## Completion evidence

- Compose passes `APPLICATION_HTTP_PORT` to the container with `8080` as its default, retains host networking without port publishing, and resolves the same value into its health check.
- The container entrypoint validates integer syntax and the TCP range, checks Linux IPv4 and IPv6 listener tables for conflicts, and configures ASP.NET Core's HTTP listener only after validation.
- `scripts/test-http-port-configuration.sh` covers the default, a valid override, malformed and out-of-range inputs, generated default and overridden Compose configurations, matching health URLs, and absence of published ports.
- Operator documentation records the non-secret variable, default, host-network semantics, recreation procedure, validation commands, conflict diagnosis, and the documented bind-race limitation.
- UI, workflow, persistence, scanner hardware, cancellation, retry, temporary-data cleanup, component tests, and viewport checks are not applicable because this story changes only container startup configuration. The existing readiness endpoint supplies the representative operational end-to-end check.

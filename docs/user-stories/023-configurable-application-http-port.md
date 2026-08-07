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

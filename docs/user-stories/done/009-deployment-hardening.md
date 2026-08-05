# US-009: Harden self-hosted deployment (done)

## User story

**As a** self-hosting administrator, **I want** an observable and recoverable deployment, **so that** the scan bridge can run reliably on an always-on machine.

## Acceptance criteria

- The documented Docker Compose deployment is verified on a supported non-ARM development host with `docker compose config`; Raspberry Pi/ARM64 hardware verification remains a release-gate manual check because this environment has no target device.
- Container health checks reflect application readiness without depending on the scanner or Paperless-ngx always being available.
- Structured logs diagnose scan, PDF, persistence, and upload failures without leaking secrets or document content.
- Secrets, Docker Compose variables, network discovery, volume ownership, temporary storage, persistent data, and resource expectations are documented, including variable meanings, defaults, secret handling, and validation commands.
- Backup and restore procedures cover SQLite configuration and clarify whether in-progress scans are recoverable.
- Graceful shutdown and restart behavior do not corrupt persistent state or publish partial output.
- Upgrade and rollback instructions are documented for representative data, and the target-hardware end-to-end workflow is recorded as a manual release-gate check.

## Out of scope

- High availability, multi-node operation, automated deployment, and registry image publishing unless separately approved.

## Dependencies

- US-001 through US-008

## Completion notes

- The image and Compose service now define a local `/health` readiness probe that validates SQLite plus writable temporary and data-protection storage only.
- Deployment documentation now covers host networking, Docker Compose variable usage and meanings, secrets, persistent and temporary volumes, backup, restore, upgrade, rollback, graceful shutdown, logs, and resource expectations.
- The development environment required installing .NET and Docker tooling; Docker daemon startup and Raspberry Pi/HP scanner hardware checks are explicitly listed as warnings in the implementation evidence rather than claimed as passed.

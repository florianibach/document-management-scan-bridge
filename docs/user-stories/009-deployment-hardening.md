# US-009: Harden self-hosted deployment

## User story

**As a** self-hosting administrator, **I want** an observable and recoverable deployment, **so that** the scan bridge can run reliably on an always-on machine.

## Acceptance criteria

- The documented Docker Compose deployment is verified on a representative Raspberry Pi/ARM64 host and one supported non-ARM development host.
- Container health checks reflect application readiness without depending on the scanner or Paperless-ngx always being available.
- Structured logs diagnose scan, PDF, persistence, and upload failures without leaking secrets or document content.
- Secrets, network discovery, volume ownership, temporary storage, persistent data, and resource expectations are documented.
- Backup and restore procedures cover SQLite configuration and clarify whether in-progress scans are recoverable.
- Graceful shutdown and restart behavior do not corrupt persistent state or publish partial output.
- Upgrade and rollback instructions are exercised with representative data, and an end-to-end workflow is manually verified against target hardware.

## Out of scope

- High availability, multi-node operation, automated deployment, and registry image publishing unless separately approved.

## Dependencies

- US-001 through US-008

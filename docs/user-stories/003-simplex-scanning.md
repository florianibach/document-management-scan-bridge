# US-003: Scan a simplex document

## User story

**As a** user, **I want** to start a one-sided scan from my phone, **so that** I can capture a document without using the printer panel or a desktop computer.

## Acceptance criteria

- A touch-friendly screen lets the user select supported scan settings and start a simplex job.
- The workflow captures one or more pages through the scanner adapter and stores them in an isolated scan session.
- The UI reports useful queued, running, completed, cancelled, and failed states.
- Duplicate submissions are prevented while a job is active.
- Cancellation is offered where the backend permits it and always leaves the session in a consistent state.
- Partial output, command failures, timeouts, and unavailable scanners are handled safely and communicated clearly.
- Temporary files use configured storage and are not shared across sessions.

## Out of scope

- Manual duplex scanning, page editing, PDF creation, and upload.

## Dependencies

- US-002

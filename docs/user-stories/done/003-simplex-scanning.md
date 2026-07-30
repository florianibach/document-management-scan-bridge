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

## Verification record

- The application workflow tests cover isolated multi-page sessions, duplicate prevention, cancellation cleanup, partial-output cleanup, and safe failure diagnostics.
- The process boundary is exercised through controlled adapter/process tests; the complete automated suite covers the Blazor screen and existing discovery integration.
- The responsive Bootstrap layout was checked at 390 × 844 and 1440 × 900. Controls remain full-width and touch-sized on mobile, and form fields form three columns on desktop.
- A controlled end-to-end run starts from the component, invokes the workflow adapter, writes two fixture pages into a unique session, and reaches the completed state.
- Local application startup and `/health` are verified without scanner hardware. Docker was unavailable in this environment, so container build and Compose validation/startup remain CI checks. A physical simplex platen/ADF run on the target HP Color Laser MFP 179fnw remains a deployment acceptance check because the device is not reachable from this environment; record its firmware and the commands listed in the root README before milestone acceptance.

## Definition of Done assessment

- All acceptance criteria map to application, adapter, component, unit, or integration behavior. Retry is not applicable: automatically repeating a physical scan could duplicate pages and is therefore unsafe.
- SQLite persistence, schema migrations, Paperless credentials, duplex ordering, editing, PDF generation, and upload are not affected by this story.
- Temporary-data cleanup, cancellation, timeouts, unavailable commands/scanners, diagnostics, dependency auditing, container operation, and documentation are covered. No new dependency was introduced.
- The target-hardware run is the only accepted environmental limitation; no known critical defect remains in the controlled implementation.

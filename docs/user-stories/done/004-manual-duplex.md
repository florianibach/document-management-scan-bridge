# US-004: Scan a document using manual duplex

## User story

**As a** user with a simplex document feeder, **I want** guidance through two scan passes, **so that** a two-sided document is assembled in the correct reading order.

## Acceptance criteria

- The workflow scans all front sides, pauses, and presents an unambiguous mobile-friendly stack-flip instruction.
- The user explicitly confirms the flip before the back-side pass starts.
- Front and back passes are merged into reading order for the verified feeder output behavior.
- Odd-page documents and a final blank back side are handled without losing a real page.
- The workflow detects incompatible pass page counts and asks the user to resolve or restart rather than silently guessing.
- Refreshing or reconnecting the UI does not accidentally start another pass or corrupt the active session.
- Automated tests cover ordering for representative even, odd, reversed, cancelled, and mismatched-pass scenarios.

## Out of scope

- Automatic duplex hardware support and arbitrary feeder orientations not validated for the target printer.
- Page editing and final PDF output.

## Dependencies

- US-003

## Completion evidence

- The application-layer `ManualDuplexWorkflow` owns both passes, requires an explicit flip confirmation, reverses the target feeder's back pass, interleaves pages, handles a declared blank final back, rejects incompatible counts, and cleans cancelled/failed sessions.
- Unit tests cover even and odd ordering, the reversed second pass, explicit confirmation, a returned final blank, cancellation, and mismatched counts. Component tests cover the mobile flip instruction and confirmation control.
- The original application-wide singleton was replaced by the browser-circuit isolation in US-010. A temporary Blazor reconnect retains its circuit, while independently connected browsers cannot observe or control each other's pass.
- User and operational behavior, temporary ordered output, restart semantics, and the verified feeder orientation are documented in `README.md`.
- A component regression test verifies that duplex selects the cached ADF source independently of the simplex source selector and forwards the currently selected color mode and resolution unchanged.
- Regression coverage verifies that the initially displayed cached source is forwarded without requiring a change event and that cancellation while waiting for the stack flip finishes immediately and removes the session.
- Source, color, and resolution options render their selected state explicitly, so browser HTML parsing before Blazor becomes interactive cannot show the first option while retaining a different backing value; component coverage verifies both the selected markup and submitted settings.

## Definition of Done record

- Validation, cancellation, empty/failure behavior, safe logging, cleanup, and recovery are implemented. Preview, editing, PDF creation, and automatic duplex remain explicitly out of scope.
- No new external dependency or persistence migration is required. Existing scanner process integration remains covered by controlled integration tests.
- Locked restore, Release build, the complete automated suite, repository validation, and dependency vulnerability audit passed. Container build, Compose validation/startup, and a container health check could not run because this environment has no Docker or compatible container runtime; the unchanged container boundary remains covered by CI.
- Desktop and mobile structure is responsive through Bootstrap's existing breakpoints; the flip instruction uses large full-width controls, an ordered list, status announcements, and a labelled checkbox.
- Hardware execution was not repeated in this environment. The already recorded HP Color Laser MFP 179fnw feeder orientation is the supported orientation; firmware capture and final target-device milestone verification remain the documented outstanding hardware limitation.

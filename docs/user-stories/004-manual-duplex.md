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

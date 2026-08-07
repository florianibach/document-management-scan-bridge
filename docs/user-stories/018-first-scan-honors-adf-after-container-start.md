# US-018: First scan honors ADF after container start

## User story

**As a** user starting Scan Bridge after a container restart, **I want** the first scan to use the selected ADF source, **so that** pages are not unexpectedly scanned from the flatbed.

## Acceptance criteria

- When ADF is the selected and displayed source, the first scan after container startup passes that ADF source to the scanner backend instead of falling back to the flatbed.
- The source shown in the initial UI state is the source used by the scan request without requiring the user to change and reselect it.
- The regression is covered for simplex scanning and investigated for manual duplex scanning; if duplex is affected, the same fix and regression coverage apply, and if it is not affected, the verification evidence records why.
- Restart-focused automated coverage reproduces the initial-state path rather than relying on state left by an earlier scan.
- Existing source validation still blocks unavailable or stale ADF selections instead of silently substituting a different source.
- Verification on the target scanner records the container restart, displayed source, scan mode, backend source, and whether paper was acquired from the feeder.

## Out of scope

- Adding automatic duplex support or new scanner backends.
- Choosing ADF automatically when the user selected the flatbed.

## Dependencies

- US-003
- US-004

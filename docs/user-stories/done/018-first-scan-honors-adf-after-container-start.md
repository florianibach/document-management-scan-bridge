# US-018: First scan honors ADF after container start

**Status:** Done in automated verification. Container startup and target-scanner acquisition remain explicitly recorded release-gate checks because this development environment provides neither Docker nor access to the HP device.

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

## Verification evidence

- The initial Blazor render explicitly selects the cached ADF option and the first simplex action forwards that exact source without a browser change event. A fresh-render component regression covers the container-start-equivalent path.
- The manual-duplex path was affected by the same initial state. Its fresh-render regression proves that it independently resolves and forwards the cached ADF source for the first pass.
- `ScanSettingsSelection` validates the source against the current capability snapshot immediately before either workflow starts. A stale ADF value is rejected with recovery guidance; it is never replaced with `Flatbed`.
- The SANE adapter regression verifies that the validated source becomes the value following `scanimage --source`.

### Target HP scanner release gate

The CI/development container has no route to the target HP Color Laser MFP 179fnw, so paper acquisition cannot be truthfully reported as passed here. Before release acceptance, run the following on the target Linux host and attach the non-sensitive results to the release record:

1. `docker compose down && docker compose up --detach --build`
2. Open Scan Bridge in a new browser session and record that **Source** displays the expected ADF source before any selector interaction.
3. Load non-sensitive test paper, start simplex, and capture `docker compose logs --since=5m scan-bridge` plus the scanner backend diagnostics. Record scan mode `simplex`, the exact backend `--source` value, and whether the feeder acquired the paper.
4. Restart the container again and repeat with manual duplex. Record the displayed source, both backend source values, and feeder acquisition for both passes.
5. Remove or disable the cached ADF capability in a controlled test and confirm that the UI blocks the stale source rather than scanning from the flatbed.

Do not attach document contents, device serial numbers, private addresses, or credentials. Record scanner model, firmware, image commit, and test date alongside the results.

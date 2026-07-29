# US-002: Discover and validate the scanner

## User story

**As a** system administrator, **I want** the application environment to discover and inspect the network scanner, **so that** I can confirm it is compatible before relying on scan workflows.

## Acceptance criteria

- The container includes SANE, `sane-airscan`, and `scanimage` on each supported target platform.
- Scanner discovery is executed through the scanner adapter's process boundary, not directly from UI code.
- The application can report discovered devices and the selected device's supported sources, formats, resolutions, and paper sizes.
- Configuration allows selecting the target device without embedding a device-specific identifier in code.
- Timeouts, missing executables, no-device results, and non-zero command exits produce actionable diagnostics without exposing secrets.
- The tested HP device model, firmware, network discovery requirements, and verified options are documented.

## Out of scope

- Starting a production scan job or assembling pages.
- Supporting scanner backends other than SANE/`sane-airscan`.

## Dependencies

- US-001

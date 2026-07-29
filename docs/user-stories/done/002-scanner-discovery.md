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

## Completion record

- Device discovery and capability inspection run exclusively through `IScanner` and `IProcessRunner`; parser, orchestration, process-boundary, and component tests cover the acceptance behavior.
- The runtime image installs `sane-utils` and `sane-airscan`; Compose uses Linux host networking so mDNS/WSD discovery reaches the container and exposes device selection and a bounded timeout without a source-coded identifier.
- Missing commands, timeouts, no devices, ambiguous/configured devices, and non-zero exits have distinct diagnostics that omit backend stderr.
- Local Release restore/build/test, image build, Compose configuration/start/health, repository validation, and skill validation are required completion checks.
- No scan acquisition or later workflow was introduced. Persistence, cleanup, and recovery are not applicable because discovery creates no files or database records. Retry is user-initiated through the discovery button.
- Mobile and desktop layout use Bootstrap's `col-12 col-md-6` breakpoints and were visually verified with Playwright. Generated screenshots remain local because the review platform does not support binary files.
- Physical HP model/firmware verification is hardware-dependent and remains explicitly required before milestone acceptance; commands and expected fixture-backed option categories are documented in `README.md`.

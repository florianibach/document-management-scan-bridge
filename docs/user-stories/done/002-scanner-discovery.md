# US-002: Discover and validate the scanner

## User story

**As a** system administrator, **I want** the application environment to discover and inspect the network scanner, **so that** I can confirm it is compatible before relying on scan workflows.

## Acceptance criteria

- The container includes SANE, `sane-airscan`, and `scanimage` on each supported target platform.
- Scanner discovery is executed through a backend discovery boundary, not directly from UI code or an external discovery executable.
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

- DNS-SD discovery runs through `IScannerDiscoveryService` and a .NET Zeroconf adapter for `_uscan` and `_uscans`; the existing `IScanner`/`scanimage` adapter remains responsible for SANE capability inspection.
- The runtime image installs `sane-utils` and `sane-airscan`; Compose retains Linux host networking. The UI displays every discovered physical scanner after duplicate advertisements are merged, requires explicit selection, validates eSCL `ScannerCapabilities`, and never accepts a browser-supplied URL.
- Missing commands, timeouts, no devices, ambiguous/configured devices, and non-zero exits have distinct diagnostics that omit backend stderr.
- Local Release restore/build/test, image build, Compose configuration/start/health, repository validation, and skill validation are required completion checks.
- The validated selection is persisted in SQLite and regenerates an atomic, data-volume-backed `airscan.conf` at selection and startup. No scan acquisition or later workflow was introduced; retry is user-initiated through the discovery button.
- Duplicate HTTPS/HTTP advertisements remain linked internally: HTTPS is tried first, and a certificate-specific failure may fall back only to the matching DNS-SD-advertised HTTP endpoint after validating its eSCL capabilities. TLS validation is never disabled.
- Mobile and desktop layout use Bootstrap's `col-12 col-md-6` breakpoints and were visually verified with Playwright. Generated screenshots remain local because the review platform does not support binary files.
- The HP Color Laser MFP 179fnw returned valid eSCL capabilities over its advertised HTTP endpoint; model capabilities and the certificate-specific HTTPS fallback are documented without retaining its serial number or UUID. Firmware recording remains required before milestone acceptance.

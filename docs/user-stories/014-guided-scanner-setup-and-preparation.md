# US-014: Guided scanner setup and preparation

## User story

**As a** household scanner user, **I want** scanner selection, validation, and scan settings presented as a guided preparation step, **so that** I can start a scan confidently without understanding eSCL, AirScan, or network diagnostics.

## Acceptance criteria

- When no scanner is selected, the Scan area shows a prominent setup card instead of a disabled scan action, with a clear path to scanner setup.
- The scanner setup screen can start network discovery, shows discovered compatible devices as touch-friendly cards, and provides an “select and validate” action per scanner.
- Scanner discovery and validation states distinguish search in progress, no devices found, duplicate physical devices merged, validation in progress, HTTPS preferred, controlled HTTP fallback after certificate-specific HTTPS failure, selected scanner, incompatible scanner, and unreadable capabilities.
- Scanner setup error messages begin with an understandable user explanation and hide transport details, raw capabilities, and diagnostics behind an explicit technical-details action.
- The Prepare step summarizes the selected scanner, last validation time, scan mode, source, color mode, resolution, and whether values came from profile defaults.
- Users can refresh scanner capabilities from the Prepare step, and unavailable or stale setting values are called out before a scan can start.
- If scanner validation fails, the UI offers safe retry and diagnostic paths without mutating the current selected scanner until a replacement has been validated.

## UI concept references

- “Noch kein Scanner ausgewählt” setup card.
- “Scanner einrichten” screen with discovery and validation cards.
- Preparation summary for scanner, document mode, source, color, resolution, and profile defaults.
- Scanner error tone with retry and expandable technical details.

## Out of scope

- Adding support for scanner protocols beyond the existing eSCL/AirScan path.
- Changing the scanner discovery algorithm except where required to expose already-known states accurately in the UI.

## Dependencies

- US-002
- US-008
- US-013

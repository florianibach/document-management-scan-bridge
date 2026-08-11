# US-033: Wake a sleeping scanner before scanning

## User story

**As a** user scanning from an automatic document feeder, **I want** Scan Bridge to prepare a sleeping scanner before the scan begins, **so that** the first scan uses my selected ADF source instead of unexpectedly falling back to the flatbed.

## Acceptance criteria

- Every simplex and manual-duplex scan request performs a bounded readiness/wake-up phase before starting page acquisition; it does not require a separate user action.
- The implementation investigates standards-based, backend-supported readiness mechanisms for the supported scanner protocols and records which mechanism is used. It does not assume that Wake-on-LAN, SNMP, a vendor API, or an arbitrary warm-up delay is universally supported.
- Readiness handling is capability-aware and works across supported scanners: an unsupported wake-up mechanism does not make an otherwise usable scanner incompatible.
- After the readiness phase, the scan command explicitly applies the source selected in the UI. Waking, probing, or retrying never silently replaces ADF with flatbed or another source.
- If the scanner is asleep but becomes ready within the configured scan timing boundaries, the original scan continues without duplicate page acquisition or an additional click.
- If wake-up/readiness cannot be confirmed, times out, or returns an unsupported response, Scan Bridge proceeds with the normal scan path as requested. Existing loading, timeout-decision, cancellation, and error behavior remains available.
- Diagnostics distinguish readiness activity from page acquisition without logging document content, credentials, or private scanner response payloads.
- Automated tests cover a scanner that is already ready, a sleeping scanner that becomes ready, an unsupported readiness mechanism, a readiness timeout/failure followed by the normal scan path, cancellation, and preservation of the selected ADF source for simplex and manual duplex.
- Hardware verification covers representative supported scanner models where available and records model, firmware, connection/backend, observed sleep state, readiness mechanism, selected source, backend source, and whether the first page was acquired from the ADF.
- Verification is coordinated with US-018: US-018 continues to cover initial source state after container startup, while this story covers device sleep immediately before any scan and must not mask an initial-state source defect with a wake-up retry.

## Out of scope

- Vendor-specific wake support that cannot be implemented through an existing supported scanner protocol or backend.
- Powering on a switched-off or physically disconnected scanner.
- Automatically choosing ADF when the user selected flatbed.

## Dependencies

- US-003
- US-004
- US-015
- US-018
- US-022

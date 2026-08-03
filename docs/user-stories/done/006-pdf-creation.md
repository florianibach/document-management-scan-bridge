# US-006: Create the final PDF

## User story

**As a** user, **I want** my reviewed pages converted into one PDF, **so that** the document is ready for archiving.

## Acceptance criteria

- The application creates one valid PDF from the session's current page order, rotations, and deletions.
- Output has practical image quality and size using documented defaults.
- Creation is repeatable for the same session state and cannot publish a partially written file as complete.
- Unsupported or corrupt input fails with an actionable message while retaining recoverable session data.
- Temporary and intermediate files are cleaned after success, cancellation, and failure according to a documented retention policy.
- Automated tests verify page count, order, rotation, basic validity, and cleanup behavior using controlled fixtures.

## Out of scope

- OCR, searchable text layers, digital signatures, and archival conformance guarantees such as PDF/A.

## Dependencies

- US-005

## Implementation evidence

- `PdfCreationWorkflow` validates the active edit snapshot and forwards its exact page order and rotations to the PDF boundary; unit tests cover order, rotations, empty/corrupt state, and actionable failure recovery.
- `PdfSharpDocumentWriter` resolves only PNG files inside the session, creates lossless image pages at the documented 300 dpi default, and publishes through a same-directory atomic rename. Integration tests open the result and verify validity, page count, rotation dimensions, replacement, partial cleanup, and source retention after corrupt input.
- The preview UI disables creation while a page is unavailable, exposes creation/cancellation state, and offers the completed PDF download. A component test covers the reviewed-page-to-download interaction.
- The representative automated workflow is controlled and does not require scanner hardware. Mobile-first controls reuse the responsive preview layout already checked for US-005; no new viewport-specific layout is introduced.

## Definition of Done record

- Acceptance criteria, safe validation, cancellation, repeat creation, diagnostics, atomic publication, and cleanup are implemented and covered by unit, integration, and component tests.
- PDF generation is behind an application boundary; no Paperless upload, OCR, text layer, signature, or PDF/A behavior from later stories is included.
- Logging contains only session identifiers, page counts, and exception types—not page names, content, or metadata.
- Source PNGs and a completed PDF remain in temporary session storage for recovery. Only `.partial` intermediates are removed on every outcome; automated age-based cleanup remains explicitly deferred to US-009 deployment hardening.
- Hardware verification is not applicable because PDF creation uses controlled local files and has no scanner interaction. Existing outstanding milestone hardware verification remains unchanged.
- Build, full tests, repository validation, and dependency vulnerability audit pass. The pull request records the environment limitation that prevented container, Compose, health, and screenshot checks. No disabled tests or known critical defects remain.

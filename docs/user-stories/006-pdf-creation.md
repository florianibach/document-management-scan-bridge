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

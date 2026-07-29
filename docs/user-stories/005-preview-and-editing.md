# US-005: Preview and edit scanned pages

## User story

**As a** user, **I want** to inspect and correct scanned pages, **so that** obvious mistakes are fixed before a document is finalized.

## Acceptance criteria

- The UI shows a practical, responsive preview or thumbnail for every page in its current reading order.
- A user can delete a page only after an intentional action and can rotate a page in 90-degree increments.
- Page numbers and ordering update consistently after edits.
- Edits change only the active session and preserve source quality wherever the selected tooling permits.
- Large documents load progressively or otherwise avoid making the mobile UI unusable.
- Missing or corrupt page data produces a recoverable error and does not damage other pages.
- Editing behavior is covered by application and component tests.

## Out of scope

- Full image editing, OCR correction, annotation, and arbitrary page reordering.

## Dependencies

- US-003 and US-004

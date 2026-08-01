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

## Implementation evidence

- Responsive, lazy-loaded previews are rendered after both simplex and manual-duplex completion in current reading order.
- Rotation is stored as non-destructive 90-degree session metadata; deletion requires a second confirmation and immediately renumbers the remaining pages.
- PNG availability is checked per page. A missing/corrupt page gets a recoverable message without affecting its siblings.
- Application tests cover rotation, deletion, renumbering, source preservation, and corrupt data; component tests cover preview rendering and intentional deletion.
- Empty, cancellation, and scan failure behavior remain owned by US-003/US-004. Arbitrary reordering, image manipulation, persistence across reload, PDF generation, and hardware-specific preview behavior are not applicable or explicitly out of scope.
- Representative UI behavior is covered programmatically at Bootstrap's mobile-first base layout and responsive grid breakpoint. Hardware validation is not applicable because editing has no scanner boundary.

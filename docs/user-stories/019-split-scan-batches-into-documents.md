# US-019: Split scan batches into documents

## User story

**As a** household user processing incoming mail, **I want** to scan one complete stack and split its pages into separate documents afterward, **so that** I can minimize scanner handling while still creating and uploading each letter independently.

## Acceptance criteria

- A user can acquire one multi-page batch through either simplex or the supported manual duplex workflow before deciding document boundaries.
- The Review UI lets the user add, move, and remove manual split points between pages and clearly previews the resulting ordered documents.
- Every scanned page belongs to exactly one resulting document; empty documents, overlapping ranges, and unassigned pages cannot be submitted.
- Page rotations, removals, and ordering remain visible and are applied to the correct resulting document when split points change.
- Each resulting document has its own PDF creation state, download, Paperless metadata, upload state, success result, and retry path.
- A user can finish or retry one document without recreating or re-uploading already successful documents from the same batch.
- Navigation and recovery preserve the unsent parts of the batch across ordinary reconnects or refreshes according to the existing temporary-document lifetime and profile isolation rules.
- The UI shows batch-level progress using document and page counts without inventing completion percentages that the backend cannot provide.
- Automated tests cover simplex and manual-duplex batches, boundary editing, page removal near a boundary, per-document metadata, partial upload success, retry behavior, and profile isolation.

## Out of scope

- Separator-sheet, barcode, OCR, or content-based automatic document detection.
- Combining pages from unrelated scan sessions into a batch.
- Uploading the whole batch as one Paperless document after it has been split.

## Dependencies

- US-015
- US-016

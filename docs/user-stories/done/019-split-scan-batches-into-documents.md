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

## Verification record

- `ScanBatchWorkflowTests` cover simplex/manual-duplex inputs, add/move/remove boundaries, complete page coverage, removal near a boundary, rotation retention, independent metadata, partial success, idempotent successful uploads, retry state, refresh recovery, and profile isolation.
- `HomePageTests.ReviewCanSplitBatchAndShowsDocumentAndPageProgress` covers the accessible boundary control, ordered document preview, and count-only batch progress.
- Existing scanner, manual-duplex, page-editing, PDF, Paperless client, ownership-route, and component suites provide regression coverage for the unchanged acquisition and external boundaries.
- Persistence uses atomic temporary JSON beside the source scan. This is intentionally temporary rather than a document history and follows the existing volume cleanup lifetime.
- Scanner hardware was not available in this environment. The already verified HP ordering behavior is unchanged; a representative physical batch split/upload remains a release-device check.
- No Compose variable, migration, dependency, scanner process, or deployment topology changed. Those Definition-of-Done documentation items are not applicable; container and Compose checks remain regression checks.

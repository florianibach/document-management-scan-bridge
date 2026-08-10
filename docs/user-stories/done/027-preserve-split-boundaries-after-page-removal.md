# US-027: Preserve split boundaries after page removal

## User story

**As a** user who splits a scan batch into multiple documents, **I want** marked document boundaries to be updated correctly when I remove a page, **so that** my intended document split does not shift unnoticed.

## Acceptance criteria

- A split boundary is managed semantically relative to the remaining pages and does not remain attached to a stale numeric page index.
- When a page before a split boundary is removed, the boundary follows the remaining pages so that the previously intended document split is preserved.
- Removing a page directly at a split boundary creates neither an empty document nor an invalid, duplicate, or out-of-range boundary.
- Behavior when removing a page adjacent to a boundary is unambiguous and established by automated tests.
- Every remaining page continues to belong to exactly one resulting document.
- The Review UI updates page numbers, split markers, and document previews immediately and consistently.
- Persisted split points restored after a refresh represent the corrected split.
- Automated workflow and component tests cover removal before, after, and directly at a boundary, as well as multiple boundaries and consecutive removals.

## Out of scope

- New automatic document-boundary detection or page-reordering behavior beyond correcting existing manual split boundaries after removal.

## Dependencies

- US-019

## Verification record

- `ScanBatchWorkflowTests` establish the boundary semantics for removal before, after, and directly at a boundary, plus multiple boundaries and consecutive removals. They assert corrected persisted split points, non-empty documents, and exact page coverage.
- `HomePageTests.RemovingPageBeforeBoundaryImmediatelyRenumbersMarkerAndDocuments` verifies that the confirmation action awaits batch correction and immediately refreshes page numbers, the accessible split marker, and document previews.
- `PageEditingSessionTests.ReloadKeepsStablePageIdentityForPersistedBoundaries` verifies stable session/file page identities so corrected semantic boundaries survive a refresh.
- Existing batch persistence, profile isolation, PDF, upload, page-editing, and component suites provide regression coverage for unchanged external boundaries and the complete review-to-send workflow.
- No configuration, migration, dependency, scanner process, container topology, logging, secret handling, or cleanup behavior changed; those Definition-of-Done items are not applicable beyond regression checks.
- Scanner hardware was not available and is not required for this metadata-only correction. The scan acquisition and manual-duplex ordering paths are unchanged.
- Representative UI behavior is covered through the rendered Blazor component at its responsive markup boundary. No perceptible styling or layout changed, so new viewport screenshots are not required.

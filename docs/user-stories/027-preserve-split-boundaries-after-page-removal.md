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

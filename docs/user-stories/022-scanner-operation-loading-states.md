# US-022: Scanner operation loading states

## User story

**As a** user interacting with scanner setup, **I want** visible loading feedback while scanner or network operations are pending, **so that** I know my action was accepted and do not submit conflicting actions.

## Acceptance criteria

- Scanner discovery, capability loading or refresh, selection and validation, forgetting, and other scanner actions that call the application API or interact with the operating-system scanner backend expose an immediate indeterminate loading state.
- Loading feedback identifies the action in progress with user-facing text and an accessible busy state; it does not use artificial percentage progress.
- While an operation can mutate or invalidate scanner selection, the entire scanner-selection control group is disabled to prevent conflicting requests and repeated taps.
- Read-only content that is safe to retain remains visible while loading, but stale values are not presented as newly validated results.
- Success, empty, cancellation, timeout, and failure outcomes replace the loading state deterministically and restore valid controls.
- Losing and restoring the browser connection does not leave controls permanently busy or allow a duplicate backend operation to be presented as a new one.
- Automated component and integration tests cover each operation category, duplicate-action prevention, accessibility state, timeout or cancellation, and recovery after errors.

## Out of scope

- Fabricated progress percentages or estimates from scanner operations that expose no measurable progress.
- A global page blocker for unrelated document review or administrative actions.

## Dependencies

- US-014
- US-017
- US-021 for the forget-scanner loading state

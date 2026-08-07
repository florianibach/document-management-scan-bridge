# US-021: Forget a saved scanner

## User story

**As a** household administrator replacing a scanner, **I want** Scan Bridge to forget the retired device, **so that** it is no longer selected or offered as a saved default.

## Acceptance criteria

- A user can start a forget action for a saved scanner from scanner settings or setup, with the affected device identified clearly.
- A confirmation step explains that saved selection, cached capabilities, profile defaults referencing the scanner, and generated scanner-backend configuration owned by Scan Bridge will be cleared where applicable.
- Confirming the action removes Scan Bridge-owned persisted and generated references to the scanner without deleting unrelated system configuration or affecting other saved scanners.
- The operation cannot run while that scanner has an active scan; the UI explains the conflict and offers a safe retry after the job finishes or is cancelled.
- While removal work is running, the affected scanner controls are blocked and show an indeterminate loading state.
- Success is explicitly confirmed and the UI returns to the no-selection or remaining-scanner state; partial or failed cleanup reports actionable recovery information and does not claim success.
- Forgetting a scanner does not blacklist it: a later discovery may show and validate the same physical scanner again as a new selection.
- Automated coverage verifies persistence cleanup, profile-default repair, generated configuration cleanup, active-job protection, idempotent retry, unrelated-scanner preservation, and rediscovery eligibility.

## Out of scope

- Deleting or reconfiguring a scanner on the network.
- Maintaining a permanent discovery denylist.

## Dependencies

- US-002
- US-008
- US-014
- US-017

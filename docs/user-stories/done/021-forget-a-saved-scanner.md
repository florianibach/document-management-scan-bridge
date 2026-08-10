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

## Verification record

- The settings component identifies each saved device, presents the complete cleanup scope in a separate confirmation, blocks its controls with an indeterminate spinner, and reports success, active-scan conflicts, and actionable cleanup failures.
- Application-level forget orchestration uses a process-wide scanner operation guard, repairs persisted profile references transactionally, preserves unrelated scanner records, reconciles generated configuration, and treats retry after completed persistence cleanup idempotently.
- Unit tests cover active-job protection and idempotent retry; SQLite integration tests cover record removal, profile repair, unrelated scanner preservation, and retry; configuration-writer integration tests verify scoped generated-file cleanup. Existing discovery tests demonstrate that no denylist is introduced.
- The complete automated suite, Release build, repository validation, dependency vulnerability audit, local startup/health check, and responsive Chromium checks at 390×844 and 1440×1000 passed. Container build and Compose validation/startup could not run because this execution environment has no Docker executable. This workflow requires no scanner hardware because it does not alter the device or network configuration.

## Definition of Done notes

- Cancellation is not exposed during the short local cleanup operation; cancelling the active scan is the safe conflict-resolution path. Retrying reconciles generated configuration after partial cleanup.
- No persistence migration, dependency, secret, Compose variable, or deployment topology change is introduced. Existing structured logs contain scanner database IDs and cleanup counts but no endpoint, document content, or credentials.
- HP hardware verification is not applicable: forgetting affects only Scan Bridge-owned SQLite rows and its generated configuration, while rediscovery remains covered by controlled discovery tests.

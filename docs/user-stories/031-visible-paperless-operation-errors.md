# US-031: Visible Paperless operation errors

## User story

**As a** user preparing or sending a scanned document, **I want** Paperless failures to remain visible and actionable in the active scan workflow, **so that** I know what failed, which document is affected, and how to recover without assuming that a document was sent.

## Acceptance criteria

- A missing effective Paperless URL or API token produces a visible, understandable error in the active scan workflow when metadata or upload requires it; the failure is not represented only in logs or a settings page.
- Metadata retrieval and upload surface user-safe messages for configuration failures, network failures, timeouts, authentication failures, authorization failures, invalid or unexpected responses, and Paperless server failures. No such failure is silently discarded.
- Each error offers a relevant next action, such as opening Paperless settings or retrying the failed operation. A copyable diagnostic ID is shown when one is available.
- An operation error remains visible until the user retries, the operation succeeds, the user deliberately dismisses it, or the user leaves the workflow. Unrelated rendering or state updates do not clear it.
- A failed upload never marks its document as sent, accepted, or complete. Retrying remains possible without recreating or re-uploading documents that Paperless already accepted.
- For a split batch, the UI identifies the affected document unambiguously using its workflow document number or another non-sensitive batch-local label, and independent documents retain their own metadata and upload states.
- UI messages and application logs do not disclose API tokens, document contents, private Paperless metadata, Paperless response bodies, or unnecessary filenames. Errors expose only the minimum safe operational context needed for recovery and diagnostic correlation.
- Component tests cover visible, persistent, dismissible, and actionable metadata and upload errors, including missing URL/token and diagnostic IDs. Workflow tests cover every failure category, retry and success transitions, failed-upload state, partial success in split batches, and isolation of the affected document.

## Out of scope

- A full log viewer, display of raw Paperless responses, or disclosure of exception details in the scan UI.
- Automatic background retries with an operator-selected retry policy.
- Paperless administration, credential issuance, or remediation of Paperless-side permissions.
- Replacing the existing batch split, PDF creation, or successful-upload workflows beyond the error-state behavior required here.

## Dependencies

- US-016
- US-017
- US-019

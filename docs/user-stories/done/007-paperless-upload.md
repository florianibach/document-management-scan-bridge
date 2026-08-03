# US-007: Upload a document to Paperless-ngx

## User story

**As a** Paperless-ngx user, **I want** to submit the PDF with useful metadata, **so that** it enters my normal document-processing workflow without another manual upload.

## Acceptance criteria

- An administrator can configure the Paperless-ngx base URL and API credentials without committing secrets.
- A connectivity check distinguishes authentication, authorization, network, and server failures where practical.
- The submission screen accepts the supported title, correspondent, document type, tags, and any other explicitly selected MVP metadata.
- Metadata choices are loaded from and mapped to the Paperless-ngx REST API correctly.
- Upload progress and the accepted result are visible, and an upload is not silently duplicated after retry or reconnect.
- Failed uploads preserve the generated PDF long enough for a controlled retry or manual recovery.
- HTTP behavior is integration-tested with a controlled server double; credentials and sensitive headers are absent from logs.

## Out of scope

- Replacing Paperless-ngx OCR and processing, multi-target uploads, and general Paperless-ngx administration.

## Dependencies

- US-006

## Completion evidence

- Typed environment-backed configuration supplies the base URL, token, and timeout without storing a credential in the repository; the operator guide documents token creation and least-required access.
- The controlled HTTP-client integration tests cover authentication, authorization, server failures, metadata mapping, authorization headers, and multipart fields. Network and timeout failures have separate safe diagnostics.
- The component test covers loading correspondent, document-type, and tag choices, entering a title, uploading, showing acceptance, and suppressing a duplicate submission for the accepted browser session. The workflow unit test independently verifies duplicate suppression.
- Upload cancellation and failures retain the completed PDF for retry or download. Logs contain only the scan-session identifier and never authorization headers, metadata, filenames, or document content.
- Representative workflow verification is automated with controlled doubles. Scanner hardware is not involved in this story. Mobile-first Bootstrap controls remain full-width and the existing responsive page was checked at mobile and desktop sizes.
- OCR, task-status polling, multi-target upload, Paperless administration, and age-based temporary-file cleanup remain out of scope. The Paperless task identifier proves API acceptance, not completion of downstream OCR.

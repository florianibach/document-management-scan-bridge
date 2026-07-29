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

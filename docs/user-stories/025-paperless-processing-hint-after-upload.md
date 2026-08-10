# US-025: Paperless processing hint after upload

## User story

**As a** user who has successfully sent a document to Paperless-ngx, **I want** to understand that further processing may take some time, **so that** I do not mistake an upload that is not yet fully processed for a failure.

## Acceptance criteria

- After Paperless-ngx has successfully accepted an upload, Scan Bridge shows a prominent hint that further processing in Paperless-ngx may take some time.
- The hint names **File Tasks** as the place in Paperless-ngx where processing progress can be checked.
- A direct link to File Tasks is not required.
- The hint appears only after confirmed successful acceptance by Paperless-ngx, not while an upload is running or after it has failed or been cancelled.
- The text neither claims that OCR has completed nor displays an invented progress value.
- For a split scan batch, the hint appears for each successfully handed-off document.
- Existing download, retry, and new-scan actions remain accessible.
- Automated component tests verify the hint in the successful state and its absence in running and failed states.

## Out of scope

- Direct File Tasks integration, Paperless processing-status polling, and reporting OCR completion or calculated processing progress.

## Dependencies

- US-016
- US-019

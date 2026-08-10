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

## Verification record

- The successful single-document and split-document send views show a prominent processing hint only inside their confirmed accepted/uploaded branches. The copy names **File Tasks**, makes no OCR-completion claim, and contains no invented progress value.
- `HomePageTests.CompletedPdfLoadsMetadataAndUploadsExactlyOnce` verifies the successful state, the processing-time explanation, File Tasks wording, PDF download, and new-scan action.
- `HomePageTests.ProcessingHintIsAbsentWhileUploadRunsAndAfterItFails` holds an upload in flight and then fails it, verifying that the hint is absent in both states while download and retry actions remain available.
- `HomePageTests.SplitDocumentsEachFollowReviewPdfAndSendBeforeAdvancing` exercises the per-document accepted branch used for every split document; successful documents retain independent hand-off state and actions.
- README user guidance now explains the post-acceptance processing state and where users can inspect it.
- No application workflow, external integration, configuration, migration, dependency, logging, secret handling, persistence, cleanup, scanner, or container behavior changed. Their story-specific Definition-of-Done items are therefore not applicable beyond the full regression and operational checks.
- The responsive UI uses the existing Bootstrap alert and full-width action system. Component rendering provides mobile/desktop-independent markup coverage; scanner hardware is not involved in this presentation-only change.
- Direct File Tasks integration, status polling, OCR completion reporting, and calculated processing progress remain out of scope.

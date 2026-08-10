# US-028: Apply Paperless default tags to split documents

## User story

**As a** user with configured Paperless default tags, **I want** them applied to every document split from a scan batch, **so that** I do not need to select the tags again for each partial document.

## Acceptance criteria

- When its Paperless metadata is initialized, every document created from a split batch receives a snapshot of the default tags effective for the scan session.
- Tags are applied regardless of whether the batch came from simplex or manual-duplex scanning.
- The behavior matches uploading a document that was not split.
- Later changes to profile defaults do not alter an already-running scan session.
- Users can add or remove inherited tags for each partial document without changing the metadata of other partial documents.
- After navigation, refresh, or retry, the individual tags of each document not yet sent successfully remain intact.
- Partial documents already uploaded successfully are not uploaded again, and their metadata is not changed afterward.
- Default tags that are invalid or no longer available in Paperless-ngx are reported according to the existing validation rules and are not used silently.
- Automated tests cover one default tag, multiple tags, no default tags, user-specific changes per partial document, refresh, and partial upload success.

## Out of scope

- Changing Paperless-ngx tags, adding new tag-selection rules, or retroactively modifying metadata of successfully uploaded documents.

## Dependencies

- US-008
- US-016
- US-019

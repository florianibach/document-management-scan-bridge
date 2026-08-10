# US-029: Reset the workflow after the final batch upload

## User story

**As a** user who has successfully sent every document in a scan batch to Paperless-ngx, **I want** to see an empty new workflow when I later return to the Scan area, **so that** a completed document is not presented for PDF creation again.

## Acceptance criteria

- After the last outstanding document uploads successfully, the entire batch is considered complete.
- After subsequently navigating to Settings and back to Scan, an empty, new scan workflow appears.
- In particular, the user does not return to the PDF step for the completed batch's first document.
- The new workflow contains no pages, split boundaries, PDFs, upload states, or editable metadata from the completed batch.
- Navigation does not discard a batch that has not been uploaded completely; only fully completed batches are reset automatically.
- A partially failed batch remains recoverable and opens at an outstanding document.
- Browser reconnect and refresh do not repeat successfully completed uploads.
- Completion state remains isolated by profile and browser.
- Automated component and workflow tests cover Scan → complete upload → Settings → Scan, as well as partial uploads, retry, refresh, and multiple partial documents.

## Out of scope

- A persistent history of completed documents or manually restoring a fully completed batch after the workflow has reset.

## Dependencies

- US-016
- US-019

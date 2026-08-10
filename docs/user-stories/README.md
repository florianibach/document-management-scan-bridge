# User stories

The stories below form the planned MVP roadmap. Implement them in order unless new findings require the dependencies or scope to be revised.

| ID | Story | Outcome |
| --- | --- | --- |
| [US-001](done/001-project-scaffolding.md) | Project scaffolding (done) | A runnable, testable, containerized foundation |
| [US-002](done/002-scanner-discovery.md) | Scanner discovery (done) | Validated communication with the target scanner |
| [US-003](done/003-simplex-scanning.md) | Simplex scanning (done) | One-sided documents can be scanned from the UI |
| [US-004](done/004-manual-duplex.md) | Manual duplex (done) | Two passes become correctly ordered pages |
| [US-005](done/005-preview-and-editing.md) | Preview and editing (done) | Users can inspect, rotate, and remove pages |
| [US-006](done/006-pdf-creation.md) | PDF creation (done) | Ordered pages become a final PDF |
| [US-007](done/007-paperless-upload.md) | Paperless-ngx upload (done) | PDFs and metadata can be submitted |
| [US-008](done/008-profiles-and-defaults.md) | Profiles and defaults (done) | Reused settings persist locally |
| [US-009](done/009-deployment-hardening.md) | Deployment hardening (done) | Self-hosting is reliable and documented |
| [US-010](done/010-browser-notifications-and-isolation.md) | Browser notifications and isolation (done) | Scan events are announced without global browser state |
| [US-011](done/011-authenticated-user-profiles.md) | Authenticated user profiles | External login protects and separates user profiles |
| [US-012](done/012-profile-service-configuration.md) | Per-profile service configuration | Paperless credentials and defaults belong to the selected profile mode |
| [US-013](done/013-mobile-first-navigation-and-workflow-shell.md) | Mobile-first navigation and workflow shell (done) | Daily scan workflow gets a guided app shell and separated admin areas |
| [US-014](done/014-guided-scanner-setup-and-preparation.md) | Guided scanner setup and preparation (done) | Scanner setup and settings are presented as a safe preparation step |
| [US-015](done/015-guided-scan-and-manual-duplex-states.md) | Guided scan and manual duplex states | Simplex and manual duplex scans use explicit, recoverable UI states |
| [US-016](done/016-review-pdf-and-paperless-send-flow.md) | Review, PDF, and Paperless send flow | Page review, PDF creation, and upload become one safe handoff flow |
| [US-017](done/017-settings-status-and-diagnostics-ui.md) | Settings, status, and diagnostics UI | Configuration and troubleshooting move into clear non-scan areas |
| [US-018](018-first-scan-honors-adf-after-container-start.md) | First scan honors ADF after container start | The first scan uses the selected feeder source reliably |
| [US-019](done/019-split-scan-batches-into-documents.md) | Split scan batches into documents (done) | One paper stack can become several separately reviewed and uploaded documents |
| [US-020](done/020-account-menu-and-profile-context.md) | Account menu and profile context (done) | Anonymous and authenticated identity information has one consistent home |
| [US-021](done/021-forget-a-saved-scanner.md) | Forget a saved scanner (done) | Retired scanners can be removed safely without blocking rediscovery |
| [US-022](022-scanner-operation-loading-states.md) | Scanner operation loading states | Slow scanner and API operations provide visible, blocking feedback |
| [US-023](done/023-configurable-application-http-port.md) | Configurable application HTTP port (done) | Host-network deployments can choose the application's listening port |
| [US-024](024-multiple-parallel-login-providers.md) | Multiple parallel login providers | Household members can choose among configured identity providers |
| [US-025](025-paperless-processing-hint-after-upload.md) | Paperless processing hint after upload | Accepted uploads clearly explain that Paperless processing may continue |
| [US-026](026-notify-scan-timeout-decision.md) | Notify scan timeout decision | Background users are called back when an active scan needs a timeout decision |
| [US-027](027-preserve-split-boundaries-after-page-removal.md) | Preserve split boundaries after page removal | Page removal keeps the intended document split valid and consistent |
| [US-028](028-apply-default-tags-to-split-documents.md) | Apply default tags to split documents | Every split document starts with an isolated snapshot of the session's default tags |
| [US-029](029-reset-workflow-after-final-upload.md) | Reset workflow after final upload | Returning to Scan after a completed batch starts with an empty workflow |

Every story is subject to the shared [Definition of Done](../definition-of-done.md). Explicitly deferred items are not implied by an acceptance criterion.

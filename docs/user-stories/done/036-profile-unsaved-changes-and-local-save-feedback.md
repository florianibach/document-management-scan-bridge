# US-036: Profile unsaved changes and local save feedback

## User story

**As a** profile owner editing settings, **I want** a clear global indication of unsaved changes and save results beside the action I used, **so that** I do not lose edits or scroll elsewhere to learn whether a save succeeded.

## Acceptance criteria

- Once any editable profile value differs from its last successfully loaded or saved value, a persistent global banner identifies that the current profile has unsaved changes.
- The dirty state covers all independently editable profile sections and all save actions on the profile/settings experience. Successfully saving one section clears only the changes persisted by that action; the banner remains while another section is still dirty.
- Every save button has an adjacent status region that reports its own in-progress, success, validation, and failure state without requiring scrolling. A result from one save action never appears only beside a different action or only at a distant page location.
- Save buttons and their feedback use clear labels or section context so users can tell which values each of the multiple actions persists. The implementation may consolidate save actions only if doing so does not silently broaden, discard, or partially persist edits.
- Successful feedback appears immediately after persistence is confirmed and remains available long enough to be perceived; making another edit replaces it with the unsaved state. Failures remain actionable until retried, dismissed, or made obsolete by a later successful save.
- Validation and save failures preserve the user's entered values and focus or link to the relevant field. Secret values and sensitive configuration are not repeated in banners, status text, URLs, logs, or accessibility announcements.
- Attempting to change profile, navigate within the app, reload, close the tab, or otherwise leave the editing context while any profile changes are unsaved requests confirmation before discarding them. Continuing the edit cancels navigation; confirmed departure performs the requested navigation without saving implicitly.
- The banner, button status regions, and departure confirmation are keyboard accessible, work at mobile and desktop widths, do not rely on color alone, and expose concise live-region announcements without duplicate or noisy announcements.
- Concurrent or stale-save handling does not falsely display success or clear the dirty state when the submitted values were not persisted.
- Component and end-to-end tests cover a single dirty section, multiple dirty sections with separate save buttons, partial save success, validation and server failure, retry, edit-after-success, profile switching, in-app and browser-level departure, confirmed discard, canceled navigation, responsive placement, focus behavior, and accessible announcements.

## Out of scope

- Automatic background saving.
- Changing profile authorization, configuration ownership, or the meaning of individual settings.
- Replacing detailed field-level validation with only a global message.

## Dependencies

- US-008
- US-011
- US-012
- US-017
- US-032

## Implementation and verification record

- The settings component compares the editable Paperless and defaults sections with their last loaded or successfully persisted snapshots. One general save action persists every dirty section, reports partial persistence explicitly if a later repository fails, and a response to an older submission cannot clear newer edits.
- A persistent, text-and-icon unsaved banner and action-local live regions report progress, success, validation errors, and operational failures. Starting a new action clears obsolete success messages so contradictory duplicate results cannot accumulate. Validation preserves entered values and moves focus to the relevant Paperless field or defaults form.
- Blazor's navigation lock covers browser reload/close and internal navigation. Internal navigation uses an explicit discard confirmation; cancellation prevents the route change and confirmation never saves implicitly. Account/profile departures use the same navigation path.
- Component coverage verifies a single dirty section, the general save across both dirty sections, removal of obsolete results, edit-after-success, actionable validation failure, secret-safe announcements, and compact multi-tag selection. Unit coverage verifies that a syntactically valid Paperless configuration can be persisted while the service is offline. The navigation lock is rendered from the same dirty predicate used by the banner; browser confirmation itself is native platform behavior.
- Responsive review: action/status pairs stack at phone widths and become two columns from 768 px; the sticky banner and semantic live regions use text and symbols in addition to color. Keyboard operation uses native buttons, fields, links, focus, and browser confirmation controls.

### Definition of Done disposition

- Acceptance criteria, validation, failure/retry, stale-save safety, accessibility, persistence confirmation, and automated component coverage are implemented. No automatic saving or authorization/configuration ownership changes were introduced.
- Application/workflow unit tests, external-boundary integration tests, database migrations, logs, cleanup, scanner hardware checks, and new Compose configuration are not applicable: this story changes only existing Blazor settings interaction and CSS, without changing those boundaries.
- The complete automated suite, Release build, repository validation, dependency vulnerability audit, container build, Compose validation/startup, and health endpoint are recorded in the pull request. Mobile and desktop layout behavior is encoded by the responsive CSS breakpoint and reviewed in the running application.
- Accepted limitation: browser reload/close prompts use the browser's native, intentionally non-customizable text. Automated component tests can assert that the navigation lock is armed; native browser chrome is covered by operational browser verification rather than bUnit.

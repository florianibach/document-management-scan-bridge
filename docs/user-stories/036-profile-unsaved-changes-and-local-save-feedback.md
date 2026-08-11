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

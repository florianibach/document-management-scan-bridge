# US-015: Guided scan and manual duplex states

## User story

**As a** user scanning paper documents, **I want** the Scan step to guide me through simplex and manual duplex states with safe decisions, **so that** I do not accidentally duplicate jobs, reorder pages, or lose recoverable work.

## Acceptance criteria

- Starting a simplex scan shows clear waiting, running, cancelled, timeout-decision, success, and failure states with large actions appropriate to each state.
- Running scans show real information such as pages received and current state; the UI does not show artificial percentage progress when the backend cannot provide it.
- While a scan job is active, the UI prevents duplicate scan starts from the same browser circuit or another visible action and explains that the current job is in progress.
- Long-running scans present a user decision to keep waiting or cancel, and cancellation communicates that partial scan data was removed when that is the backend behavior.
- Manual duplex mode is presented as a dedicated three-step guided flow: scan front sides, flip the stack, then scan back sides and merge.
- The flip-stack step includes scanner-orientation-specific instructions and a required user confirmation before scanning back sides; the UI never starts the second pass automatically.
- Users can mark the last back side as blank when supported by the workflow, and the resulting page ordering remains explicit before preview.
- If front-side and back-side counts cannot be reconciled, the UI blocks automatic merge, explains the mismatch, and offers restart or cancellation without corrupting existing completed documents.
- Browser notifications for scan completion or duplex flip prompts are offered from the Scan flow when supported, while clearly stating that the scan page must remain open.

## UI concept references

- Scan waiting, running, timeout, cancelled, and failed states.
- Manual duplex steps for front sides, stack flip confirmation, and back-side scanning.
- Page-count mismatch handling.
- Notification prompt and browser support states.

## Out of scope

- Automatic duplex scanning on hardware that supports duplex natively.
- Image analysis for detecting blank pages beyond the existing manual blank-last-back-side decision.

## Dependencies

- US-003
- US-004
- US-010
- US-013
- US-014

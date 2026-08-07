# US-013: Mobile-first navigation and workflow shell

## User story

**As a** Scan Bridge user near the scanner, **I want** a mobile-first application shell with a clear scan workflow stepper, **so that** I always know where I am, what comes next, and where administrative functions live.

## Acceptance criteria

- The application exposes the main areas Scan, Documents, Settings, and Status with the same information architecture on mobile and desktop.
- On mobile-sized viewports, the primary navigation is optimized for touch and keeps Scan as the obvious daily entry point; on desktop-sized viewports, the same destinations are available in an efficient sidebar or horizontal layout.
- The Scan area presents the workflow as the ordered steps Prepare, Scan, Review, PDF, and Send, and highlights the current step without implying progress that is not known.
- Technical and operational details such as health checks, scanner capabilities, mDNS hints, version information, and deployment diagnostics are removed from the daily scan path and are reachable from Status or Settings instead.
- The active profile or anonymous household profile is visible in the shell whenever the current mode allows the user to access protected areas.
- Empty, blocked, loading, success, warning, and error states use reusable cards with large primary actions, concise user-facing copy, and secondary technical details only when requested.
- Existing scan, settings, capability, and notification behavior remains reachable after the navigation change, with no regression in authorization or browser-circuit isolation behavior.

## UI concept references

- Mobile and desktop navigation with the areas Scan, Documents, Settings, and Status.
- Guided workflow steps: Prepare, Scan, Review, PDF, Send.
- Separation of daily use from administration and diagnosis.
- Visible safe states and profile context.

## Out of scope

- Replacing the underlying scan, PDF, upload, authentication, or persistence workflows.
- A complex design-system dependency beyond Bootstrap-compatible reusable components.

## Dependencies

- US-010
- US-011 for authenticated profile display and protected navigation behavior

## Completion evidence

- The responsive shell exposes Scan, Documents, Settings, and Status through the same links; CSS changes the desktop sidebar into a touch-sized mobile bottom bar without changing information architecture.
- The Scan page derives one current step from real workflow state and renders Prepare, Scan, Review, PDF, and Send without marking unknown steps complete.
- Scanner discovery moved to Settings. Health, build version, mDNS prerequisites, and capability-validation guidance are available on Status rather than in the daily scan path.
- The shell displays the resolved anonymous or authenticated profile and preserves the existing sign-out action and server-side authorization policy.
- `AppStateCard` provides empty, loading, success, warning, and error variants, optional large actions, and disclosure-only technical detail. Documents uses the explicit empty state until a later story adds a persisted list.
- Component coverage verifies the four destinations, profile context, workflow stepper, separation of mDNS details, scanner selection, and controlled HTTP fallback. Existing workflow, isolation, integration, and unit suites remain enabled and pass.
- Representative workflow evidence: the component suite covers scanner selection through existing scan, duplex, preview, PDF, and upload behaviors. Responsive rules were reviewed for mobile and desktop widths; Chromium screenshot capture was unavailable because this environment cannot run its snap package. No scanner hardware behavior changed in this shell-only story.
- Not applicable: persistence migrations, external-boundary integration changes, configuration variables, logging, cleanup/recovery, and target-printer firmware validation, because this story changes presentation and placement only. Container startup remains subject to the local Docker environment.
- Accepted follow-up: Documents intentionally shows a safe current-session explanation rather than introducing the session history planned outside this story; the more extensive diagnostics presentation remains US-017.

### Follow-up: focused workflow stages

The scan page now renders only the active stage's controls. Preparation choices disappear while scanning, reviewing, creating the PDF, or sending; the send and PDF stages provide explicit cancellation where an operation is active and a safe return to the review without discarding source pages. Component coverage verifies that preparation controls are absent after scanning and after PDF completion.

After Paperless accepts an upload, the send form is replaced by a final success state. It offers a clear return to the preparation start, a PDF download, and—when an effective HTTP(S) Paperless base URL is configured—a safe new-tab link to Paperless. Starting another document ignores the previous PDF state while retaining existing temporary artifacts according to the established recovery policy.

The generic “Täglicher Ablauf” eyebrow was removed because it did not add actionable context. Scanner-readiness messages are now scoped to preparation instead of persisting as a blue banner through every later stage. Cancellation records the affected session before awaiting the scanner process, suppresses stale timeout/running notifications for that session, and reports the eventual cancelled state instead.

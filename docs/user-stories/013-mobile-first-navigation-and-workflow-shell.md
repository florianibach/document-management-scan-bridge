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

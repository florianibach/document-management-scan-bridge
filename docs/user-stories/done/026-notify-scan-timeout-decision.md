# US-026: Notify users about a scan timeout decision

## User story

**As a** user who uses another tab or application during a longer scan, **I want** to be notified when Scan Bridge needs a timeout decision, **so that** I can return to the scan in time and choose to keep waiting or continue scanning.

## Acceptance criteria

- When the configurable scan timeout expires and the still-running scan requires a user decision, Scan Bridge triggers a browser/system notification.
- The notification explains that the scan is still running and that the user needs to return to Scan Bridge.
- Clicking the notification focuses or opens the existing scan context.
- The notification is triggered only when notifications were explicitly enabled beforehand and the browser granted permission.
- If browser permission is denied or notifications are unsupported, the visible timeout decision remains fully operable in the application.
- Repeated state events, reconnects, or rerenders do not create duplicate notifications within the same timeout event.
- If the user chooses to continue and a later timeout interval expires, a new notification may be created for that event.
- Cancellation, completion, or starting a new scan invalidates the previous timeout decision.
- Notifications and actions remain isolated to the affected browser tab, user, and scan workflow.
- Automated tests cover granted, denied, and unsupported notifications; deduplication; a second timeout after continuation; cancellation; and successful completion.

## Out of scope

- Notifications for closed browser sessions or workflows other than the timeout decision of the active scan.

## Dependencies

- US-010
- US-015

## Completion evidence

- Each timeout decision receives a monotonically increasing number in the circuit-scoped simplex workflow. The browser event key combines the scan session and that number, so reconnects and rerenders deduplicate one decision while a later interval remains independently notifyable.
- The timeout notification explicitly says that the scan is still running and asks the user to return. The existing service worker focuses or opens the Scan Bridge context when it is clicked.
- Notification delivery remains behind the explicit, tab-local `sessionStorage` opt-in and granted browser permission introduced by US-010. Unsupported or denied notification APIs return without delivery; the in-page keep-waiting and cancel controls are unchanged.
- Workflow and component tests cover decision numbering, a second interval, cancellation, completion, distinct event keys, and the return-to-application message. Existing US-010 tests and settings component tests cover explicit opt-in, denied and unsupported UI states, and successful notification delivery.

## Definition of Done review

- No persistence, database migration, external HTTP boundary, Compose variable, dependency, or container packaging changed; corresponding integration and configuration documentation work is not applicable.
- Cancellation and successful completion replace the decision state, and starting a new scan creates a new session identifier, invalidating the earlier event context without sharing state across tabs, users, or circuits.
- Browser/operating-system notification appearance and click behavior require a real supported browser and cannot be fully asserted by bUnit. This remains the accepted US-010 platform limitation; the service-worker behavior and safe in-app fallback are unchanged.
- Scanner hardware verification is not applicable because this story changes notification identity and presentation after an existing workflow transition, not scanner capture or page ordering.

## Accepted limitations

- Notifications require the active browser session to remain open. Server-originated Web Push for closed sessions remains out of scope.

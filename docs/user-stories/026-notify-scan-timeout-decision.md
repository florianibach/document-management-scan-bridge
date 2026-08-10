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

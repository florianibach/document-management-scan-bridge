# US-010: Browser notifications and isolated scan control

## User story

**As a** user who may leave the scan page in the background, **I want** browser notifications and an isolated workflow, **so that** I can react to a stack-flip request without seeing or controlling another browser's scan.

## Acceptance criteria

- Notification permission is requested only after an explicit user action.
- The browser reports flip requests, timeout decisions, completed scans, count mismatches, and failures.
- A reconnect or repeated state event does not deliver the same notification twice in one browser tab.
- Notification preference and deduplication state live in browser `sessionStorage`, not in an application singleton.
- Simplex and manual-duplex coordinators are scoped to the interactive browser circuit; independently connected browsers never share `Current`, confirmation, or cancellation state.
- Unsupported and denied browser permissions have actionable UI states.

## Completion evidence

- `scanNotifications.js` owns permission, browser-tab preference, service-worker delivery, and event-key deduplication; `scanNotificationServiceWorker.js` focuses the existing scan page when a notification is clicked.
- `Home.razor` maps relevant simplex and duplex transitions to notifications and safely handles a disconnected Blazor circuit.
- Both workflow registrations are scoped. Temporary scan pages and the running operating-system process necessarily remain server-side, but no global singleton contains the user's workflow state.
- Component tests cover explicit opt-in and delivery after completion; the existing workflow tests continue to cover scanning, cancellation, and duplex ordering.

## Accepted limitations

- Notifications are displayed through a service worker when an open scan tab is in the background. A completely closed page would additionally require a server-originated Web Push subscription endpoint.
- Interactive Server keeps the active coordinator for the lifetime of its SignalR circuit. Multi-node deployments must retain normal Blazor circuit affinity while a scan runs; this change removes cross-browser global state, but does not migrate a running scanner process between hosts.
- Browser notification appearance and operating-system policy require manual verification in a real browser.

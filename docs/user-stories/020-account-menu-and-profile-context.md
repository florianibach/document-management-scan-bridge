# US-020: Account menu and profile context

## User story

**As a** Scan Bridge user, **I want** a consistent account control in the upper-right application shell, **so that** I can recognize the active profile mode and find identity-related information and actions.

## Acceptance criteria

- The application shell displays an accessible account control at the upper right on desktop and in an equivalent prominent shell location on mobile.
- In anonymous mode, the control identifies the session as anonymous and opens a menu or panel explaining that the household profile and its settings are shared.
- In authenticated mode, the control uses the provider profile image when a safe image claim is available and otherwise displays initials or a neutral profile icon.
- Opening the authenticated control shows the available display name and provider context plus the existing sign-out action, without exposing tokens, immutable subject identifiers, or unnecessary claims.
- Identity details and actions reflect authentication changes without requiring stale page state to be reused.
- The control supports keyboard operation, visible focus, an accessible name, outside-click or Escape dismissal, and touch-friendly targets.
- Protected navigation and existing authorization behavior remain unchanged; the menu never represents an unauthenticated visitor as signed in.

## Out of scope

- Editing identity-provider profile data or uploading a Scan Bridge-specific avatar.
- Linking identities from different login providers.

## Dependencies

- US-011
- US-013

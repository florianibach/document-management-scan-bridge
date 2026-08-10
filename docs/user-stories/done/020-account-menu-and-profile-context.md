# US-020: Account menu and profile context

## User story

**As a** Scan Bridge user, **I want** a consistent account control in the upper-right application shell, **so that** I can recognize the active profile mode and find identity-related information and actions.

## Acceptance criteria

- The application shell displays an accessible account control at the upper right on desktop and in an equivalent prominent shell location on mobile.
- In anonymous mode, the control identifies the session as anonymous and opens a menu or panel explaining that the household profile and its settings are shared.
- In authenticated mode, the control uses the provider profile image when a safe image claim is available and otherwise displays initials or a neutral profile icon.
- Opening the authenticated control shows the available display name plus the existing sign-out action, without exposing provider boilerplate, tokens, immutable subject identifiers, or unnecessary claims.
- Identity details and actions reflect authentication changes without requiring stale page state to be reused.
- The control supports keyboard operation, visible focus, an accessible name, outside-click or Escape dismissal, and touch-friendly targets.
- Protected navigation and existing authorization behavior remain unchanged; the menu never represents an unauthenticated visitor as signed in.

## Out of scope

- Editing identity-provider profile data or uploading a Scan Bridge-specific avatar.
- Linking identities from different login providers.

## Dependencies

- US-011
- US-013

## Implementation and verification

- The shared shell uses one labelled account button at its upper-right edge on desktop and mobile. Its responsive sizing, visible focus ring, and touch target are defined in the component stylesheet.
- Anonymous mode identifies the shared household profile and explains that its settings are shared. Authenticated mode shows only the resolved display name, an HTTPS profile image when safely usable (otherwise initials), and the existing local sign-out action. Provider boilerplate is omitted because it adds no useful context.
- The component observes `AuthenticationStateProvider.AuthenticationStateChanged`, closes an open menu when identity changes, and explicitly renders a signed-out state rather than reusing authenticated details.
- The button exposes `aria-haspopup` and `aria-expanded`; the panel has menu semantics. A colocated JavaScript module dismisses it for outside pointer input and Escape, while the native button and form retain keyboard operation.
- Component tests cover anonymous disclosure, authenticated safe-image/provider/sign-out rendering, suppression of subject/token claims, unsafe-image fallback, and the authenticated-to-signed-out transition. The complete automated suite, local runtime smoke check, responsive screenshots, and any container-environment limitation are recorded in the pull request.

## Definition of Done notes

- Validation and safe fallback apply to provider image claims. Retry, cancellation, persistence, cleanup, database integration, scanner hardware, PDF/Paperless integration, and new logging are not applicable because this story changes only transient shell presentation.
- No configuration, Compose variable, dependency, credential storage, or authorization boundary changes were introduced. Protected routes continue to use the existing fallback authorization policy.
- Mobile and desktop layout behavior is implemented with the existing `640.98px` shell breakpoint. No accepted product limitation or follow-up remains for this story.

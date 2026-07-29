# US-001: Establish the project foundation

## User story

**As a** developer, **I want** a runnable and testable application scaffold, **so that** product capabilities can be added on stable architectural and deployment foundations.

## Acceptance criteria

- An ASP.NET Core Blazor Server solution starts and displays a mobile-first Bootstrap shell.
- Projects separate the web UI, application/workflow logic, infrastructure adapters, and automated tests without adding speculative product behavior.
- Typed configuration boundaries exist for scanner, Paperless-ngx, persistence, and temporary-file settings; example configuration contains no secrets.
- A replaceable scanner interface and its process-execution boundary are defined, but no real scan workflow is implemented.
- SQLite persistence is wired for future profiles and settings, including an initial migration or equivalent reproducible schema setup.
- Unit, integration, and component-test projects contain passing smoke tests and are executed by the repository build workflow.
- A multi-stage `Dockerfile` builds the application, and Docker Compose starts it with documented local settings and persistent data storage.
- The standard build, test, container-build, and Compose-validation commands work from a clean checkout.

## Notes and constraints

- Prefer current supported .NET and first-party framework facilities unless a dependency provides clear value.
- Keep the UI lightweight and build on Bootstrap responsiveness rather than introducing another design system.
- Support a path toward Linux ARM64 deployment, but do not claim hardware validation in this story.

## Out of scope

- Scanner discovery or invoking `scanimage`.
- Page workflows, PDF processing, Paperless-ngx uploads, or profile screens.
- Production authentication, image publishing, and automated deployment.

## Dependencies

None. Use the repository skill at `.codex/skills/scaffold-blazor-scan-bridge/SKILL.md` when implementing this story.

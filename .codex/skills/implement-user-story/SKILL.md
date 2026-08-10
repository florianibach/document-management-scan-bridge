---
name: implement-user-story
description: Implement or revise any paperless-scan-bridge user story from acceptance criteria through Definition of Done, including scoped architecture, tests, operational validation, documentation, review evidence, and roadmap completion. Use for product implementation work tied to a story in docs/user-stories; derive story-specific technology and verification from the selected story rather than assuming scanner discovery or another fixed feature.
---

# Implement a User Story

## Establish scope

1. Identify the requested story and read it from `docs/user-stories/` or `docs/user-stories/done/` together with `docs/definition-of-done.md`, `README.md`, applicable `AGENTS.md` files, and the affected code.
2. Trace every acceptance criterion to planned code, tests, documentation, or an explicit verification record.
3. Respect dependencies and out-of-scope statements. Do not implement later-story behavior merely because an interface already exists.
4. Inspect existing conventions and preserve intentional architectural, framework, and version choices.

## Implement behavior

1. Keep domain and workflow logic in the application layer. Put process, database, HTTP, and file-system integrations behind explicit boundaries rather than in Razor components.
2. Use typed, validated configuration for deployer-controlled values. Never embed environment-specific identifiers, credentials, or secrets in code.
3. Implement relevant validation, cancellation, retries, empty states, timeouts, cleanup, and recovery. Provide actionable diagnostics without sensitive metadata or document content.
4. Update dependency injection, persistence migrations, container packaging, Compose configuration, and logs only when required by the selected story. When Compose variables are added, renamed, repurposed, or made operator-facing, document their names, defaults, meanings, secret handling, and validation commands in `README.md`.
5. Keep mobile-first UI behavior accessible and consistent with the existing component and styling system.
6. Use English for user-facing text by default, including anonymous and authenticated states, validation messages, accessible names, tests, and documentation examples. Preserve another language only when the selected story or an established localization resource explicitly requires it; do not introduce mixed-language UI.

## Verify the change

1. Add focused unit tests for application and workflow logic, integration tests for changed external boundaries, and component tests for meaningful Blazor behavior.
2. Verify a representative end-to-end workflow. Check affected screens at representative mobile and desktop viewports; keep generated binary artifacts local unless the review platform explicitly supports them.
3. Run all repository-documented checks, including locked restore, Release build, the complete test suite, repository validation, dependency vulnerability checks, container build, Compose validation, Compose startup, health checks, and changed skill validation.
4. If a required executable is missing, install it and rerun the check. Treat a failed or interrupted run only as an intermediate state. When the environment lacks a required capability rather than a tool, record the exact limitation and still run every independent check.
5. Perform hardware-dependent checks on the target device when the story requires them. Record the model, firmware, commands, results, and any network prerequisites; never claim unperformed hardware verification passed.

## Complete the story

1. Review the full diff for secrets, unintended scope, binary artifacts, disabled tests, warnings, and unaddressed findings.
2. Map every acceptance criterion and applicable Definition of Done item to evidence. Mark non-applicable items with a reason and record accepted limitations or follow-up work.
3. Move the story into `docs/user-stories/done/` and update roadmap links only after all required completion conditions are satisfied.
4. Commit the finished change and prepare the pull request with a concise summary, test evidence, and explicit environmental or hardware limitations.

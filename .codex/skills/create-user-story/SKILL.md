---
name: create-user-story
description: Create new paperless-scan-bridge product user stories as repository-ready Markdown and add them to the roadmap. Use when requirements, feature ideas, bugs, or operational needs must be turned into new files under docs/user-stories; clarify ambiguity before writing and do not use this skill to implement the story.
---

# Create a User Story

## Establish the requirement

1. Read `docs/user-stories/README.md`, `docs/definition-of-done.md`, related stories in both the active and `done/` directories, applicable `AGENTS.md` files, and relevant code or documentation.
2. Separate the user's desired outcome from a proposed implementation. Research referenced repositories, protocols, or documentation when they affect feasible acceptance criteria.
3. Ask focused questions about any ambiguity that could materially change scope, observable behavior, security, compatibility, dependencies, or story boundaries. Do not silently invent an answer. Proceed only after blocking questions are answered; explicitly preserve implementation freedom where the user has delegated a choice.
4. Choose the next unused three-digit `US-` identifier across active and completed stories. Prefer one independently valuable story per outcome; combine requirements only when they share one user outcome and cannot be accepted meaningfully in isolation.

## Write the story

1. Create `docs/user-stories/<id>-<concise-kebab-title>.md` in English, matching the established structure:
   - `# US-NNN: Title`
   - `## User story` with **As a**, **I want**, and **so that**
   - `## Acceptance criteria` with observable, testable bullets
   - `## Out of scope`
   - `## Dependencies`
   - Add a narrowly relevant section such as superseded behavior only when needed.
2. State outcomes rather than prescribing internal design unless the chosen mechanism is itself a requirement. Include failure and recovery behavior, security and secret handling, accessibility, operational documentation, compatibility, and automated or hardware verification when applicable.
3. Resolve overlap explicitly. Reference or revise an existing pending story rather than creating contradictory acceptance criteria. Never mark a new story done and never implement it as part of story creation.
4. Add the story to `docs/user-stories/README.md` in numeric order with a concise outcome. Preserve the shared Definition of Done rather than duplicating it wholesale.

## Validate the result

1. Check that every user-confirmed requirement is represented and that no unanswered choice has become an accidental requirement.
2. Check IDs, filenames, headings, links, dependencies, terminology, and Markdown formatting with repository validation.
3. Review the diff for unsupported claims, hidden implementation scope, contradictions, secrets, and unrelated changes.

---
name: scaffold-blazor-scan-bridge
description: Scaffold the paperless-scan-bridge ASP.NET Core Blazor Server solution and its architectural, test, SQLite, Docker, and Compose foundations. Use when implementing or revising US-001 project scaffolding; do not use it to implement scanning, document processing, or Paperless-ngx product workflows.
---

# Scaffold Blazor Scan Bridge

## Establish scope

1. Read `docs/user-stories/001-project-scaffolding.md`, `docs/definition-of-done.md`, `README.md`, and the build workflow.
2. Inspect the repository before selecting versions or structure; preserve newer intentional choices.
3. Treat later-story interfaces as seams only. Do not implement scanner discovery, scanning, duplex logic, previews, PDF creation, upload, or profile UI.

## Build the foundation

1. Select the current supported .NET SDK compatible with CI and pin it for reproducible local builds.
2. Create a solution with clear web, application/domain, infrastructure, and test boundaries. Keep dependencies directed inward; keep process, database, HTTP, and file-system concerns out of UI components.
3. Add a mobile-first Blazor Server shell based on Bootstrap. Limit it to navigation, layout, status/error presentation primitives, and a clear planning-state landing page.
4. Define typed, validated configuration for scanner, Paperless-ngx, SQLite, and temporary storage. Commit safe examples only; use environment variables or user secrets for credentials.
5. Define the smallest scanner and process-runner abstractions that US-002 will need. Use cancellable asynchronous APIs and structured results; provide no fake production scan behavior.
6. Wire SQLite through a migration-capable persistence layer for future settings. Do not invent profile fields belonging to US-008.
7. Add unit, integration, and Blazor component test projects with meaningful foundation smoke tests.
8. Add a non-root, multi-stage container build and Compose setup with persistent application data, writable temporary storage, environment-based configuration, and an ARM64-compatible path.

## Verify completion

Run all repository-documented checks, including:

```bash
dotnet restore --locked-mode
dotnet build --configuration Release --no-restore
dotnet test --configuration Release --no-build
docker build --tag paperless-scan-bridge:local .
docker compose config --quiet
docker compose up --detach --build
```

Verify the health or landing endpoint, inspect container logs, then stop the stack with `docker compose down`. Run documentation and skill validation if files in those areas change.

Before declaring the story complete, map every US-001 acceptance criterion and applicable Definition of Done item to evidence. Record any hardware or ARM64 checks not performed; never represent a skipped environmental check as passing.

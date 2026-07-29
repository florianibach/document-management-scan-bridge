# paperless-scan-bridge

`paperless-scan-bridge` is a planned, small, self-hosted, mobile-first ASP.NET Core Blazor Server application for initiating scans on an HP network multifunction printer and sending the resulting documents to [Paperless-ngx](https://docs.paperless-ngx.com/).

The guided workflow will support simplex scans and manual duplex scans, put pages into reading order, offer lightweight preview and editing, create a PDF, and upload it with metadata. The application is intended to run in Docker on a Raspberry Pi or another always-on host.

> [!NOTE]
> This repository is in its planning phase. No product story has been implemented yet, and technical choices may change as scanner and deployment constraints are validated.

## Product documentation

- [User stories](docs/user-stories/README.md)
- [Definition of Done](docs/definition-of-done.md)

The stories are ordered as an initial implementation roadmap. Their acceptance criteria define scope; the shared Definition of Done describes the quality bar that applies to every story.

## Proposed architecture

```text
Mobile browser
    │
    ▼
Blazor Server UI
    │
    ▼
Workflow/application services
    ├──► Scanner adapter ──► sane-airscan / scanimage ──► HP network MFP
    ├──► Preview, page editing, and PDF assembly
    ├──► SQLite
    └──► Paperless-ngx REST API
```

The MVP deliberately excludes multi-tenant authentication, custom OCR tuning, and document-management targets other than Paperless-ngx.

## Continuous integration

The [GitHub Actions build workflow](.github/workflows/build.yml) validates documentation immediately. As the first story adds the solution and container files, the same workflow automatically starts running .NET build/test, Docker build, and Compose validation checks.

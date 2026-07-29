# paperless-scan-bridge

`paperless-scan-bridge` is a small, self-hosted, mobile-first ASP.NET Core Blazor Server application for initiating scans on an HP network multifunction printer and sending the resulting documents to [Paperless-ngx](https://docs.paperless-ngx.com/).

The guided workflow will support simplex scans and manual duplex scans, put pages into reading order, offer lightweight preview and editing, create a PDF, and upload it with metadata. The application is intended to run in Docker on a Raspberry Pi or another always-on host.

The project foundation is runnable; scan behavior remains intentionally unavailable until the following stories are implemented.

## Local development

Install the .NET SDK version selected by `global.json`, then run:

```bash
dotnet restore --locked-mode
dotnet build --configuration Release --no-restore
dotnet test --configuration Release --no-build
dotnet run --project src/PaperlessScanBridge.Web
```

Open the URL printed by the application. The health endpoint is `/health`. SQLite data and temporary files are written to ignored `data/` and `temp/` directories.

## Container operation

```bash
docker compose up --detach --build
curl --fail http://localhost:8080/health
docker compose logs scan-bridge
docker compose down
```

The named volumes `bridge-data` and `bridge-temp` keep persistent application data and writable temporary storage outside the container layer. Override `PAPERLESS_URL` and `PAPERLESS_TOKEN` through the environment; never commit the token. The multi-architecture Microsoft base images and architecture-neutral build provide an ARM64 deployment path, but this story has not been validated on ARM64 hardware.

Configuration uses standard ASP.NET Core keys:

| Section | Purpose | Container override example |
| --- | --- | --- |
| `Scanner` | Future scanner executable and timeout | `Scanner__Command=scanimage` |
| `Paperless` | Future service URL and secret token | `Paperless__ApiToken=...` |
| `Persistence` | SQLite connection | `Persistence__ConnectionString=Data Source=/app/data/bridge.db` |
| `TemporaryStorage` | Writable working directory | `TemporaryStorage__Path=/app/temp` |

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

The [GitHub Actions build workflow](.github/workflows/build.yml) validates documentation, restores locked dependencies, builds and tests the solution, builds the container, and validates Compose.

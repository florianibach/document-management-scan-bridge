# paperless-scan-bridge

`paperless-scan-bridge` is a small, self-hosted, mobile-first ASP.NET Core Blazor Server application for initiating scans on an HP network multifunction printer and sending the resulting documents to [Paperless-ngx](https://docs.paperless-ngx.com/).

The guided workflow will support simplex scans and manual duplex scans, put pages into reading order, offer lightweight preview and editing, create a PDF, and upload it with metadata. The application is intended to run in Docker on a Raspberry Pi or another always-on host.

The project can discover and inspect a SANE-compatible network scanner. Starting a scan remains intentionally unavailable until US-003.

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

The image installs `sane-utils` and `sane-airscan` for both AMD64 and ARM64. The named volumes `bridge-data` and `bridge-temp` keep persistent application data and writable temporary storage outside the container layer. Override `PAPERLESS_URL` and `PAPERLESS_TOKEN` through the environment; never commit the token.

Scanner discovery uses mDNS and WSD. The Docker host and scanner must be on a network where multicast DNS (UDP 5353) and the scanner's eSCL/WSD traffic are reachable. If bridge networking blocks multicast, configure the exact identifier returned by `scanimage -L` through `SCANNER_DEVICE_ID`; never add it to source code. Run `docker compose exec scan-bridge scanimage -L` to diagnose discovery.

Configuration uses standard ASP.NET Core keys:

| Section | Purpose | Container override example |
| --- | --- | --- |
| `Scanner` | Executable, timeout, and optional selected device | `SCANNER_DEVICE_ID=airscan:e0:...` |
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

## Scanner hardware verification

The controlled fixtures verify the option shapes emitted by an HP OfficeJet through `sane-airscan`: Flatbed/ADF sources, Color/Gray modes, 75–600 dpi, and A4/Letter/Legal geometry. The physical target printer's exact model and firmware were not available in this development environment and must be recorded before milestone acceptance. On that printer, record the model/firmware from its status page and retain the output of:

```bash
scanimage -L
scanimage --help --device-name "$SCANNER_DEVICE_ID"
```

This is the only outstanding hardware-dependent verification; discovery, parsing, error handling, UI behavior, and the real OS process boundary are covered locally with controlled doubles.

## Continuous integration

The [GitHub Actions build workflow](.github/workflows/build.yml) validates documentation, restores locked dependencies, builds and tests the solution, builds the container, and validates Compose.

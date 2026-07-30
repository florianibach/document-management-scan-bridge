# paperless-scan-bridge

`paperless-scan-bridge` is a small, self-hosted, mobile-first ASP.NET Core Blazor Server application for initiating scans on an HP network multifunction printer and sending the resulting documents to [Paperless-ngx](https://docs.paperless-ngx.com/).

The guided workflow will support simplex scans and manual duplex scans, put pages into reading order, offer lightweight preview and editing, create a PDF, and upload it with metadata. The application is intended to run in Docker on a Raspberry Pi or another always-on host.

The project discovers eSCL/AirScan scanners directly with DNS-SD/mDNS, validates a selected device through its `ScannerCapabilities` endpoint, and then exposes it to the SANE adapter. The selected scanner can capture simplex documents from a touch-friendly screen using the platen or automatic document feeder. Each job is written to its own temporary session directory; later stories add preview, editing, PDF creation, and upload.

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

For a local image, pass the current commit so the running version is visible in the page header and OCI image metadata:

```bash
export GIT_COMMIT="$(git rev-parse --short HEAD)"
docker compose up --detach --build
```

The image installs `sane-utils` and `sane-airscan` for both AMD64 and ARM64. Compose uses the Linux host network so multicast scanner discovery reaches the container; port `8080` is therefore opened directly by the application rather than published through Docker. The bind mounts `./app/data:/app/data` and `./app/temp:/app/temp` keep the selected scanner, generated SANE configuration, and temporary storage directly accessible on the host. Override `PAPERLESS_URL` and `PAPERLESS_TOKEN` through the environment; never commit the token.

No UID or GID configuration and no manual directory creation are required. Like many third-party images, the container starts through a small entrypoint: it creates the mounted directories when necessary, gives the image's built-in unprivileged .NET user access, and then immediately drops root privileges before starting the application. `APP_UID` is an internal variable supplied by Microsoft's ASP.NET base image, not a setting that users need to provide in Compose.

`/app/data/dataprotection-keys` is a directory created by the application at startup; it is expected not to exist on the first run. The entrypoint only prepares its parent bind mount so that the unprivileged application can create it.

The image keeps SANE's package-standard `/etc/sane.d` configuration directory. `/etc/sane.d/airscan.conf` is a symbolic link to the generated, persistent `/app/data/sane.d/airscan.conf`; the adjacent package-managed `dll.conf` and `dll.d/airscan` registration therefore remain available without relying on a custom configuration search path. Because the application performs DNS-SD itself and writes a validated static device, the generated `[options]` section sets `discovery = disable`. This prevents sane-airscan from starting its separate Avahi/WSD discovery and makes it use only the configured endpoint. `airscan-discover` still requires Avahi, but it is not part of the application workflow.

Scanner discovery is implemented in .NET with DNS-SD queries for `_uscan._tcp.local.` and `_uscans._tcp.local.`. It does not execute `airscan-discover` and does not require an Avahi daemon. The Linux Docker host and scanner must be on the same network, with multicast DNS (UDP 5353) and the scanner's eSCL traffic permitted by the host firewall. Select a result in the web UI; the server validates `<advertised eSCL URL>/ScannerCapabilities`, stores only a validated selection in SQLite, and atomically writes `airscan.conf`. URLs submitted by the browser are never accepted.

Discovery and selection are also exposed for operational diagnostics through `GET /api/scanners`, `POST /api/scanners/{discoveryId}/select`, and `GET /api/scanners/selected`. A discovery ID expires after five minutes. Duplicate HTTP and HTTPS advertisements are combined with HTTPS preferred, and the UI reports multicast timeouts, empty results, validation errors, and duplicate advertisements.

When one physical scanner advertises both protocols, HTTPS is validated first. If—and only if—HTTPS fails because the device certificate is untrusted or does not match its IP address, the server validates the matching DNS-SD-advertised HTTP endpoint. It never disables certificate validation and never accepts a fallback URL from the browser. Timeouts, invalid XML, invalid capabilities, and other failures do not trigger a downgrade.

`docker compose logs --follow scan-bridge` reports when the .NET Zeroconf backend starts, which DNS-SD service types it queries, advertisement and unique-scanner counts, eSCL validation, persistence, generated sane-airscan configuration, and later `scanimage` discovery/capability inspection. Scanner document data and API tokens are never logged.

ASP.NET Core data-protection keys are persisted under `/app/data/dataprotection-keys` in the existing data volume. This prevents antiforgery cookies from becoming unreadable after ordinary container recreation. The cookie name was also versioned so a token created by an older image is ignored once during this upgrade instead of producing repeated decryption errors.

Host networking is supported by the intended Linux/Raspberry Pi deployment. On Docker Desktop, enable host networking in Docker Desktop settings or run the application directly with `dotnet run` for scanner discovery.

Configuration uses standard ASP.NET Core keys:

| Section | Purpose | Container override example |
| --- | --- | --- |
| `Scanner` | Executable, short discovery timeout, long scan-job timeout, and optional selected device | `SCANNER_SCAN_TIMEOUT_SECONDS=1800` |
| `ScannerDiscovery` | mDNS/validation timeouts and managed SANE configuration | `ScannerDiscovery__TimeoutSeconds=5` |
| `Paperless` | Future service URL and secret token | `Paperless__ApiToken=...` |
| `Persistence` | SQLite connection | `Persistence__ConnectionString=Data Source=/app/data/bridge.db` |
| `TemporaryStorage` | Writable working directory | `TemporaryStorage__Path=/app/temp` |
| `DataProtectionStorage` | Persistent ASP.NET Core encryption keys | `DataProtectionStorage__Path=/app/data/dataprotection-keys` |
| `Build` | Visible source revision | `Build__Commit=abc1234` |

## Simplex scanning

Scanning is the primary action and therefore appears first on the start page. The application caches the last successfully inspected SANE device identifier, input sources, and resolutions in SQLite. Selecting a known scanner uses this cache immediately and does not run `scanimage -L`. Use **Scannerwerte aktualisieren** when the device, network, container, or SANE configuration changed; that explicit refresh shows an activity indicator, discovers the live device, and replaces the cached values. A newly discovered scanner is inspected once to populate its cache.

A platen job captures one page; an ADF job continues until the feeder is empty. The page reports queued, running, completed, cancelled, and failed states and disables duplicate submission while a scan is active. Scanner discovery and capability inspection display an activity indicator while `scanimage` is running. Cancellation terminates the underlying process when supported.

The source, color mode, and resolution are rendered with an explicit selected option during the initial server response. Consequently the value visible immediately after an application start is the value submitted to `scanimage`; selecting ADF captures the complete feeder batch without requiring the source to be toggled once first.

`Scanner:TimeoutSeconds` limits short discovery/capability commands. `Scanner:ScanTimeoutSeconds` is instead a 1,800-second (30-minute) user-confirmation interval: when it expires, the still-running process is left intact and the UI asks whether the scanner is still working. “Weiter warten” resets that interval; “Scan jetzt abbrechen” cancels the process and cleans the session. `Scanner:MaximumScanDurationSeconds` remains a final 14,400-second (four-hour) safety boundary for abandoned jobs. Compose exposes these as `SCANNER_SCAN_TIMEOUT_SECONDS` and `SCANNER_MAXIMUM_SCAN_DURATION_SECONDS`.

The application stores complete PNG pages under `<TemporaryStorage:Path>/<session-id>/`. A cancelled, timed-out, failed, or empty scan removes its entire session, including partial files. Session identifiers and page counts may appear in logs, but scanner output, document content, command stderr, and file names are not logged. These files are deliberately not exposed over HTTP and are consumed only by the later preview/PDF stories.

## Manual duplex scanning

For a two-sided document in a simplex ADF, choose **Manuellen Duplex-Scan starten**. Duplex always uses the scanner's available ADF source, even when the simplex source selector currently shows the platen; the selected color mode and resolution are applied unchanged to both passes and are summarized above the start button. The application first captures every front, then stops and displays a touch-friendly, numbered stack-flip instruction. It cannot start the second pass until **Stapel liegt richtig – Rückseiten scannen** is pressed. Keep the stack together and do not change its order while flipping it.

The HP Color Laser MFP 179fnw's verified feeder behavior returns the flipped back-side pass in reverse reading order. The workflow reverses that pass and alternates it with the fronts. If the physical document has an odd number of printed pages, select **Die allerletzte Rückseite des Dokuments ist leer** before confirming. A scanner-returned blank first image is then omitted; scanners that suppress that blank are supported as well. Other unequal pass counts stop at a resolution screen instead of guessing: check the stack and restart both passes.

The active two-pass coordinator lives for the application lifetime on this deliberately single-user appliance. A browser refresh or temporary Blazor reconnect therefore shows the existing stage and never submits another pass. Cancelling or restarting removes all partial session data. Ordered PNG pages remain private under `<TemporaryStorage:Path>/<session-id>/ordered/`; preview, editing, and PDF output remain deferred to later stories.

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

The target HP Color Laser MFP 179fnw was verified to return HTTP 200 and a valid eSCL `ScannerCapabilities` document from `/eSCL/ScannerCapabilities`. It reports platen and simplex ADF inputs, black-and-white/grayscale/RGB modes, 100/200/300 dpi profiles, and a maximum optical resolution of 600 dpi. Its HTTPS advertisement uses a certificate that is not trusted for its IP address, while its matching HTTP advertisement is usable; this is handled by the controlled fallback described above. Serial numbers and device UUIDs are intentionally not retained. Record the firmware from its status page before milestone acceptance and retain the output of:

```bash
scanimage -L
scanimage --help --device-name "$SCANNER_DEVICE_ID"
SANE_DEBUG_DLL=255 scanimage -L 2>&1 | grep -i airscan
readlink -f /etc/sane.d/airscan.conf
```

This is the only outstanding hardware-dependent verification; discovery, parsing, error handling, UI behavior, and the real OS process boundary are covered locally with controlled doubles.

## Continuous integration

The [GitHub Actions build workflow](.github/workflows/build.yml) validates documentation, restores locked dependencies, builds and tests the solution, builds the container, and validates Compose.

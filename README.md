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
| `Paperless` | Service URL, secret API token, and HTTP timeout | `Paperless__ApiToken=...` |
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

The active two-pass coordinator belongs to the current Blazor browser circuit rather than to an application-wide singleton. Independently connected browsers therefore have separate status, flip confirmation, and cancellation controls. A temporary SignalR reconnect retains the circuit; a full reload intentionally starts a new browser circuit and cancels its abandoned in-process scan. Temporary pages and the scanner process remain server-side because a browser cannot safely execute or own `scanimage`. In a multi-node deployment, keep standard Blazor circuit affinity for the duration of an active scan; a running physical scanner process cannot migrate between hosts.

## Preview and page editing

After a successful simplex or manual-duplex scan, the active browser circuit shows every page in its current reading order. Responsive thumbnails use native lazy loading so large batches do not block the mobile screen. A page can be rotated clockwise in 90-degree steps or removed only through a separate confirmation action; page numbers are recalculated immediately.

These edits are non-destructive session metadata: original PNG files retain their scan quality and are not changed or deleted. An unreadable or corrupt PNG is marked individually, while the remaining pages stay editable. Reloading the page intentionally discards the active edit state; persistence and final PDF application belong to the following stories.

## PDF creation

After reviewing the pages, select **PDF erstellen**. The application uses the visible page order and applies every 90-degree rotation and deletion to one final PDF. Unavailable or corrupt pages block creation with a recoverable message. The default keeps the original PNG image data lossless and sizes each PDF page at the scan-oriented default of 300 dpi; OCR, PDF/A, signatures, and searchable text are deliberately not added.

The completed file is written as `<session>/document.pdf.partial`, closed, and then atomically renamed to `document.pdf`, so the download endpoint can never publish a partial result. Repeating creation replaces the previous complete PDF. Success removes the partial file; cancellation and failure also remove it while retaining the original scan pages and non-destructive edit state for retry. The complete PDF and source pages remain together in temporary session storage until a later workflow deletes the session or the deployer clears the configured `TemporaryStorage` volume. This retention is intentional recovery behavior for US-006; automated age-based retention belongs to deployment hardening.

## Paperless-ngx einrichten und hochladen

1. In Paperless-ngx anmelden, rechts oben das Benutzermenü öffnen und **Mein Profil** wählen.
2. Im Bereich **API-Authentifizierung** mit dem runden Pfeil einen Token erzeugen beziehungsweise erneuern und den angezeigten Wert kopieren. Ein erneuerter Token macht den bisherigen Token ungültig. Details stehen in der [Paperless-ngx-Dokumentation zur API-Authentifizierung](https://docs.paperless-ngx.com/api/#authorization).
3. Auf dem Scan-Bridge-Host eine nicht versionierte `.env`-Datei neben `compose.yaml` anlegen:

   ```dotenv
   PAPERLESS_URL=https://paperless.example.test
   PAPERLESS_TOKEN=den-kopierten-token-hier-einsetzen
   ```

4. `docker compose up --detach --build` ausführen. Die `.env`-Datei und der Token dürfen nicht committed, in Screenshots geteilt oder in Support-Ausgaben eingefügt werden. Der Benutzer des Tokens benötigt mindestens Leserechte für Dokumente, Korrespondenten, Dokumenttypen und Tags sowie die Berechtigung zum Hinzufügen von Dokumenten.
5. Nach der PDF-Erstellung **Verbindung prüfen und Metadaten laden** wählen. Titel, Korrespondent, Dokumenttyp und Tags auswählen und anschließend **An Paperless senden** drücken. Nach Annahme zeigt die Anwendung die Paperless-Auftrags-ID. Bei Netzwerk- oder Serverfehlern bleibt `document.pdf` in der Scan-Sitzung erhalten und der kontrollierte erneute Versuch ist möglich.

Die Verbindungskontrolle unterscheidet fehlende Konfiguration, ungültige Authentifizierung (HTTP 401), fehlende Berechtigung (HTTP 403), Netzwerk-/Timeoutprobleme und Paperless-Serverfehler. `PAPERLESS_TIMEOUT_SECONDS` ändert bei Bedarf das Standardzeitlimit von 60 Sekunden. Paperless übernimmt OCR und seine normale Verarbeitung; die Bridge wartet nicht auf deren Abschluss.

## Lokale Profilvorgaben

Unter **Einstellungen** können Scan Bridge-Nutzer einen Standardscanner samt Quelle, Farbmodus und Auflösung sowie Titel, Korrespondent, Dokumenttyp und Tags für Paperless speichern. Die Werte liegen im persistenten SQLite-Datenvolumen. Eine neue Scan-Sitzung übernimmt eine Momentaufnahme; spätere Änderungen wirken nur auf danach gestartete Sitzungen.

Vor dem Speichern werden Scannerquelle und Auflösung gegen die gespeicherten Fähigkeiten geprüft. Über **Paperless-Verbindung prüfen und Auswahl laden** werden gespeicherte Metadaten-IDs gegen die aktuelle Paperless-Instanz geprüft. Entfernte oder nicht mehr unterstützte Werte werden angezeigt und müssen korrigiert werden. **Auf Werkseinstellungen zurücksetzen** entfernt den lokalen Profildatensatz vollständig.

US-008 ist absichtlich ein einzelnes lokales Profil ohne Anmeldung. Die geplanten [US-011](docs/user-stories/011-authenticated-user-profiles.md) und [US-012](docs/user-stories/012-profile-service-configuration.md) ergänzen eine OpenID-Connect-Anmeldung (beispielsweise Google oder Microsoft Entra ID), Benutzerisolation und verschlüsselt gespeicherte Paperless-URL und API-Token. Bis dahin bleiben URL und Token deployer-gesteuerte Umgebungswerte.

## Browser notifications

Use **Benachrichtigungen aktivieren** on the start page to opt in. The browser then reports when a simplex scan needs a timeout decision, duplex fronts are ready to flip, pass counts differ, or a scan completes or fails. Permission is never requested during page load. A denied or unsupported permission is explained in the page.

The opt-in and delivered-event keys are kept in the browser tab's `sessionStorage`. Reconnects and repeated state events therefore do not repeat an already delivered notification, and no application singleton stores notification state. Delivery uses a service worker, so an open application tab also raises the operating-system notification while another tab or application has focus. Clicking it focuses the existing scan page. The Blazor page must remain open and connected so it can receive the scan transition; this release does not implement server-originated Web Push for a fully closed browser. Browsers require a secure HTTPS origin (or `localhost`) for notifications and service workers.

## Deployment hardening

The supported self-hosted deployment is a single Linux host, preferably Raspberry Pi OS or another ARM64 Linux system, with Docker Engine and the Compose plugin. A non-ARM Linux development host is also supported for build and smoke validation. Host networking is intentional because scanner discovery depends on multicast DNS; do not add Compose port publishing while `network_mode: host` is active.

### Configuration and secrets

Use an ignored `.env` file or an external secret manager for deployer-controlled values. Never bake Paperless tokens, scanner identifiers, or private URLs into an image. The current deployment-wide Paperless fallback is configured with `PAPERLESS_URL` and `PAPERLESS_TOKEN`; later profile stories will make this optional for signed-in or anonymous profile modes.

`./app/data` contains SQLite configuration, selected scanner state, generated SANE configuration, profile defaults, and ASP.NET Core data-protection keys. `./app/temp` contains active scan sessions, source PNG pages, ordered working copies, and generated PDFs. Back up `./app/data` while the container is stopped. Back up `./app/temp` only if you intentionally want to recover unfinished local scan artifacts; an in-progress scanner process itself is not recoverable after shutdown.

```bash
docker compose down
rsync -a ./app/data/ /backup/paperless-scan-bridge/data/
# Optional recovery copy for not-yet-uploaded documents:
rsync -a ./app/temp/ /backup/paperless-scan-bridge/temp/
docker compose up --detach
```

Restore by stopping the container, replacing `./app/data` from the backup, optionally restoring `./app/temp`, and starting Compose again. Keep ownership writable by the container; the entrypoint repairs `./app/data` and `./app/temp` permissions for the image user at startup.

### Readiness, logs, and resource expectations

The container image and Compose file define a health check against `http://127.0.0.1:8080/health`. The endpoint verifies application readiness for SQLite plus writable temporary and data-protection storage. It deliberately does not require the scanner or Paperless-ngx to be online, because those are workflow dependencies that may be unavailable while the bridge should still start and show diagnostics.

Use structured container logs for operations:

```bash
docker compose ps
docker compose logs --timestamps --tail=200 scan-bridge
docker compose logs --follow scan-bridge
```

Logs include event categories, session identifiers, page counts, command outcomes, and failure types for scanning, PDF creation, persistence, and Paperless uploads. They must not contain API tokens, scanner document pixels, uploaded PDF contents, or private metadata values. If you share diagnostics, redact hostnames, IP addresses, and user-specific document metadata first.

Plan persistent storage for the SQLite database, data-protection key ring, generated SANE configuration, temporary PNG pages, and generated PDFs. Memory and CPU requirements depend mostly on scan resolution and page count; keep enough free disk for at least two copies of the largest expected document while PDF creation is running.

### Upgrade, rollback, and shutdown

For an upgrade, back up `./app/data`, pull or build the new revision, set `GIT_COMMIT`, and recreate the container:

```bash
docker compose down
rsync -a ./app/data/ /backup/paperless-scan-bridge/data-before-upgrade/
export GIT_COMMIT="$(git rev-parse --short HEAD)"
docker compose up --detach --build
curl --fail http://127.0.0.1:8080/health
```

For rollback, check out the previous revision, restore the matching data backup if the migration is not backward-compatible, and run `docker compose up --detach --build`. Graceful `docker compose down` lets the Blazor Server process stop cleanly; active scanner processes and in-flight uploads are cancelled, partial PDF files remain hidden by the atomic `.partial` to final-file rename, and completed PDFs in `./app/temp` can be retried after restart.

Manual verification for a release should include `docker compose config`, container build, a health check, scanner discovery on the target network, one representative scan through PDF creation, and one Paperless upload using non-production test data. Record the host architecture, scanner model/firmware, Compose commands, and any accepted hardware limitations in the release notes or pull request.

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

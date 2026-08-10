# paperless-scan-bridge

`paperless-scan-bridge` is a small, self-hosted, mobile-first ASP.NET Core Blazor Server application for initiating scans on an HP network multifunction printer and sending the resulting documents to [Paperless-ngx](https://docs.paperless-ngx.com/).

The guided workflow will support simplex scans and manual duplex scans, put pages into reading order, offer lightweight preview and editing, create a PDF, and upload it with metadata. The application is intended to run in Docker on a Raspberry Pi or another always-on host.

The project discovers eSCL/AirScan scanners directly with DNS-SD/mDNS, validates a selected device through its `ScannerCapabilities` endpoint, and then exposes it to the SANE adapter. The selected scanner can capture simplex documents from a touch-friendly screen using the platen or automatic document feeder. Each job is written to its own temporary session directory; later stories add preview, editing, PDF creation, and upload.

## Mobile-first application shell

The protected application uses the same four destinations on every viewport: **Scan**, **Documents**, **Settings**, and **Status**. On phones they form a touch-sized bottom navigation; from tablet/desktop widths they use the persistent sidebar. The upper-right account menu always identifies the resolved shared-household or authenticated profile. In anonymous mode it explains that settings are shared; after OpenID Connect sign-in it presents the display name, safe provider image or initials, and the sign-out action without exposing provider boilerplate, identity keys, or tokens. The menu supports keyboard focus, Escape, outside-click, and touch input.

**Scan** keeps the existing workflow behavior and shows only the current known step in the ordered sequence Prepare, Scan, Review, PDF, and Send. Scanner discovery and selection now live in **Settings**. Build, health, mDNS, capability-validation, and deployment guidance are separated under **Status**. **Documents** currently explains that active artifacts remain in the protected scan workflow; it does not promise a persisted history. State cards use concise messages and keep technical detail behind an explicit disclosure.

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

Configuration uses standard ASP.NET Core keys. In Compose, most operator-facing settings are exposed as shell-style variables with safe defaults. Put overrides in an untracked `.env` file next to `compose.yaml`, export them in the shell before running Compose, or provide them through your deployment system. Compose substitutes `${NAME:-default}` before the container starts; inside the container the resulting environment variables use ASP.NET Core's double-underscore section syntax, for example `Paperless__BaseUrl` maps to `Paperless:BaseUrl`.

Example `.env`:

```dotenv
GIT_COMMIT=local-dev
PAPERLESS_URL=https://paperless.example.test
PAPERLESS_TOKEN=replace-with-a-paperless-api-token
PAPERLESS_TIMEOUT_SECONDS=60
SCANNER_SCAN_TIMEOUT_SECONDS=1800
SCANNER_MAXIMUM_SCAN_DURATION_SECONDS=14400
```

Do not commit `.env`, API tokens, client secrets, private scanner IDs, or private hostnames. After changing variables, recreate the service with `docker compose up --detach --build` and confirm the effective configuration with `docker compose config` before sharing diagnostics.

| Compose variable | Container key | Default | Meaning |
| --- | --- | --- | --- |
| `GIT_COMMIT` | `Build__Commit` image build argument and label | `unknown` | Shows the running source revision in the UI and OCI metadata. |
| `PAPERLESS_URL` | `Paperless__BaseUrl` | `http://paperless:8000` | Deployment-wide Paperless-ngx base URL used by the current upload flow and by later fallback/anonymous profile modes. |
| `PAPERLESS_TOKEN` | `Paperless__ApiToken` | empty | Deployment-wide Paperless API token. Treat it as a secret; later profile stories may replace it with encrypted per-profile tokens. |
| `PAPERLESS_TIMEOUT_SECONDS` | `Paperless__TimeoutSeconds` | `60` | HTTP timeout for Paperless connectivity, metadata loading, and upload calls. |
| `SCANNER_DEVICE_ID` | `Scanner__DeviceId` | empty | Optional fixed SANE device identifier for diagnostics or deployments that bypass UI selection. Prefer UI discovery for normal use. |
| `SCANNER_TIMEOUT_SECONDS` | `Scanner__TimeoutSeconds` | `30` | Timeout for short scanner discovery and capability commands. |
| `SCANNER_SCAN_TIMEOUT_SECONDS` | `Scanner__ScanTimeoutSeconds` | `120` | User-confirmation interval while a scan process is still running. Increase for slow ADF batches. |
| `SCANNER_MAXIMUM_SCAN_DURATION_SECONDS` | `Scanner__MaximumScanDurationSeconds` | `14400` | Hard safety limit for abandoned scan processes. |
| *(Compose fixed)* | `Persistence__ConnectionString` | `Data Source=/app/data/bridge.db` | SQLite database path in the persistent data volume. Change only together with backup/restore plans. |
| *(Compose fixed)* | `TemporaryStorage__Path` | `/app/temp` | Temporary scan/PDF working directory. Keep it on writable storage with enough free space for large jobs. |
| *(Compose fixed)* | `DataProtectionStorage__Path` | `/app/data/dataprotection-keys` | Persisted ASP.NET Core data-protection key ring used for cookies and future encrypted profile secrets. |
| *(Compose fixed)* | `ScannerDiscovery__SaneConfigurationDirectory` | `/app/data/sane.d` | Persistent directory where the application writes generated sane-airscan configuration. |
| `PROFILE_MODE` | `Profiles__Mode` | `Anonymous` | `Anonymous` uses one shared no-login profile; `OpenIdConnect` requires OIDC sign-in and isolates profiles by issuer plus subject. |
| `PROFILE_ANONYMOUS_SUBJECT` | `Profiles__AnonymousSubject` | `scan-bridge-local-anonymous-profile` | Stable deployment-local subject for the shared anonymous profile. Not secret; keep stable across restarts. |
| `PROFILE_LEGACY_DEFAULTS_MIGRATION` | `Profiles__LegacyDefaultsMigration` | `MoveToAnonymous` | One-time handling for pre-US-011 local defaults. `MoveToAnonymous` keeps them on the anonymous profile; `Reset` can be used before first authenticated production use to discard them. |
| `PROFILE_ALLOW_PAPERLESS_URL_OVERRIDE` | `ProfileServices__AllowProfileUrlOverride` | `true` | Lets authenticated profiles store a validated Paperless URL; set `false` to enforce the deployment URL. |
| `PROFILE_REMOTE_SIGNOUT_URL` | `Profiles__RemoteSignOutUrl` | empty | Optional absolute HTTPS provider logout URL used after local cookie removal. Leave empty when the provider advertises an OIDC `end_session_endpoint`; configure it only for providers whose supported logout flow is not discoverable from their metadata. |
| `OIDC_AUTHORITY` | `Authentication__Oidc__Authority` | empty | OpenID Connect issuer/authority, for example `https://accounts.google.com`. Required when `PROFILE_MODE=OpenIdConnect`. |
| `OIDC_CLIENT_ID` | `Authentication__Oidc__ClientId` | empty | OIDC web application client ID. Treat deployment-specific values as sensitive operational metadata. |
| `OIDC_CLIENT_SECRET` | `Authentication__Oidc__ClientSecret` | empty | OIDC web application client secret. Secret; store only in `.env` or a secret manager. |


Advanced ASP.NET Core configuration keys can also be set directly with double underscores, but prefer the documented Compose variables above for supported deployments.

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

US-008 führte lokale Vorgaben ein. Die abgeschlossenen [US-011](docs/user-stories/done/011-authenticated-user-profiles.md) und [US-012](docs/user-stories/done/012-profile-service-configuration.md) ergänzen OpenID-Connect-Anmeldung, Benutzerisolation und verschlüsselt gespeicherte Paperless-URL und API-Token; Bereitstellungswerte bleiben als kontrollierbarer Fallback verfügbar.

## Per-profile Paperless configuration

The settings page can validate and activate a profile-specific Paperless URL and API token. Activation checks the HTTPS URL policy (plain HTTP is accepted only for loopback development), connectivity, authentication, document-list permission, and the correspondent, document-type, and tag endpoints. A profile value takes precedence over the optional deployment fallback; the page identifies the effective source. Operators can disable URL overrides with `PROFILE_ALLOW_PAPERLESS_URL_OVERRIDE=false`. Anonymous mode has exactly one shared profile and always uses the read-only `PAPERLESS_URL` and `PAPERLESS_TOKEN` deployment values without prompting; this provides no per-person separation.

Profile API tokens are encrypted with ASP.NET Core Data Protection before SQLite writes. The plaintext is never returned to the browser after saving. The settings page can reveal only a newly entered replacement while it is still browser-local; use the explicit replace/delete controls for rotation. Deployment tokens are read at process startup and never copied into SQLite. Scan-session download routes have a persisted profile owner and return not-found across profile boundaries; metadata/default records and service configuration are keyed by the same internal profile ID. Do not place sensitive values in logs.

Recovery procedures:

- **Rotate or invalidate a token:** enter a replacement and activate it; the old encrypted value is overwritten only after the full connection test passes. If credentials fail, the last active configuration remains available.
- **Paperless unavailable:** correct DNS/TLS/firewall or URL settings and retry validation. No candidate settings become active after a failed check.
- **Account deletion:** deny the identity at the OIDC provider, then remove its `UserProfiles`, profile defaults, profile service configuration, scan ownership rows, and corresponding temporary session directories during a stopped maintenance window.
- **Migration/backup/restore:** stop Compose and back up `bridge.db` together with `dataprotection-keys`; both are required to decrypt profile tokens. Restore them as one consistent set. Losing the key ring intentionally makes old tokens unreadable; delete the affected rows and enter new tokens. Deployment fallback secrets must be restored separately from the secret manager or `.env`.
- **Mode changes:** authenticated and anonymous identities use distinct internal profile IDs. Switching modes does not merge records. Before permanently changing mode, finish or remove temporary documents; the one anonymous profile remains shared by every anonymous visitor.

## Profile mode and OpenID Connect sign-in

Scan Bridge supports two profile modes controlled by deployment settings:

- `Anonymous` (default) starts without a login screen. Every browser visitor uses one deployment-local profile named by `PROFILE_ANONYMOUS_SUBJECT`, so scanner and Paperless defaults are shared intentionally.
- `OpenIdConnect` requires sign-in before scans, documents, settings, Paperless defaults, and scanner APIs are available. `/health`, `/signin`, and `/signin-oidc` remain reachable so reverse proxies and the identity provider can complete startup and login flows.

Google provider setup example:

1. In Google Cloud, create or choose a project and configure the OAuth consent screen for your household or trusted test users.
2. Create an OAuth 2.0 **Web application** client. Add the public Scan Bridge origin, for example `https://scan.example.test`, and register the exact callback path `https://scan.example.test/signin-oidc`.
3. Store the client ID and client secret outside the image, for example in an untracked `.env` file or your secret manager, and map them to `OIDC_CLIENT_ID` and `OIDC_CLIENT_SECRET`.
4. Set `PROFILE_MODE=OpenIdConnect`, `OIDC_AUTHORITY=https://accounts.google.com`, and serve Scan Bridge through HTTPS. Google does not advertise a standard OIDC `end_session_endpoint`, so Scan Bridge can always end its own session but cannot discover a Google browser-session logout. Only set `PROFILE_REMOTE_SIGNOUT_URL` if Google documents and supports an appropriate logout URL for your deployment; token revocation is account disconnection and is not the same operation. Keep forwarded headers enabled at the reverse proxy so generated redirect URIs use the external HTTPS origin.
5. Test sign-in from a mobile browser, sign out, try a denied Google account if consent-screen restrictions are used, rotate the client secret, and keep a recovery path: either restore the provider configuration or temporarily set `PROFILE_MODE=Anonymous` while the provider is unavailable.

Authenticated profile identity uses the provider issuer and stable subject claim. Email addresses are display metadata only and are not used as immutable profile keys. If an account should be removed, delete or deny it at the provider and remove the corresponding SQLite `UserProfiles` row during maintenance; its defaults are isolated by internal profile ID.

Logout always clears the local Scan Bridge cookie first. It then redirects to `PROFILE_REMOTE_SIGNOUT_URL` when explicitly configured; otherwise it asks the OIDC handler to use the provider's discovered `end_session_endpoint`. If discovery or provider logout fails, Scan Bridge records a warning and still completes the local logout. The optional URL is therefore an escape hatch, not a generally required setting.

Provider behavior differs:

| Provider | Recommended Scan Bridge configuration |
| --- | --- |
| authentik | Leave `PROFILE_REMOTE_SIGNOUT_URL` empty. authentik exposes an application-specific end-session endpoint in its OpenID configuration. |
| Microsoft Entra ID | Leave it empty. Microsoft publishes `end_session_endpoint` in its OIDC metadata, so the standard handler can discover the logout endpoint. |
| Google | Google publishes a token-revocation endpoint but no OIDC `end_session_endpoint`. Local Scan Bridge logout still works, but provider-session logout therefore requires an explicitly configured URL. Do not confuse revoking consent/tokens with ending the Google browser session; configure only a Google-supported endpoint appropriate to the deployment. |
| Apple | Apple publishes token revocation but no OIDC `end_session_endpoint`. Local logout works; revoking Sign in with Apple tokens is account disconnection rather than browser-session logout. |
| GitHub | GitHub's regular user OAuth Apps are OAuth 2.0 rather than an OIDC login provider and do not expose an OIDC end-session endpoint. They cannot be used directly as `OIDC_AUTHORITY` by this integration; use an OIDC broker such as authentik if GitHub login is required. |

These recommendations are based on the providers' current official interfaces: [authentik OAuth2/OIDC endpoints](https://docs.goauthentik.io/add-secure-apps/providers/oauth2/), [Microsoft OIDC logout](https://learn.microsoft.com/entra/identity-platform/v2-protocols-oidc#send-a-sign-out-request), [Google OpenID configuration](https://accounts.google.com/.well-known/openid-configuration), [Apple OpenID configuration](https://appleid.apple.com/.well-known/openid-configuration), and [GitHub OAuth App authorization](https://docs.github.com/apps/oauth-apps/building-oauth-apps/authorizing-oauth-apps). Re-check the relevant provider documentation before configuring a custom URL because provider behavior can change.

Cookies use ASP.NET Core data-protection keys persisted in `/app/data/dataprotection-keys`; back up this directory with the database so authentication and antiforgery cookies survive ordinary container recreation. The auth cookie is `Secure`, uses `SameSite=Lax`, and should be used behind an HTTPS reverse proxy. After changing profile or OIDC variables, run `docker compose config`, recreate the service, and verify `/health` plus a full sign-in/sign-out loop.

## Browser notifications

Use **Benachrichtigungen aktivieren** under **Einstellungen** to opt in. The browser then reports when a simplex scan needs a timeout decision, duplex fronts are ready to flip, pass counts differ, or a scan completes or fails. Permission is never requested during page load. A denied or unsupported permission is explained in the page.

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

## Splitting scan batches into documents

After either a simplex or manual-duplex acquisition, the Review step treats all captured pages as one batch. On desktop, three page cards remain side by side and a narrow vertical grey **Split here** separator sits between adjacent cards; on mobile, the same control becomes a full-width horizontal separator between stacked cards. Selecting it turns the separator blue and makes it an active document boundary. The same control removes it again. The resulting document cards always cover the pages once, in visible order. Rotations and confirmed removals remain page edits, so moving a boundary changes only which document receives those edited pages.

After the boundaries are confirmed, each document follows the regular guided **Review → PDF → Send** path in order. The workflow stepper labels those three stages with the current document number and total, such as `2/3`, while the batch overview and completion action show how many documents remain. Every document has independent Paperless metadata, PDF preparation, download, upload result, and retry controls. A successful document is immutable and is not regenerated or uploaded when another document is retried. A split batch therefore cannot accidentally be submitted as one Paperless document.

Boundary, metadata, PDF, and upload state is atomically stored next to the temporary scan pages using a non-reversible profile key. An ordinary Blazor reconnect keeps the scoped workflow, while a refresh reloads the matching profile's batch state from temporary storage. Clearing `TemporaryStorage:Path` removes this recovery data under the same lifetime policy as source pages and PDFs. The document download route authorizes the parent scan session before serving a generated child PDF; another profile receives not-found.

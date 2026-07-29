# paperless-scan-bridge

`paperless-scan-bridge` is intended to be a small, self-hosted, mobile-first ASP.NET Core Blazor Server application that improves scanning workflows for [Paperless-ngx](https://docs.paperless-ngx.com/). It is designed to run in Docker—likely on a Raspberry Pi or another always-on machine—and to initiate scans on an HP network multifunction printer.

> [!NOTE]
> This project is at an early planning stage. The architecture and implementation details below describe the current direction and may change as scanner behavior and deployment constraints become better understood.

## The problem

Scanning a document into Paperless-ngx can involve more manual work than the scan itself. Starting a job may require using the printer's control panel or a desktop computer; a printer without automatic duplex scanning makes two-sided documents especially awkward. The resulting front and back images then need to be reordered, assembled into one document, and checked before they can become a PDF. Finally, that PDF still needs to be uploaded to Paperless-ngx and supplied with useful metadata.

`paperless-scan-bridge` aims to turn those disconnected steps into one guided workflow that works comfortably from a phone. In particular, it should guide a user through manual duplex scanning, assemble the pages in reading order, and send the finished document and its metadata to Paperless-ngx.

## MVP scope

The initial version is expected to:

- Start scan jobs from the web UI.
- Support simplex (single-sided) scanning.
- Support manual duplex scanning by:
  1. scanning the front sides,
  2. prompting the user to flip the stack,
  3. scanning the back sides, and
  4. merging both passes into the correct page order.
- Provide a simple preview of the scanned pages.
- Allow basic page deletion and rotation where practical for the selected PDF/image tooling.
- Generate a final PDF from the ordered pages.
- Upload the PDF to Paperless-ngx through its REST API, together with basic metadata.
- Support a user profile and configurable defaults for commonly reused scan and document settings.
- Store configuration in SQLite.
- Include a `Dockerfile` and Docker Compose configuration from the initial project setup so the application can be built and started locally with `docker compose up`.

Exact scanner options, supported metadata fields, preview fidelity, and editing behavior remain to be validated during implementation.

## Non-goals

The MVP is not intended to provide:

- Multi-tenant authentication or tenant isolation.
- Complex OCR tuning; Paperless-ngx is expected to remain responsible for its normal document processing and OCR workflow.
- Multiple document-management-system targets. The first version will target Paperless-ngx only.

## High-level architecture

The proposed architecture consists of:

- **Mobile browser UI:** a touch-friendly Bootstrap-based interface for starting scans, following duplex prompts, previewing pages, entering metadata, and submitting a document. Custom styling should remain lightweight and build on Bootstrap's responsive components and utilities.
- **ASP.NET Core Blazor Server application:** the web host and interactive UI layer.
- **Workflow/application services:** orchestration for scan sessions, manual duplex sequencing, page state, PDF generation, and uploads.
- **Replaceable scanner adapter:** an abstraction intended to keep scanner-specific process invocation and device behavior outside the application workflow.
- **MVP scanner backend:** SANE with `sane-airscan` for network-device access and `scanimage` for discovery and scan execution. Compatibility and the precise command options will need testing with the target HP multifunction printer.
- **PDF assembly and lightweight page-editing components:** tooling for image conversion, ordering, rotation, deletion, previews, and PDF output. The specific libraries have not yet been selected.
- **Paperless-ngx REST API client:** a focused client for uploading the generated PDF and supported metadata.
- **SQLite persistence:** local storage for profiles, configuration, defaults, and potentially lightweight workflow state.
- **Container deployment:** a `Dockerfile` and Docker Compose configuration maintained alongside the application from the initial scaffold. They should provide a straightforward local `docker compose up` workflow and form the basis for running on a Raspberry Pi or another self-hosted machine with network access to both the printer and Paperless-ngx. Automated image publishing or deployment is deferred until later.

A possible request flow is:

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

## Definition of done

A feature or roadmap item is considered done when all applicable criteria below are met:

- Its expected behavior and acceptance criteria are documented and implemented, including useful validation and error handling.
- Automated unit tests cover workflow and application logic, with particular attention to page ordering and manual duplex edge cases.
- Integration tests cover boundaries such as persistence, scanner command handling, PDF generation, and the Paperless-ngx client where practical. External systems may be replaced with controlled test doubles or fixtures in automated test runs.
- Blazor components with meaningful behavior have component tests where practical, and the responsive Bootstrap UI is checked at representative mobile and desktop viewport sizes.
- A representative end-to-end scan workflow is verified. Hardware-dependent behavior that cannot run in continuous integration is documented and tested manually against the target HP printer before the relevant milestone is accepted.
- The complete automated test suite passes, and the application builds without errors.
- The container image builds successfully, `docker compose config` validates the Compose file, and `docker compose up` starts a usable local instance with documented configuration.
- Documentation and example configuration are updated, with no credentials or other secrets committed to the repository.
- Changes have been reviewed and no known critical defects remain. Any accepted limitations are recorded.

The exact test frameworks and continuous-integration service are still to be selected, but adding the appropriate tests is part of each feature rather than a separate final phase.

## Rough roadmap

1. **Project scaffolding**
   - Establish the ASP.NET Core Blazor Server solution and basic mobile-first Bootstrap UI shell.
   - Define initial configuration, persistence, and scanner-adapter boundaries.
   - Add the initial `Dockerfile` and Docker Compose configuration so a developer can build and try the application locally with `docker compose up` from the outset.
   - Establish the test projects and make the initial build, test, container-build, and Compose-validation checks repeatable.
2. **Scanner discovery and validation**
   - Build a container prototype with SANE, `sane-airscan`, and `scanimage`.
   - Discover the HP network multifunction printer and verify supported sources, formats, resolutions, and paper sizes.
3. **Simplex scanning**
   - Start a scan from the UI and capture one or more single-sided pages.
   - Add useful progress reporting, cancellation, and error messages where the backend permits them.
4. **Manual duplex ordering**
   - Model the two-pass workflow and stack-flip prompt.
   - Validate front/back ordering for the printer's feeder behavior and merge both passes correctly.
5. **Preview and lightweight editing**
   - Show page thumbnails or another practical preview.
   - Add page deletion and rotation based on the chosen image/PDF components.
6. **PDF creation**
   - Convert the ordered scan output into a final PDF.
   - Review output size, quality, temporary-file handling, and cleanup.
7. **Paperless-ngx integration**
   - Add API configuration and connectivity checks.
   - Upload PDFs with the supported title, correspondent, document type, tags, or other selected metadata.
8. **Profiles and defaults**
   - Persist a user profile and configurable scan/upload defaults in SQLite.
   - Refine the repeat-scan experience for common document types.
9. **Deployment hardening**
   - Harden and verify the existing container setup for Raspberry Pi and other intended target platforms.
   - Add health checks, logging, secrets guidance, backup considerations, and recovery behavior.
   - Test upgrades and representative end-to-end workflows on the target hardware.
   - Consider automated deployment and publishing images to a registry such as Docker Hub only after the local deployment workflow is stable.

The milestones are deliberately broad: early experiments with the scanner, SANE drivers, and PDF tooling are expected to influence the final design.

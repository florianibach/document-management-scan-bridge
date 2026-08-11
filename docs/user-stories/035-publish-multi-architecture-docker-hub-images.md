# US-035: Publish multi-architecture Docker Hub images

## User story

**As a** self-hosting operator, **I want** versioned Scan Bridge images published automatically to Docker Hub, **so that** I can deploy a tested image on Raspberry Pi or AMD64 hosts without building it myself.

## Acceptance criteria

- A GitHub Actions workflow based on the PullPulse publishing approach logs in with repository secrets, builds through Docker Buildx/QEMU, and publishes to `${DOCKERHUB_USERNAME}/${DOCKERHUB_IMAGENAME}` without hard-coding an account or repository name.
- The published manifest contains `linux/amd64`, `linux/arm64`, and `linux/arm/v7` images, or a documented architecture is removed only when the application base image or a locked dependency demonstrably cannot support it.
- Publishing runs on pushes to the repository's default branch and through manual dispatch. Pull requests build and validate the image but never authenticate to Docker Hub or publish it.
- Each successful default-branch publication receives immutable version and commit-derived tags plus the mutable `latest` tag. Version calculation, concurrent runs, retries, and existing tags cannot overwrite an immutable release or create two releases with the same version.
- Git tag creation and image publication behave atomically from an operator perspective: a failed multi-architecture publication is not presented as a complete release, and rerunning a failed job has a documented recovery path.
- Docker Hub credentials use `DOCKERHUB_USERNAME` and `DOCKERHUB_TOKEN` GitHub secrets, the image name uses the `DOCKERHUB_IMAGENAME` repository variable, and logs and metadata do not expose secrets.
- The workflow passes the source revision into the existing image metadata and applies OCI labels, including source repository, revision, version, and license where available.
- A separate manually triggerable or README-change workflow can synchronize the repository README to the Docker Hub description, following the reference repository without causing an image release for documentation-only changes.
- GitHub Actions use pinned major versions of the official Docker and checkout actions. The implementation evaluates BuildKit provenance/SBOM support and enables it when compatible with Docker Hub multi-architecture publishing, or records a concrete reason for deferral; image signing and registry vulnerability enforcement are not silently implied.
- README release documentation covers required secrets/variables, triggers, tag semantics, supported platforms, local manifest inspection, failed-release recovery, and pulling a pinned version rather than relying on `latest` for reproducible deployment.
- Workflow validation and a release dry run verify metadata/tag generation and all target-platform builds without publishing from untrusted changes; the first authorized publication verifies the remote manifest and platform list.

## Out of scope

- Publishing to GitHub Container Registry or another registry.
- Embedding Docker Hub credentials in repository files or making them available to pull-request workflows.
- Guaranteeing vendor-specific image signing or vulnerability policy until the required mechanism is explicitly selected and configured.

## Dependencies

- US-001
- US-009
- US-023

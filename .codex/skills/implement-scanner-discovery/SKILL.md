---
name: implement-scanner-discovery
description: Implement and verify US-002 scanner discovery for paperless-scan-bridge using SANE, sane-airscan, scanimage, the application process boundary, safe diagnostics, typed device selection, tests, container packaging, and hardware verification documentation. Use when implementing or revising scanner discovery and capability inspection; do not use it to start scans or implement later document workflows.
---

# Implement Scanner Discovery

## Establish scope

1. Read US-002 in `docs/user-stories/` or `docs/user-stories/done/`, plus `docs/definition-of-done.md`, `README.md`, and the existing scanner boundaries.
2. Keep discovery and option inspection behind `IScanner` and `IProcessRunner`; keep process execution out of Razor components.
3. Limit the implementation to `scanimage -L` and option inspection. Do not acquire pages or implement later stories.

## Implement discovery

1. Extend typed scanner configuration with an optional target device identifier and a bounded discovery timeout.
2. Invoke commands with `ProcessStartInfo.ArgumentList`, redirected output, no shell, cancellation, and forced process-tree termination on timeout.
3. Parse the stable SANE device-list format and tolerate unrelated output. Treat an empty list, a missing executable, timeout, cancellation, and non-zero exit as distinct actionable results.
4. Inspect the configured device, or the sole discovered device when none is configured. Never guess between multiple devices.
5. Parse and expose sources, modes/formats, resolutions, and geometry-derived standard paper sizes. Preserve raw option output only in debug logs; never expose environment variables, command lines containing secrets, or stack traces to users.
6. Register the adapter in dependency injection and expose a mobile-first status screen with loading, empty, success, and failure states.
7. Install `sane-utils` and `sane-airscan` in the runtime image on every supported architecture. Document host networking/discovery requirements and configuration.

## Test and finish

1. Unit-test device and option parsing, device-selection rules, and all failure mappings.
2. Integration-test the real process boundary with controlled executable fixtures, including non-zero exit, missing executable, and timeout behavior.
3. Component-test the meaningful screen states and check representative mobile and desktop viewports.
4. Run the complete repository suite, Release build, container build, Compose validation/startup, health check, repository validator, and skill validator. Install missing tools rather than accepting a failed check.
5. Record target HP model, firmware, network prerequisites, verified options, and the exact hardware commands. Clearly mark hardware facts as pending until performed on the target device.
6. Map every acceptance criterion and applicable Definition of Done item to evidence. Move `002-scanner-discovery.md` into `docs/user-stories/done/` only after all locally executable checks pass; update links and roadmap state.

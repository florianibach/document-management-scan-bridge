# Definition of Done

A story is done only when every applicable criterion below is satisfied. Any criterion that does not apply must be marked as such in the change description with a reason; it is not silently skipped.

## Product behavior

- Acceptance criteria are implemented and traceable to tests or an explicit verification record.
- Validation, cancellation, retry, empty states, and failure behavior are useful and safe where relevant.
- No behavior from a later story is introduced merely because its future boundary already exists.
- Accepted limitations and follow-up work are recorded.

## Quality and testing

- Unit tests cover application and workflow logic, especially manual-duplex ordering and edge cases when affected.
- Integration tests cover changed boundaries such as SQLite, scanner processes, PDF generation, and the Paperless-ngx client using controlled doubles or fixtures where practical.
- Meaningful Blazor behavior has component tests, and relevant screens are checked at representative mobile and desktop viewport sizes.
- A representative end-to-end workflow is verified. Hardware-dependent checks that cannot run in CI are recorded and manually run on the target HP printer before the relevant milestone is accepted.
- The complete automated suite passes without disabled or flaky tests being hidden.

## Build and operations

- The application builds without errors, and CI-required warnings are addressed.
- The container image builds, `docker compose config` validates, and `docker compose up` starts a usable local instance with documented configuration.
- Changed behavior has appropriate logs and diagnostics without credentials, sensitive metadata, or document content.
- Temporary data, persistence, cleanup, and recovery behavior are considered for both success and failure paths.

## Documentation, security, and review

- User, developer, operations, and example configuration documentation are updated as applicable.
- No credentials, tokens, private documents, personal data, or other secrets are committed.
- Dependencies are justified, supported, and checked for known vulnerabilities by the repository's automated tooling.
- The change has been reviewed, all review findings are resolved or explicitly accepted, and no known critical defects remain.

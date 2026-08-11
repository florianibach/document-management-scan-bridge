# US-030: Paperless HTTP connections

## User story

**As a** self-hosting user whose Paperless-ngx instance is reachable only inside a trusted local network, **I want** Scan Bridge to support an explicitly configured HTTP or HTTPS Paperless base URL, **so that** I can use my local deployment while understanding when its traffic is not encrypted.

## Acceptance criteria

- Deployment configuration accepts an absolute Paperless base URL with either the `http://` or `https://` scheme, including non-loopback HTTP hosts. When profile URL overrides are permitted, the same URL policy applies to an authenticated profile's saved override.
- A URL accepted by configuration validation is used consistently for the Paperless connection check, metadata retrieval, and document upload; no operation applies a stricter HTTPS-only or loopback-only policy afterward.
- Whenever the configured or effective Paperless URL uses HTTP, the relevant Paperless settings UI clearly states that the connection is unencrypted and that credentials, metadata, and documents can be observed or changed in transit. HTTPS remains the clearly recommended option.
- HTTP support does not disable or weaken certificate validation for HTTPS and does not change scanner discovery, scanner endpoint validation, or the scanner protocol downgrade rules.
- Validation rejects relative URLs, malformed URLs, unsupported schemes, and URLs containing user information such as a username or password. Existing validation for a usable Paperless base URL remains in effect.
- Automated tests cover deployment and permitted profile URLs for HTTP and HTTPS, non-loopback HTTP, rejected URL forms, the visible HTTP warning, and consistent use by connection checks, metadata retrieval, and upload. Regression tests prove that HTTPS certificate validation and scanner protocol rules remain unchanged.
- Compose and README documentation describe both supported schemes, the risks of HTTP on a local network, the HTTPS recommendation, URL validation rules, and representative configuration examples.

## Out of scope

- Disabling HTTPS certificate validation, trusting arbitrary certificates, or adding a Paperless-specific certificate authority management UI.
- Automatically upgrading, downgrading, or discovering a Paperless endpoint.
- Changing any scanner transport, discovery, certificate, or protocol-fallback behavior.
- Claiming that a local or private network makes HTTP confidential or tamper-resistant.

## Dependencies

- US-007
- US-012
- US-017

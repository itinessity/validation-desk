# Verification notes

Verified on 11 September 2026 using .NET SDK 10.0.401, Node.js 22 and Chromium/Playwright 1.63.0 on Windows.

- Release compilation: passed, no warnings or errors.
- Domain/protocol executable: 10 checks passed.
- Isolated HTTP workflow checks: passed.
- Browser primary workflow and mobile overflow checks: 2 passed.
- Browser JavaScript errors during the live workflow: none observed.
- npm dependency audit: zero known vulnerabilities at verification time.

## Separate live-provider smoke check

A clearly synthetic German VAT input (all zeroes) received a real VIES negative registration result. A published checksum example passed local IBAN validation. Saved evidence was masked. No private business identifier was used.

The screenshot was captured from the live-provider mode with English browser locale and reviewed visually. Provider availability and data may change after this check. CI uses explicitly labeled deterministic fixtures rather than external uptime.

Dockerfile and Compose configuration are included, but a Docker engine was not available on the verification machine; container execution was not tested locally. The Release .NET application was built and run directly. This repository has not been deployed as a public web service.

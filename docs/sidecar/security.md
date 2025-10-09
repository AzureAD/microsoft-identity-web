# Security & Hardening

## Key Practices

| Area | Guidance |
|------|----------|
| Secrets | Prefer certificates or managed identity over client secrets |
| Exposure | Keep sidecar internal (localhost / cluster network) |
| Least Privilege | Configure minimal scopes per downstream API |
| Rate Limiting | Protect `/AuthorizationHeader*` and `/DownstreamApi*` with proxy limits |
| Logging | Disable PII unless diagnosing; never log raw tokens |
| Updates | Pin & scan images; track releases |

## Token Cache

Avoid unnecessary `ForceRefresh`—increases latency & potential throttling.

## Network Policies

Restrict egress to Microsoft Entra endpoints + explicit downstream hosts. Deny other outbound by default.

## SHR (Signed HTTP Request)

Only use where downstream validates SHR. Rotate keys with overlapping validity; store private key securely.

## Claims Challenges

Use `Claims` override for CAE / step-up. Pair with correlation ID for traceability.

## Reverse Proxy Fronting

See [reverse-proxy.md](reverse-proxy.md) for Envoy/YARP/Nginx examples applying authN/Z centrally.

## Supply Chain

- Verify image digest.
- Scan for vulnerabilities.
- Maintain SBOM if required (future addition).

# Microsoft Entra Identity Sidecar Documentation

Start here to explore the Sidecar and integrate it into your applications.

## Start Here

**[Overview](index.md)** – What the Sidecar is, how it works, and when to use it versus in‑process Microsoft.Identity.Web.

## Choose Your Path

| Goal | Go Here First |
|------|---------------|
| New to the Sidecar | [Overview](index.md) → [Installation](installation.md) |
| Deploy / Operate | [Installation](installation.md) → [Configuration](configuration.md) → [Security](security.md) |
| Validate tokens or call downstream APIs | [Scenario Guides](#scenario-guides) → [Endpoints](endpoints.md) |
| Compare approaches | [Comparison](comparison.md) |
| Troubleshoot | [Troubleshooting](troubleshooting.md) / [FAQ](faq.md) |
| Agent identities | [Agent Identities](agent-identities.md) |

## Documentation Structure

| Document | Description |
|----------|-------------|
| **[index.md](index.md)** | Conceptual overview, architecture, decision guidance |
| **[installation.md](installation.md)** | Pull and run the container (Kubernetes, Docker, managed identity) |
| **[configuration.md](configuration.md)** | Settings and schema |
| **[agent-identities.md](agent-identities.md)** | Autonomous vs delegated agent patterns |
| **[endpoints.md](endpoints.md)** | HTTP API reference |
| **[security.md](security.md)** | Hardening and best practices |
| **[comparison.md](comparison.md)** | Sidecar vs in‑process Microsoft.Identity.Web |
| **[troubleshooting.md](troubleshooting.md)** | Common issues and fixes |
| **[faq.md](faq.md)** | Frequent questions |

## Scenario Guides

| Scenario | Description |
|----------|-------------|
| **[Validate an Authorization Header](scenarios/validate-authorization-header.md)** | Validate an incoming bearer token and read claims. |
| **[Obtain an Authorization Header](scenarios/obtain-authorization-header.md)** | Get an access token (user OBO, app, or managed identity) formatted for downstream use. |
| **[Call a Downstream API](scenarios/call-downstream-api.md)** | Make an HTTP call with automatic token acquisition and attachment. |
| **[Use Managed Identity](scenarios/managed-identity.md)** | Use a managed identity instead of client secrets or certificates. |
| **[Implement Long-Running OBO](scenarios/long-running-obo.md)** | Support flows that outlive the original request context. |
| **[Use Signed HTTP Requests](scenarios/signed-http-request.md)** | Generate PoP (Signed HTTP Requests) for proof-of-possession scenarios. |
| **[Agent Autonomous Batch Processing](scenarios/agent-autonomous-batch.md)** | Run batch jobs with autonomous agent identity (no user). |
| **[Integration from TypeScript](scenarios/using-from-typescript.md)** | Consume Sidecar APIs from a TypeScript service. |
| **[Integration from Python](scenarios/using-from-python.md)** | Call Sidecar endpoints using Python (requests/httpx). |

## Additional Resources

| Resource | Link |
|----------|------|
| **Microsoft Entra ID Documentation** | https://learn.microsoft.com/en-us/entra/identity/ |
| **Microsoft Identity Web Repository** | https://github.com/AzureAD/microsoft-identity-web |
| **Microsoft Identity Platform Docs** | https://learn.microsoft.com/entra/identity-platform/ |
| **Agentic identity platform documentation** | https://learn.microsoft.com/entra/agentic-identity-platform |

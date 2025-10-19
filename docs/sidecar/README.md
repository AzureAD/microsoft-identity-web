# Microsoft Entra Identity Sidecar Documentation

This directory contains comprehensive documentation for the Microsoft Entra Identity Sidecar.

## Start Here

👉 **[Sidecar Overview](index.md)** - Begin with the overview to understand what the sidecar is and how it works.

## Documentation Structure

| Document | Description |
|----------|-------------|
| **[index.md](index.md)** | Overview hub with architecture and key concepts |
| **[installation.md](installation.md)** | Container deployment and configuration |
| **[configuration.md](configuration.md)** | Configuration schema and options |
| **[agent-identities.md](agent-identities.md)** | Agent identity patterns and semantics |
| **[endpoints.md](endpoints.md)** | HTTP API reference |
| **[security.md](security.md)** | Security best practices and hardening |
| **[comparison.md](comparison.md)** | Comparison with in-process Microsoft.Identity.Web |
| **[troubleshooting.md](troubleshooting.md)** | Common issues and solutions |
| **[faq.md](faq.md)** | Frequently asked questions |

## Scenario Guides

| Scenario | Description |
|----------|-------------|
| **[Validate an Authorization Header](scenarios/validate-authorization-header.md)** | Validate incoming bearer tokens |
| **[Obtain an Authorization Header](scenarios/obtain-authorization-header.md)** | Acquire access tokens for downstream APIs |
| **[Call a Downstream API](scenarios/call-downstream-api.md)** | Make authenticated HTTP calls to protected APIs |
| **[Use Managed Identity](scenarios/managed-identity.md)** | Leverage Azure Managed Identity for authentication |
| **[Implement Long-Running OBO](scenarios/long-running-obo.md)** | Handle long-running On-Behalf-Of scenarios |
| **[Use Signed HTTP Requests](scenarios/signed-http-request.md)** | Implement SHR for enhanced security |
| **[Agent Autonomous Batch Processing](scenarios/agent-autonomous-batch.md)** | Autonomous agent identity for batch processing |
| **[Integration from TypeScript](scenarios/using-from-typescript.md)** | Integrate the sidecar from TypeScript applications |
| **[Integration from Python](scenarios/using-from-python.md)** | Integrate the sidecar from Python applications |

## Additional Resources

| Resource | Link |
|----------|------|
| **Microsoft Entra ID Documentation** | https://learn.microsoft.com/en-us/entra/identity/ |
| **Microsoft Identity Web GitHub Repository** | https://github.com/AzureAD/microsoft-identity-web |
| **Microsoft Identity Platform Documentation** | https://learn.microsoft.com/en-us/entra/identity-platform/ |

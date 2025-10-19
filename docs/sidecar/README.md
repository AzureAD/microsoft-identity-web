# Microsoft Entra Identity Sidecar Documentation

This directory contains comprehensive documentation for the Microsoft Entra Identity Sidecar.

## Start Here

👉 **[Sidecar Overview](index.md)** — Overview, architecture, and key concepts

## Documentation

| Guide | Description |
|-------|-------------|
| [Overview](index.md) | Conceptual overview and architecture |
| [Installation](installation.md) | Deploy the sidecar container |
| [Configuration](configuration.md) | Configuration schema and options |
| [Endpoints](endpoints.md) | HTTP API reference |
| [Agent Identities](agent-identities.md) | Agent identity patterns |
| [Security](security.md) | Security best practices |
| [Comparison](comparison.md) | vs in-process Microsoft.Identity.Web |
| [Troubleshooting](troubleshooting.md) | Common issues and solutions |
| [FAQ](faq.md) | Frequently asked questions |

## Scenario Guides

See [scenarios/](scenarios/) for task-focused guides, or jump directly to:

- [Validate an Authorization Header](scenarios/validate-authorization-header.md)
- [Obtain an Authorization Header](scenarios/obtain-authorization-header.md)
- [Call a Downstream API](scenarios/call-downstream-api.md)
- [Use Managed Identity](scenarios/managed-identity.md)
- [Implement Long-Running OBO](scenarios/long-running-obo.md)
- [Use Signed HTTP Requests](scenarios/signed-http-request.md)
- [Agent Autonomous Batch Processing](scenarios/agent-autonomous-batch.md)
- [Integration from TypeScript](scenarios/using-from-typescript.md)
- [Integration from Python](scenarios/using-from-python.md)

## Additional Resources

- [Microsoft Entra ID Documentation](https://docs.microsoft.com/en-us/azure/active-directory/)
- [Microsoft Identity Web GitHub Repository](https://github.com/AzureAD/microsoft-identity-web)
- [Microsoft Identity Platform Documentation](https://docs.microsoft.com/en-us/azure/active-directory/develop/)
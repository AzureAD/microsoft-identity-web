# Microsoft Entra Identity Sidecar

## Overview

The Microsoft Entra Identity Sidecar is a containerized service that provides token acquisition and validation capabilities for applications running in distributed environments. It enables applications written in any language or framework to integrate with Microsoft Entra ID without requiring language-specific SDKs.

The sidecar pattern decouples authentication logic from your application code, providing:

- **Language Agnostic**: Call the sidecar from any language via HTTP APIs
- **Simplified Deployment**: Centralized authentication configuration and token management
- **Enhanced Security**: Token caching and secure credential storage isolated from application code
- **Reduced Dependencies**: No need for language-specific Microsoft.Identity.Web packages in your application

## Key Capabilities

### Token Acquisition
- Acquire access tokens for downstream APIs using various flows:
  - On-Behalf-Of (OBO) flow for user delegation scenarios
  - Client credentials flow for application-to-application scenarios
  - Managed Identity support for Azure-hosted applications
- Support for agent identities (autonomous and delegated scenarios)
- Token caching and lifecycle management

### Token Validation
- Validate incoming bearer tokens
- Decrypt encrypted tokens
- Extract and surface token claims

### Downstream API Integration
- Simplified HTTP calls to protected APIs with automatic token attachment
- Configurable request/response handling
- Support for Signed HTTP Requests (SHR)

## Architecture

The sidecar runs as a separate container alongside your application container, typically in the same pod in Kubernetes environments. Your application communicates with the sidecar over HTTP (usually via localhost) to:

1. Validate incoming tokens from client applications
2. Acquire tokens for calling downstream APIs
3. Make authenticated HTTP calls to downstream services

```
┌─────────────────────────────────────┐
│         Kubernetes Pod              │
│                                     │
│  ┌──────────────┐  ┌─────────────┐ │
│  │              │  │             │ │
│  │  Application │◄─┤  Sidecar    │ │
│  │  Container   │  │  Container  │ │
│  │              │  │             │ │
│  └──────────────┘  └─────────────┘ │
│                          │          │
└──────────────────────────┼──────────┘
                           │
                           ▼
                Microsoft Entra ID
```

## Getting Started

### Quick Links

- [Installation Guide](installation.md) - Deploy the sidecar container
- [Configuration Reference](configuration.md) - Configure authentication settings
- [Endpoints Reference](endpoints.md) - HTTP API documentation
- [Agent Identities](agent-identities.md) - Understand agent identity patterns
- [Security Best Practices](security.md) - Secure your sidecar deployment

### Common Scenarios

Explore task-focused guides:

- [Obtain an Authorization Header](scenarios/obtain-authorization-header.md)
- [Call a Downstream API](scenarios/call-downstream-api.md)
- [Use Managed Identity](scenarios/managed-identity.md)
- [Implement Long-Running OBO](scenarios/long-running-obo.md)
- [Use Signed HTTP Requests](scenarios/signed-http-request.md)
- [Agent Autonomous Batch Processing](scenarios/agent-autonomous-batch.md)
- [Integration from TypeScript](scenarios/using-from-typescript.md)
- [Integration from Python](scenarios/using-from-python.md)

## When to Use the Sidecar

**Use the sidecar when:**
- Building microservices in multiple languages that need Microsoft Entra authentication
- Deploying in Kubernetes or container orchestration environments
- Wanting to centralize authentication configuration
- Building applications in languages without Microsoft.Identity.Web support

**Use in-process Microsoft.Identity.Web when:**
- Building ASP.NET Core applications exclusively
- Wanting maximum performance with zero HTTP overhead
- Requiring direct access to MSAL.NET features
- Working in non-containerized environments

See [Comparison with Microsoft.Identity.Web](comparison.md) for detailed migration guidance.

## Support and Resources

- [Troubleshooting Guide](troubleshooting.md)
- [Frequently Asked Questions](faq.md)
- [Reverse Proxy Integration](reverse-proxy.md)
- [Microsoft Entra ID Documentation](https://docs.microsoft.com/en-us/azure/active-directory/)
- [Microsoft Identity Web GitHub Repository](https://github.com/AzureAD/microsoft-identity-web)

## Container Image

The sidecar is available as a container image:

```
mcr.microsoft.com/identity/sidecar:<tag>
```

> **Note**: The container image is currently in preview. Check the [installation guide](installation.md) for the latest image coordinates and version information.

## License

The Microsoft Entra Identity Sidecar is released under the MIT License.

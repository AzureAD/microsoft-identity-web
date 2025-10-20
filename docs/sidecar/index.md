# Microsoft Entra Identity Sidecar

## Overview

The Microsoft Entra Identity Sidecar is a containerized service that handles token acquisition, validation, and secure downstream calls so your application code stays focused on business logic. You offload identity concerns to a companion container.


## Architecture

A typical flow: a client calls your Web API. The Web API delegates identity operations to the Sidecar via its HTTP endpoints. The Sidecar validates inbound tokens (/Validate), acquires tokens (/AuthorizationHeader and /AuthorizationHeaderUnauthenticated), and can directly invoke downstream APIs (/DownstreamApi and /DownstreamApiUnauthenticated). It interacts with Microsoft Entra ID for token issuance and Open ID Connect metadata retrieval.

```mermaid
%%{init: {
  "theme": "base",
  "themeVariables": {
    "background": "#121212",
    "primaryColor": "#1E1E1E",
    "primaryBorderColor": "#FFFFFF",
    "primaryTextColor": "#FFFFFF",
    "textColor": "#FFFFFF",
    "lineColor": "#FFFFFF",
    "labelBackground": "#000000"
  }
}}%%
flowchart LR
    classDef dnode fill:#1E1E1E,stroke:#FFFFFF,stroke-width:2px,color:#FFFFFF
    linkStyle default stroke:#FFFFFF,stroke-width:2px,color:#FFFFFF

    client[Client Application]:::dnode -->| Bearer over HTTP | webapi[Web API]:::dnode
    subgraph Pod / Host
        webapi -->|"/Validate<br/>/AuthorizationHeader/{name}<br/>/DownstreamApi/{name}"| sidecar[Identity Sidecar]:::dnode
    end
    sidecar -->|Token validation & acquisition| entra[Microsoft Entra ID]:::dnode
```

Benefits:

- **Language agnostic** – Call over HTTP from any stack.
- **Centralized config** – One place for identity settings and secrets.
- **Improved security posture** – Keep tokens and credentials out of app code.
- **Lower dependency footprint** – No Microsoft.Identity.Web library needed in non-.NET apps.

## Key Capabilities

### Token Validation
- Validate bearer tokens
- (Optional) decrypt encrypted tokens
- Surface claims for authorization decisions

### Token Acquisition / Authorization header creation
- On-Behalf-Of OAuth 2.0 flow (user delegation)
- Client credentials OAuth 2.0 flow (application-to-application)
- Managed identity (Azure hosting scenarios)
- Agent identities (autonomous or delegated)
- Caching and lifecycle management

### Downstream API Calls
- Acquire and attach tokens automatically
- Optional request overrides (scopes, method, headers)
- Signed HTTP Requests (PoP/SHR) support

## When to Use the Sidecar

### Use the Sidecar when:
- You have polyglot microservices.
- You run in containers (Kubernetes, Docker).
- You want a consistent identity surface across services.
- You need agent identity patterns in languages other than .NET
- You need Entra token validation in languages other than .NET
- You prefer isolating secrets and token handling.

### Use in-process Microsoft.Identity.Web when:
- All services are in .NET.
- You want maximum performance (no extra hop).
- You are not containerized.

See the **[Comparison Guide](comparison.md)** for nuances.

## Getting Started

Next steps:
1. **Install** – Pull and run the container: [Installation](installation.md)
2. **Configure** – Define identity settings: [Configuration](configuration.md)
3. **Call Endpoints** – Use the HTTP API: [Endpoints](endpoints.md)
4. **Pick a Scenario** – Try a focused guide: [README navigation](README.md)

## Container Image

```
mcr.microsoft.com/entra-sdk/auth-sidecar:<tag>
```

> Note: The image is currently in preview. See [Installation](installation.md) for supported tags.

## License

MIT License.

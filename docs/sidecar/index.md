# Microsoft Entra Identity Sidecar

The Microsoft Entra Identity Sidecar externalizes token acquisition and downstream API invocation so your application (any language) can call simple HTTP endpoints instead of embedding token plumbing.

Core capabilities:
- Acquire authorization headers for configured downstream APIs (user, app, or agent-related contexts).
- Proxy calls to downstream APIs with consistent logging and normalization.
- Validate incoming tokens.
- Support advanced overrides: scopes, tenant, force refresh, Signed HTTP Request (SHR), long-running OBO session keys, managed identity options.

## Quick Start

1. Deploy the sidecar container adjacent to your application (Docker Compose, Kubernetes sidecar).
2. Provide Azure AD application configuration (tenant, client id, credentials or managed identity).
3. Define downstream API entries (scopes, base URL, default method/path).
4. From your app:
   - Get a token header: `GET /AuthorizationHeader/{apiName}`
   - Or proxy a call: `POST /DownstreamApi/{apiName}`

## Key Endpoints

| Endpoint | Purpose |
|----------|---------|
| `GET /Validate` | Validate & parse an inbound token |
| `GET /AuthorizationHeader/{apiName}` | Acquire an authorization header (authenticated request) |
| `GET /AuthorizationHeaderUnauthenticated/{apiName}` | Acquire an app (client) token without inbound user |
| `POST /DownstreamApi/{apiName}` | Proxy a configured downstream API call (authenticated) |
| `POST /DownstreamApiUnauthenticated/{apiName}` | Proxy using app (client) credentials |

## Agent Identities

Agent identity selection is purely request-based: add `AgentIdentity`, `AgentUsername`, or `AgentUserId` query parameters (with the required combinations—see agent-identities doc). No additional config section is required. See [agent-identities.md](agent-identities.md).

## Documentation Map

- [Installation](installation.md)
- [Configuration](configuration.md)
- [Agent Identities](agent-identities.md)
- [Endpoints](endpoints.md)
- [Scenarios](scenarios/obtain-authorization-header.md)
- [Reverse Proxy Deep Dive](reverse-proxy.md)
- [Security & Hardening](security.md)
- [Troubleshooting](troubleshooting.md)
- [FAQ](faq.md)
- [Comparison & Migration](comparison.md)

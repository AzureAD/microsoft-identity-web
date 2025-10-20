# Endpoints Reference

This document provides reference for the HTTP endpoints exposed by the Microsoft Entra Identity Sidecar.

**OpenAPI specification**: Available at `/openapi/v1.json` (development environment) and in the repository: https://github.com/AzureAD/microsoft-identity-web/blob/master/src/Microsoft.Identity.Web.Sidecar/OpenAPI/Microsoft.Identity.Web.Sidecar.json
Use it to:
- Generate client code
- Validate requests
- Discover available endpoints

## Endpoint Overview

| Endpoint | Method(s) | Purpose | Auth Required |
|----------|-----------|---------|---------------|
| `/Validate` | GET | Validate an inbound bearer token and return claims | Yes |
| `/AuthorizationHeader/{serviceName}` | GET | Validate inbound token (if present) and acquire an authorization header for a downstream API | Yes |
| `/AuthorizationHeaderUnauthenticated/{serviceName}` | GET | Acquire an authorization header (app or agent identity) with no inbound user token | Yes |
| `/DownstreamApi/{serviceName}` | GET, POST, PUT, PATCH, DELETE | Validate inbound token (if present) and call downstream API with automatic token acquisition | Yes |
| `/DownstreamApiUnauthenticated/{serviceName}` | GET, POST, PUT, PATCH, DELETE | Call downstream API (app or agent identity only) | Yes |
| `/healthz` | GET | Health probe (liveness/readiness) | No |
| `/openapi/v1.json` | GET | OpenAPI 3.0 document | No (Dev only) |

## Authentication

All token acquisition and validation endpoints require a bearer token in the `Authorization` header unless explicitly marked unauthenticated.

```http
GET /AuthorizationHeader/Graph
Authorization: Bearer eyJ0eXAiOiJKV1QiLCJhbGc...
```

Tokens are validated against configured Microsoft Entra ID settings (tenant, audience, issuer, scopes if enabled).

---
## /Validate

Validates the inbound bearer token and returns its claims.

### Request
```http
GET /Validate HTTP/1.1
Authorization: Bearer eyJ0eXAiOiJKV1QiLCJhbGc...
```

### Successful Response (200)
```json
{
  "protocol": "Bearer",
  "token": "eyJ0eXAiOiJKV1QiLCJhbGc...",
  "claims": {
    "aud": "api://your-api-id",
    "iss": "https://sts.windows.net/tenant-id/",
    "iat": 1234567890,
    "nbf": 1234567890,
    "exp": 1234571490,
    "acr": "1",
    "appid": "client-id",
    "appidacr": "1",
    "idp": "https://sts.windows.net/tenant-id/",
    "oid": "user-object-id",
    "tid": "tenant-id",
    "scp": "access_as_user"
    "sub": "subject",
    "ver": "1.0"
  }
}
```

### Error Examples
```json
// 400 Bad Request - No token
{ "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1", "title": "Bad Request", "status": 400, "detail": "No token found" }

// 401 Unauthorized - Invalid token
{ "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1", "title": "Unauthorized", "status": 401 }
```

---
## /AuthorizationHeader/{serviceName}

Acquires an access token for the configured downstream API and returns it as an authorization header value. If a user bearer token is provided inbound, OBO (delegated) is used; otherwise app context patterns apply (if enabled).

### Path Parameter
- `serviceName` – Name of the downstream API in configuration.

### Query Parameters

#### Standard Overrides
| Parameter | Type | Description | Example |
|-----------|------|-------------|---------|
| `optionsOverride.Scopes` | string[] | Override configured scopes (repeatable) | `?optionsOverride.Scopes=User.Read&optionsOverride.Scopes=Mail.Read` |
| `optionsOverride.RequestAppToken` | boolean | Force app-only token (skip OBO) | `?optionsOverride.RequestAppToken=true` |
| `optionsOverride.AcquireTokenOptions.Tenant` | string | Override tenant ID | `?optionsOverride.AcquireTokenOptions.Tenant=tenant-guid` |
| `optionsOverride.AcquireTokenOptions.PopPublicKey` | string | Enable PoP/SHR (base64 public key) | `?optionsOverride.AcquireTokenOptions.PopPublicKey=base64key` |
| `optionsOverride.AcquireTokenOptions.PopClaims` | string | Additional PoP claims (JSON) | `?optionsOverride.AcquireTokenOptions.PopClaims={"nonce":"abc"}` |

#### Agent Identity
| Parameter | Type | Description | Example |
|-----------|------|-------------|---------|
| `AgentIdentity` | string | Agent app (client) ID | `?AgentIdentity=11111111-2222-3333-4444-555555555555` |
| `AgentUsername` | string | User principal name (delegated agent) | `?AgentIdentity=<id>&AgentUsername=user@contoso.com` |
| `AgentUserId` | string | User object ID (delegated agent) | `?AgentIdentity=<id>&AgentUserId=aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee` |

Rules:
- `AgentUsername` or `AgentUserId` require `AgentIdentity` (user agent).
- `AgentUsername` and `AgentUserId` are mutually exclusive.
- `AgentIdentity` alone = autonomous agent.
- `AgentIdentity` + user inbound token = delegated agent.

### Examples
```http
GET /AuthorizationHeader/Graph HTTP/1.1
Authorization: Bearer eyJ0eXAiOiJKV1QiLCJhbGc...
```
```http
GET /AuthorizationHeader/Graph?optionsOverride.RequestAppToken=true HTTP/1.1
Authorization: Bearer eyJ0eXAiOiJKV1QiLCJhbGc...
```
```http
GET /AuthorizationHeader/Graph?AgentIdentity=agent-id HTTP/1.1
Authorization: Bearer eyJ0eXAiOiJKV1QiLCJhbGc...
```

### Responses
```json
{ "authorizationHeader": "Bearer eyJ0eXAiOiJKV1QiLCJhbGc..." }
```
PoP / SHR example:
```json
{ "authorizationHeader": "PoP eyJ0eXAiOiJhdCtqd3QiLCJhbGc..." }
```

---
## /AuthorizationHeaderUnauthenticated/{serviceName}

Same behavior and parameters as `/AuthorizationHeader/{serviceName}` but no inbound user token is expected. Used for app-only or autonomous/agent identity acquisition without a user context. Avoids the overhead of validating a user token.

---
## /DownstreamApi/{serviceName}

Acquires an access token and performs an HTTP request to the downstream API. Returns status code, headers, and body from the downstream response. Supports user OBO, app-only, or agent identity patterns.

### Path Parameter
- `serviceName` – Configured downstream API name.

### Additional Query Parameters (in addition to `/AuthorizationHeader` parameters)
| Parameter | Type | Description | Example |
|-----------|------|-------------|---------|
| `optionsOverride.HttpMethod` | string | Override HTTP method | `?optionsOverride.HttpMethod=POST` |
| `optionsOverride.RelativePath` | string | Append relative path to configured BaseUrl | `?optionsOverride.RelativePath=me/messages` |
| `optionsOverride.CustomHeader.<Name>` | string | Add custom header(s) | `?optionsOverride.CustomHeader.X-Custom=value` |

### Request Body Forwarding
Body is passed through unchanged:
```http
POST /DownstreamApi/Graph?optionsOverride.RelativePath=me/messages HTTP/1.1
Authorization: Bearer eyJ0eXAiOiJKV1QiLCJhbGc...
Content-Type: application/json

{ "subject": "Hello", "body": { "contentType": "Text", "content": "Hello world" } }
```

### Response (Example)
```json
{
  "statusCode": 200,
  "headers": { "content-type": "application/json" },
  "content": "{\"@odata.context\":\"...\",\"displayName\":\"...\"}"
}
```

Errors mirror `/AuthorizationHeader` plus downstream API error status codes.

---
## /DownstreamApiUnauthenticated/{serviceName}

Same as `/DownstreamApi/{serviceName}` but no inbound user token is validated. Use for app-only or autonomous agent operations.

---
## /healthz

Basic health probe.
- 200 OK when healthy
- 503 Service Unavailable when failing internal checks

---
## /openapi/v1.json

Returns OpenAPI 3.0 specification (development environment only).

---
## Error Patterns

### Common Error Responses
```json
// 400 Bad Request - Missing service name
{ "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1", "title": "Bad Request", "status": 400, "detail": "Service name is required" }

// 400 Bad Request - Invalid agent combination
{ "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1", "title": "Bad Request", "status": 400, "detail": "AgentUsername and AgentUserId are mutually exclusive" }

// 401 Unauthorized - Invalid token
{ "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1", "title": "Unauthorized", "status": 401 }

// 403 Forbidden - Missing scope
{ "type": "https://tools.ietf.org/html/rfc7231#section-6.5.3", "title": "Forbidden", "status": 403, "detail": "The scope 'access_as_user' is required" }

// 404 Not Found - Service not configured
{ "type": "https://tools.ietf.org/html/rfc7231#section-6.5.4", "title": "Not Found", "status": 404, "detail": "Downstream API 'UnknownService' not configured" }

// 500 Internal Server Error - Token acquisition failure
{ "type": "https://tools.ietf.org/html/rfc7231#section-6.6.1", "title": "Internal Server Error", "status": 500, "detail": "Failed to acquire token for downstream API" }
```

### MSAL Error Example
```json
{ "type": "https://tools.ietf.org/html/rfc7231#section-6.6.1", "title": "Internal Server Error", "status": 500, "detail": "MSAL.NetCore.invalid_grant: AADSTS50076: Due to a configuration change ...", "extensions": { "errorCode": "invalid_grant", "correlationId": "..." } }
```

---
## Complete Override Reference

```text
optionsOverride.Scopes=<scope>                     # Repeatable
optionsOverride.RequestAppToken=<true|false>
optionsOverride.BaseUrl=<url>
optionsOverride.RelativePath=<path>
optionsOverride.HttpMethod=<method>
optionsOverride.AcquireTokenOptions.Tenant=<tenant-id>
optionsOverride.AcquireTokenOptions.AuthenticationScheme=<scheme>
optionsOverride.AcquireTokenOptions.CorrelationId=<guid>
optionsOverride.AcquireTokenOptions.PopPublicKey=<base64-key>
optionsOverride.AcquireTokenOptions.PopClaims=<json>
optionsOverride.CustomHeader.<Name>=<value>

AgentIdentity=<agent-client-id>
AgentUsername=<user-upn>            # Requires AgentIdentity
AgentUserId=<user-object-id>        # Requires AgentIdentity
```

### Examples of override

**Override scopes**:
```http
GET /AuthorizationHeader/Graph?optionsOverride.Scopes=User.Read&optionsOverride.Scopes=Mail.Read HTTP/1.1
Authorization: Bearer eyJ0eXAiOiJKV1QiLCJhbGc...
```

## Rate Limiting
The Sidecar itself does not impose rate limits. Effective limits come from:
1. Microsoft Entra ID token service throttling (shouldn't happen as the sidecar caches token)
2. Downstream API limits
3. Token cache efficiency (reduces acquisition volume)

## Best Practices
1. Prefer configuration over ad-hoc overrides.
2. Keep service names static and declarative.
3. Implement retry policies for transient failures (HTTP 500/503).
4. Validate agent parameters before calling.
5. Log correlation IDs for tracing across services.
6. Monitor token acquisition latency and error rates.
7. Use health probes in orchestration platforms.

## Next Steps
- [Configuration](configuration.md)
- [Agent Identities](agent-identities.md)
- [Security](security.md)
- [Scenarios](README.md#scenario-guides)
- [Troubleshooting](troubleshooting.md)

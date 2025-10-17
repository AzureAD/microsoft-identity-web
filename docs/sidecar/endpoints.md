# Endpoints Reference

This document provides comprehensive reference for all HTTP endpoints exposed by the Microsoft Entra Identity Sidecar.

## Endpoint Overview

The sidecar exposes the following endpoints:

| Endpoint | Method | Purpose | Auth Required |
|----------|--------|---------|---------------|
| `/Validate` | GET | Validate incoming bearer token | Yes |
| `/AuthorizationHeader/{serviceName}` | GET | Get authorization header for downstream API | Yes |
| `/DownstreamApi/{serviceName}` | GET, POST, PUT, PATCH, DELETE | Call downstream API with automatic token acquisition | Yes |
| `/health` | GET | Health check endpoint | No |
| `/openapi/v1.json` | GET | OpenAPI specification | No (Dev only) |

## Authentication

All token acquisition and validation endpoints require authentication via bearer token in the Authorization header:

```http
GET /AuthorizationHeader/Graph
Authorization: Bearer eyJ0eXAiOiJKV1QiLCJhbGc...
```

The bearer token is validated against the configured Azure AD settings and must contain the required scopes if scope validation is enabled.

## /Validate

Validates the incoming bearer token and returns its claims.

### Request

```http
GET /Validate HTTP/1.1
Authorization: Bearer eyJ0eXAiOiJKV1QiLCJhbGc...
```

### Response

**Success (200 OK)**:

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
    "aio": "...",
    "appid": "client-id",
    "appidacr": "1",
    "idp": "https://sts.windows.net/tenant-id/",
    "oid": "user-object-id",
    "rh": "...",
    "sub": "subject",
    "tid": "tenant-id",
    "uti": "...",
    "ver": "1.0",
    "scp": "access_as_user"
  }
}
```

**Error (400 Bad Request)**:

```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "Bad Request",
  "status": 400,
  "detail": "No token found"
}
```

**Error (401 Unauthorized)**:

Invalid or missing token.

## /AuthorizationHeader/{serviceName}

Acquires an access token for the specified downstream API and returns it formatted as an Authorization header value.

### Path Parameters

- `serviceName` - The name of the downstream API as configured in `DownstreamApis` section

### Query Parameters

#### Standard Override Parameters

| Parameter | Type | Description | Example |
|-----------|------|-------------|---------|
| `optionsOverride.Scopes` | string[] | Override scopes (can be repeated) | `?optionsOverride.Scopes=User.Read&optionsOverride.Scopes=Mail.Read` |
| `optionsOverride.RequestAppToken` | boolean | Request application token instead of OBO | `?optionsOverride.RequestAppToken=true` |
| `optionsOverride.AcquireTokenOptions.Tenant` | string | Override tenant ID | `?optionsOverride.AcquireTokenOptions.Tenant=tenant-guid` |
| `optionsOverride.AcquireTokenOptions.PopPublicKey` | string | Enable SHR with public key (base64) | `?optionsOverride.AcquireTokenOptions.PopPublicKey=base64key` |
| `optionsOverride.AcquireTokenOptions.PopClaims` | string | Additional PoP claims (JSON) | `?optionsOverride.AcquireTokenOptions.PopClaims={"claim":"value"}` |

#### Agent Identity Parameters

| Parameter | Type | Description | Example |
|-----------|------|-------------|---------|
| `AgentIdentity` | string | Agent application client ID | `?AgentIdentity=12345678-...` |
| `AgentUsername` | string | User principal name for delegated agent | `?AgentUsername=user@contoso.com` |
| `AgentUserId` | string | User object ID (GUID) for delegated agent | `?AgentUserId=87654321-...` |

**Agent Identity Rules**:
- `AgentUsername` or `AgentUserId` **require** `AgentIdentity`
- `AgentUsername` and `AgentUserId` are **mutually exclusive**
- `AgentIdentity` alone = autonomous agent (application context)
- `AgentIdentity` + `AgentUsername`/`AgentUserId` = delegated agent (user context)

See [Agent Identities](agent-identities.md) for detailed semantics.

### Request Examples

**Basic request**:
```http
GET /AuthorizationHeader/Graph HTTP/1.1
Authorization: Bearer eyJ0eXAiOiJKV1QiLCJhbGc...
```

**Override scopes**:
```http
GET /AuthorizationHeader/Graph?optionsOverride.Scopes=User.Read&optionsOverride.Scopes=Mail.Read HTTP/1.1
Authorization: Bearer eyJ0eXAiOiJKV1QiLCJhbGc...
```

**Request application token**:
```http
GET /AuthorizationHeader/Graph?optionsOverride.RequestAppToken=true HTTP/1.1
Authorization: Bearer eyJ0eXAiOiJKV1QiLCJhbGc...
```

**Multi-tenant override**:
```http
GET /AuthorizationHeader/Graph?optionsOverride.AcquireTokenOptions.Tenant=tenant-guid HTTP/1.1
Authorization: Bearer eyJ0eXAiOiJKV1QiLCJhbGc...
```

**Autonomous agent**:
```http
GET /AuthorizationHeader/Graph?AgentIdentity=agent-client-id HTTP/1.1
Authorization: Bearer eyJ0eXAiOiJKV1QiLCJhbGc...
```

**Autonomous agent with user agent identity specified by username**:
```http
GET /AuthorizationHeader/Graph?AgentIdentity=agent-client-id&AgentUsername=user@contoso.com HTTP/1.1
Authorization: Bearer eyJ0eXAiOiJKV1QiLCJhbGc...
```

**Signed HTTP Request (SHR)**:
```http
GET /AuthorizationHeader/Graph?optionsOverride.AcquireTokenOptions.PopPublicKey=base64EncodedPublicKey HTTP/1.1
Authorization: Bearer eyJ0eXAiOiJKV1QiLCJhbGc...
```

### Response

**Success (200 OK)**:

```json
{
  "authorizationHeader": "Bearer eyJ0eXAiOiJKV1QiLCJhbGc..."
}
```

For SHR requests, the authorization header will use the PoP scheme:

```json
{
  "authorizationHeader": "PoP eyJ0eXAiOiJhdCtqd3QiLCJhbGc..."
}
```

**Error Responses**:

```json
// 400 Bad Request - Invalid parameters
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "Bad Request",
  "status": 400,
  "detail": "AgentUsername requires AgentIdentity to be specified"
}

// 401 Unauthorized - Token validation failed
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "Unauthorized",
  "status": 401
}

// 404 Not Found - Service name not configured
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.4",
  "title": "Not Found",
  "status": 404,
  "detail": "Downstream API 'UnknownService' not configured"
}
```

## /DownstreamApi/{serviceName}

Acquires an access token and makes an HTTP request to the downstream API, returning the response.

### Path Parameters

- `serviceName` - The name of the downstream API as configured in `DownstreamApis` section of the configuration

### Query Parameters

Supports all parameters from `/AuthorizationHeader/{serviceName}` plus:

| Parameter | Type | Description | Example |
|-----------|------|-------------|---------|
| `optionsOverride.HttpMethod` | string | HTTP method to use | `?optionsOverride.HttpMethod=POST` |
| `optionsOverride.RelativePath` | string | Relative path to append to BaseUrl | `?optionsOverride.RelativePath=me/messages` |
| `optionsOverride.CustomHeader.<name>` | string | Custom header to add | `?optionsOverride.CustomHeader.X-Custom=value` |

### Request Body

For POST, PUT, and PATCH requests, the request body is forwarded to the downstream API:

```http
POST /DownstreamApi/Graph?optionsOverride.RelativePath=me/messages HTTP/1.1
Authorization: Bearer eyJ0eXAiOiJKV1QiLCJhbGc...
Content-Type: application/json

{
  "subject": "Test Message",
  "body": {
    "contentType": "Text",
    "content": "Hello, World!"
  },
  "toRecipients": [
    {
      "emailAddress": {
        "address": "recipient@contoso.com"
      }
    }
  ]
}
```

### Request Examples

**GET request**:
```http
GET /DownstreamApi/Graph?optionsOverride.RelativePath=me HTTP/1.1
Authorization: Bearer eyJ0eXAiOiJKV1QiLCJhbGc...
```

**POST request**:
```http
POST /DownstreamApi/MyApi?optionsOverride.RelativePath=items HTTP/1.1
Authorization: Bearer eyJ0eXAiOiJKV1QiLCJhbGc...
Content-Type: application/json

{
  "name": "New Item",
  "description": "Item description"
}
```

**Custom headers**:
```http
GET /DownstreamApi/Graph?optionsOverride.RelativePath=me&optionsOverride.CustomHeader.X-Custom=value HTTP/1.1
Authorization: Bearer eyJ0eXAiOiJKV1QiLCJhbGc...
```

**Agent with relative path**:
```http
GET /DownstreamApi/Graph?AgentIdentity=agent-id&optionsOverride.RelativePath=users HTTP/1.1
Authorization: Bearer eyJ0eXAiOiJKV1QiLCJhbGc...
```

### Response

**Success (200 OK)**:

```json
{
  "statusCode": 200,
  "headers": {
    "content-type": "application/json; charset=utf-8",
    "request-id": "...",
    "client-request-id": "...",
    "x-ms-ags-diagnostic": "...",
    "date": "..."
  },
  "content": "{\"@odata.context\":\"...\",\"displayName\":\"...\",\"mail\":\"...\"}"
}
```

The response includes:
- `statusCode` - HTTP status code from downstream API
- `headers` - Response headers from downstream API
- `content` - Response body from downstream API (as string)

**Error Responses**:

Similar to `/AuthorizationHeader/{serviceName}`, plus downstream API errors are returned with their original status codes.

## /health

Health check endpoint for liveness and readiness probes.

### Request

```http
GET /health HTTP/1.1
```

### Response

**Healthy (200 OK)**:

```json
{
  "status": "Healthy"
}
```

**Unhealthy (503 Service Unavailable)**:

```json
{
  "status": "Unhealthy",
  "errors": ["Configuration error", "Unable to connect to Microsoft Entra ID"]
}
```

## /openapi/v1.json

OpenAPI specification for the sidecar API (available in Development environment only).

### Request

```http
GET /openapi/v1.json HTTP/1.1
```

### Response

Returns OpenAPI 3.0 specification as JSON.

## Error Patterns

### Common Error Responses

**400 Bad Request - Missing Required Parameter**:
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "Bad Request",
  "status": 400,
  "detail": "Service name is required"
}
```

**400 Bad Request - Invalid Agent Parameter Combination**:
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "Bad Request",
  "status": 400,
  "detail": "AgentUsername and AgentUserId are mutually exclusive"
}
```

**400 Bad Request - Agent Parameter Without Identity**:
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "Bad Request",
  "status": 400,
  "detail": "AgentUsername requires AgentIdentity to be specified"
}
```

**401 Unauthorized - Invalid Token**:
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "Unauthorized",
  "status": 401
}
```

**403 Forbidden - Insufficient Scopes**:
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.3",
  "title": "Forbidden",
  "status": 403,
  "detail": "The scope 'access_as_user' is required"
}
```

**404 Not Found - Service Not Configured**:
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.4",
  "title": "Not Found",
  "status": 404,
  "detail": "Downstream API 'UnknownService' not configured"
}
```

**500 Internal Server Error - Token Acquisition Failed**:
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.6.1",
  "title": "Internal Server Error",
  "status": 500,
  "detail": "Failed to acquire token for downstream API"
}
```

### MSAL Errors

When token acquisition fails, MSAL error details may be included:

```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.6.1",
  "title": "Internal Server Error",
  "status": 500,
  "detail": "MSAL.NetCore.4.56.0.invalid_grant: AADSTS50076: Due to a configuration change made by your administrator, or because you moved to a new location, you must use multi-factor authentication to access...",
  "extensions": {
    "errorCode": "invalid_grant",
    "correlationId": "..."
  }
}
```

## Complete Override Reference

### Token Acquisition Overrides

```
optionsOverride.Scopes=<scope>                          # Can be repeated
optionsOverride.RequestAppToken=<true|false>
optionsOverride.BaseUrl=<url>
optionsOverride.RelativePath=<path>
optionsOverride.HttpMethod=<method>

optionsOverride.AcquireTokenOptions.Tenant=<tenant-id>
optionsOverride.AcquireTokenOptions.AuthenticationScheme=<scheme>
optionsOverride.AcquireTokenOptions.CorrelationId=<guid>
optionsOverride.AcquireTokenOptions.PopPublicKey=<base64-key>
optionsOverride.AcquireTokenOptions.PopClaims=<json>

optionsOverride.CustomHeader.<name>=<value>             # Custom headers
```

### Agent Identity Parameters

```
AgentIdentity=<agent-client-id>
AgentUsername=<user-upn>                                # Requires AgentIdentity
AgentUserId=<user-object-id>                            # Requires AgentIdentity
```

## Rate Limiting

The sidecar does not implement rate limiting itself but relies on:

1. **Microsoft Entra ID throttling**: Token requests are subject to Microsoft Entra ID rate limits
2. **Downstream API limits**: API calls are subject to the target API's rate limiting
3. **Token caching**: Reduces token acquisition requests through intelligent caching

Monitor token cache hit rates and adjust cache configuration if needed.

## Best Practices

1. **Use Configuration Over Overrides**: Configure downstream APIs in settings; use overrides for exceptional cases
2. **Cache Service Names**: Don't construct service names dynamically; use configured names
3. **Handle All Error Codes**: Implement retry logic for transient errors (500, 503)
4. **Validate Agent Parameters**: Check agent identity parameter combinations before making requests
5. **Monitor Token Acquisition**: Track token acquisition latency and failure rates
6. **Use Health Checks**: Configure proper liveness and readiness probes
7. **Log Correlation IDs**: Use correlation IDs for request tracing across services

## Next Steps

- [Configuration Reference](configuration.md) - Configure downstream APIs
- [Agent Identities](agent-identities.md) - Understand agent identity patterns
- [Security Best Practices](security.md) - Secure your endpoints
- [Scenarios](scenarios/README.md) - Practical usage examples
- [Troubleshooting](troubleshooting.md) - Resolve endpoint errors

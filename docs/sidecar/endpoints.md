# Endpoints Reference (Updated Semantics)

## Overview

| Endpoint | Method | Inbound Auth | Description |
|----------|--------|--------------|-------------|
| `/Validate` | GET | Bearer (caller supplies) | Validates and returns token structure/claims (dev use). |
| `/AuthorizationHeader/{apiName}` | GET | Bearer (user or agent) | Returns JSON containing `authorizationHeader`. Supports agent + optional user delegation. |
| `/AuthorizationHeaderUnauthenticated/{apiName}` | GET | None | App (client) token without inbound user context. |
| `/DownstreamApi/{apiName}` | POST | Bearer | Proxies downstream call using inbound or agent(+optional user) context. |
| `/DownstreamApiUnauthenticated/{apiName}` | POST | None | Proxies downstream call using app (client) credentials. |

## Agent & User Parameters Rules

| Parameter Combo | Allowed | Effect |
|-----------------|---------|--------|
| `AgentIdentity` | Yes | Autonomous agent (app context) |
| `AgentIdentity` + `AgentUsername` | Yes | Agent + delegated user (UPN) |
| `AgentIdentity` + `AgentUserId` | Yes | Agent + delegated user (OID) |
| `AgentUsername` alone | No | 400 error |
| `AgentUserId` alone | No | 400 error |
| `AgentUsername` + `AgentUserId` | No | 400 error |

## Common Query Overrides (Representative)

| Override | Purpose | Notes |
|----------|---------|-------|
| `optionsOverride.Scopes` (repeat) | Add/augment scopes | Repeat parameter to accumulate scopes. |
| `optionsOverride.RequestAppToken` | Force client credentials token | Boolean. |
| `optionsOverride.BaseUrl` / `RelativePath` | Override target URL parts | Proxy endpoints only. |
| `optionsOverride.HttpMethod` | Override method | Proxy endpoints. |
| `optionsOverride.AcceptHeader` | Set Accept header | Proxy; value used for outbound call. |
| `optionsOverride.ContentType` | Set body serialization content-type | Proxy. |
| `optionsOverride.AcquireTokenOptions.Tenant` | Tenant override | Multi-tenant or common scenarios. |
| `optionsOverride.AcquireTokenOptions.ForceRefresh` | Bypass cache | Use sparingly. |
| `optionsOverride.AcquireTokenOptions.Claims` | Provide claims challenge response | CAE / step-up. |
| `optionsOverride.AcquireTokenOptions.CorrelationId` | Trace correlation | GUID. |
| `optionsOverride.AcquireTokenOptions.LongRunningWebApiSessionKey` | Long-running OBO continuity | Stable key. |
| `optionsOverride.AcquireTokenOptions.ManagedIdentity.UserAssignedClientId` | Choose user-assigned MI | Azure only. |
| `optionsOverride.AcquireTokenOptions.PopPublicKey` | Signed HTTP Request (SHR) key | URL-encoded JWK. |
| `AgentIdentity` | Agent blueprint selection | Enables autonomous or delegated via following param. |
| `AgentUsername` | Delegated user (UPN) — requires AgentIdentity | Mutually exclusive with `AgentUserId`. |
| `AgentUserId` | Delegated user (OID) — requires AgentIdentity | Mutually exclusive with `AgentUsername`. |

## Response Schemas

### AuthorizationHeaderResult
```json
{ "authorizationHeader": "Bearer <token>" }
```

### DownstreamApiResult
```json
{
  "statusCode": 200,
  "headers": { "content-type": ["application/json"] },
  "content": "{...raw body...}"
}
```

### ProblemDetails
```json
{
  "type": "...",
  "title": "...",
  "status": 400,
  "detail": "...",
  "instance": "..."
}
```

## Examples

Autonomous agent header:
```
GET /AuthorizationHeader/Graph?AgentIdentity=Scheduler
```

Agent + user (OID) with correlation:
```
GET /AuthorizationHeader/Graph?AgentIdentity=Scheduler&AgentUserId=<oid>&optionsOverride.AcquireTokenOptions.CorrelationId=<guid>
```

Proxy with path override and SHR:
```
POST /DownstreamApi/Graph?AgentIdentity=Scheduler&optionsOverride.HttpMethod=GET&optionsOverride.RelativePath=/v1.0/me/calendar/events&optionsOverride.AcquireTokenOptions.PopPublicKey=<url-encoded-jwk>
```

Invalid (will 400):
```
GET /AuthorizationHeader/Graph?AgentUserId=<oid>
```

## Error Patterns (Additions)

| HTTP | Condition | Notes |
|------|-----------|-------|
| 400 | User param without AgentIdentity | Violates parameter rules |
| 400 | Both AgentUsername & AgentUserId | Mutually exclusive |
| 400 | Unknown apiName | Not configured |
| 401 | Missing/invalid bearer (auth variants) | Supply correct token |
| 200 + DownstreamApiResult.statusCode != 2xx | Downstream error | Inspect `content` for payload |

See [troubleshooting.md](troubleshooting.md) for remedies.

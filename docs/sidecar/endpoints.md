# Endpoints Reference

## 1. Overview

| Endpoint | Method | Inbound Auth | Description | Typical Use |
|----------|--------|--------------|-------------|-------------|
| `/Validate` | GET | Authorization header forwarded from web API | Returns protocol, raw token, parsed claims. | Delegate part of token validation / extract claims for enhanced authorization decisions |
| `/AuthorizationHeader/{apiName}` | GET | Bearer | Returns `authorizationHeader` using caller or agent (optionally with user) context. | App obtains an authorization header (protocol + token) then calls downstream directly |
| `/AuthorizationHeaderUnauthenticated/{apiName}` | GET | None | Returns authorization header to call downstream APIs on behalf of the app. | Daemons / jobs without inbound user |
| `/DownstreamApi/{apiName}` | POST | Bearer | Sidecar proxies downstream call with caller / agent context (handles auth). | Centralize outbound call policy/logging |
| `/DownstreamApiUnauthenticated/{apiName}` | POST | None | Proxies downstream call using app credentials. | Simple backend-to-backend call |

### Quick Decision Matrix

| Need | Use |
|------|-----|
| Web API delegating some validation of inbound authentication | `/Validate` |
| Lowest latency & custom HTTP stack control | `/AuthorizationHeader` (then call downstream yourself) |
| Centralize outbound HTTP auth, normalization, logging | `/DownstreamApi` |
| Call with app identity only | `/AuthorizationHeaderUnauthenticated` or `/DownstreamApiUnauthenticated` |
| Agent performing tasks (autonomous or delegated user) | `/AuthorizationHeader` or `/DownstreamApi` with `AgentIdentity` (+ optional user param) |

## 2. Agent & User Parameter Rules

The endpoints `/AuthorizationHeader`, `/AuthorizationHeaderUnauthenticated`, `/DownstreamApi`, and `/DownstreamApiUnauthenticated` accept **optional** agent parameters.

| Combination | Allowed | Outcome |
|-------------|---------|---------|
| `AgentIdentity` alone | Yes | Autonomous agent (app/client credentials) |
| `AgentIdentity` + `AgentUsername` | Yes | Delegated (agent acting for user identified by UPN) |
| `AgentIdentity` + `AgentUserId` | Yes | Delegated (agent acting for user identified by OID) |
| `AgentUsername` only | No | 400 (must include `AgentIdentity`) |
| `AgentUserId` only | No | 400 (must include `AgentIdentity`) |
| `AgentUsername` + `AgentUserId` (same request) | No | 400 (mutually exclusive) |

See [Agent Identities](agent-identities.md#agent-selection-flow) for flowchart.

## 3. Exhaustive Override Table  <a id="override-table"></a>

| Group | Query Parameter | Underlying Property / Concept | Type | Example | Notes |
|-------|-----------------|-------------------------------|------|---------|-------|
| Identity / Agent | `AgentIdentity` | Agent identity context | string | `AgentIdentity=BatchOrchestrator` | Enables agent context |
| Identity / Agent | `AgentUsername` | Agent user (UPN) | string | `AgentUsername=jane@contoso.com` | Requires `AgentIdentity` |
| Identity / Agent | `AgentUserId` | Agent user (OID) | GUID | `AgentUserId=11111111-2222-...` | Requires `AgentIdentity`; mutually exclusive with UPN |
| Scopes | `optionsOverride.Scopes` (repeatable) | `DownstreamApiOptions.Scopes` | string | `optionsOverride.Scopes=Mail.Read` | Each occurrence adds a scope |
| Token Mode | `optionsOverride.RequestAppToken` | `RequestAppToken` | bool | `...=true` | Forces app (client credentials) token |
| URL | `optionsOverride.BaseUrl` | `BaseUrl` | string | `...BaseUrl=https://graph.microsoft.com` | Single-call override |
| URL | `optionsOverride.RelativePath` | `RelativePath` | string | `...RelativePath=/v1.0/me/messages` | Overrides configured path |
| HTTP | `optionsOverride.HttpMethod` | `HttpMethod` | string | `...HttpMethod=GET` | Proxy endpoints only |
| HTTP | `optionsOverride.AcceptHeader` | `AcceptHeader` | string | `...AcceptHeader=application/json` | Proxy endpoints |
| HTTP | `optionsOverride.ContentType` | `ContentType` | string | `...ContentType=application/json` | Proxy endpoints |
| Acquire | `optionsOverride.AcquireTokenOptions.Tenant` | `AcquireTokenOptions.Tenant` | string | `...Tenant=organizations` | Cross-/multi-tenant override |
| Acquire | `optionsOverride.AcquireTokenOptions.ForceRefresh` | `ForceRefresh` | bool | `...ForceRefresh=true` | Cache bypass (avoid routine use) |
| Acquire | `optionsOverride.AcquireTokenOptions.Claims` | `Claims` | string (JSON) | `...Claims=%7B%22xms_cc%22%3A%5B%22cp1%22%5D%7D` | CAE / step-up challenges |
| Acquire | `optionsOverride.AcquireTokenOptions.CorrelationId` | `CorrelationId` | GUID | `...CorrelationId=1a2b...` | Logging & tracing |
| Acquire | `optionsOverride.AcquireTokenOptions.LongRunningWebApiSessionKey` | `LongRunningWebApiSessionKey` | string | `...LongRunningWebApiSessionKey=workflow-42` | Long-lived OBO |
| Managed Identity | `optionsOverride.AcquireTokenOptions.ManagedIdentity.UserAssignedClientId` | `ManagedIdentity.UserAssignedClientId` | string (clientId) | `...ManagedIdentity.UserAssignedClientId=xxxxxxxx-...` | User-assigned MI select |
| SHR | `optionsOverride.AcquireTokenOptions.PopPublicKey` | `PopPublicKey` | string (JWK) | `...PopPublicKey=<url-encoded-jwk>` | Signed HTTP Request token |
| Federated MI | `optionsOverride.AcquireTokenOptions.FmiPath` | `FmiPath` | string | `...FmiPath=/federation/idp` | Federated managed identity |
| Auth Scheme | `optionsOverride.AcquireTokenOptions.AuthenticationOptionsName` | `AuthenticationOptionsName` | string | `...AuthenticationOptionsName=OpenIdConnect` | Multi-scheme selection |

## 4. Response Schemas

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
  "type": "about:blank",
  "title": "Bad Request",
  "status": 400,
  "detail": "Explanation...",
  "instance": "/AuthorizationHeader/Graph"
}
```

## 5. Examples

**Autonomous header**
```
GET /AuthorizationHeader/Graph?AgentIdentity=Scheduler
```

**Delegated (OID) with long-running OBO**
```
GET /AuthorizationHeader/Graph?AgentIdentity=Scheduler&AgentUserId=<oid>&optionsOverride.AcquireTokenOptions.LongRunningWebApiSessionKey=job-17
```

**Proxy call overriding path + method**
```
POST /DownstreamApi/Graph?optionsOverride.HttpMethod=GET&optionsOverride.RelativePath=/v1.0/me/messages
```

**SHR token**
```
GET /AuthorizationHeader/Graph?AgentIdentity=Batch&optionsOverride.AcquireTokenOptions.PopPublicKey=<url-encoded-jwk>
```

**CAE retry with claims**
```
GET /AuthorizationHeader/Graph?optionsOverride.AcquireTokenOptions.Claims=<url-encoded-json>
```

## 6. Error Patterns

| HTTP | Condition | Notes |
|------|-----------|-------|
| 400 | Invalid combination (user param w/o agent or both user forms) | Parameter validation |
| 400 | Unknown `apiName` | Misconfiguration |
| 401 | Missing / invalid bearer on auth endpoints | Provide valid token |
| 200 (wrapper) + downstream non-2xx | Downstream error surfaced | Check `statusCode` & `content` |
| 400 | Unsupported override key | Typo or not exposed |
| 400 | Non-GUID `AgentUserId` | Must parse as GUID |

## 7. Cross-Links

- Scenarios: [Obtain Header](scenarios/obtain-authorization-header.md) • [Proxy Call](scenarios/call-downstream-api.md) • [Managed Identity](scenarios/managed-identity.md) • [Long-Running OBO](scenarios/long-running-obo.md) • [SHR](scenarios/signed-http-request.md)
- Agent Flow: [Agent Identities](agent-identities.md)
- Reference (auto-generated): [Endpoints Reference (Full)](reference/endpoints-reference.md)
- Performance: (proposed) `performance.md`
- Troubleshooting: [troubleshooting.md](troubleshooting.md)

---
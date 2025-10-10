# Microsoft Identity Web Sidecar - HTTP API Reference

## Overview

The Microsoft Identity Web Sidecar is an HTTP service that simplifies token acquisition and downstream API calls for applications using Microsoft Entra ID (formerly Azure AD). It acts as a lightweight proxy, handling authentication complexities while your application focuses on business logic.

**Key capabilities:**
- Validate incoming bearer tokens
- Acquire authorization headers for downstream APIs
- Proxy calls to downstream APIs with automatic token handling
- Support for both user (OBO) and application (client credentials) tokens
- Managed Identity support
- Advanced scenarios: PoP tokens, long-running OBO, claims challenges

---

## Configuration

The sidecar requires configuration for authentication and downstream APIs. Configuration is typically provided via `appsettings.json` or environment variables.

### Configuration Schema

The configuration follows the [microsoft-identity-web.json schema](https://github.com/AzureAD/microsoft-identity-web/blob/728223314709d99d13eea5e2a6a93bae2f5c0480/JsonSchemas/microsoft-identity-web.json).

### Minimal Configuration Example

```json
{
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com",
    "TenantId": "common",
    "ClientId": "your-client-id-guid"
  }
}
```

### Full Configuration Example

```json
{
  "Version": "1.0",
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com",
    "TenantId": "your-tenant-id-or-common",
    "ClientId": "your-client-id-guid",
    "ClientCredentials": [
      {
        "SourceType": "ClientSecret",
        "ClientSecret": "your-client-secret"
      }
    ],
    "ClientCapabilities": ["cp1"],
    "SendX5C": false,
    "AzureRegion": "westus2"
  },
  "DownstreamApis": {
    "GraphApi": {
      "BaseUrl": "https://graph.microsoft.com/v1.0",
      "Scopes": ["User.Read"]
    },
    "CustomApi": {
      "BaseUrl": "https://api.contoso.com",
      "Scopes": ["api://custom-api-client-id/.default"],
      "RelativePath": "/api/data",
      "HttpMethod": "GET"
    }
  }
}
```

### AzureAd Section

Required section for configuring the application's identity.

| Property | Type | Required | Default | Description |
|----------|------|----------|---------|-------------|
| `Instance` | string | Yes | `https://login.microsoftonline.com` | Cloud instance URL. Use `https://login.microsoftonline.us` for US Government, `https://login.chinacloudapi.cn` for China, etc. |
| `TenantId` | string | Yes | - | Tenant ID (GUID) or `common`, `organizations`, `consumers` |
| `ClientId` | string | Yes | - | Application (client) ID from your app registration |
| `Authority` | string | No | - | Full authority URL. **Exclusive with Instance + TenantId.** Example: `https://login.microsoftonline.com/contoso.onmicrosoft.com` |
| `ClientCredentials` | array | No* | - | Array of credential configurations. *Required for unauthenticated endpoints. See [Client Credentials](#client-credentials) |
| `ClientCapabilities` | array | No | - | Array of client capability strings (e.g., `["cp1"]` for claims challenge support) |
| `SendX5C` | boolean | No | `false` | Send X.509 certificate chain (X5C) in token requests when using certificate credentials |
| `AzureRegion` | string | No | - | Azure region for regional endpoints (e.g., `westus2`) |
| `ShowPII` | boolean | No | `false` | Show Personally Identifiable Information in logs (use only for debugging) |
| `ExtraQueryParameters` | object | No | - | Additional query parameters to include in authorization requests |
| `Audiences` | array | No | - | Array of accepted token audiences for validation |
| `TokenDecryptionCredentials` | array | No | - | Credentials for decrypting encrypted tokens |

#### Client Credentials

The `ClientCredentials` array supports multiple credential types. See the [Credentials.json schema](https://github.com/AzureAD/microsoft-identity-web/blob/728223314709d99d13eea5e2a6a93bae2f5c0480/JsonSchemas/Credentials.json) for full details.

**Client Secret Example:**
```json
"ClientCredentials": [
  {
    "SourceType": "ClientSecret",
    "ClientSecret": "your-secret-value"
  }
]
```

**Certificate (Key Vault) Example:**
```json
"ClientCredentials": [
  {
    "SourceType": "KeyVault",
    "KeyVaultUrl": "https://your-vault.vault.azure.net",
    "KeyVaultCertificateName": "your-cert-name"
  }
]
```

**Certificate (File) Example:**
```json
"ClientCredentials": [
  {
    "SourceType": "Path",
    "CertificateDiskPath": "/path/to/cert.pfx",
    "CertificatePassword": "cert-password"
  }
]
```

**Managed Identity Example:**
```json
"ClientCredentials": [
  {
    "SourceType": "SignedAssertionFromManagedIdentity",
    "ManagedIdentityClientId": "user-assigned-identity-client-id"
  }
]
```

### DownstreamApis Section

Configure downstream APIs that can be referenced by the `{apiName}` path parameter.

Each key in the `DownstreamApis` object becomes an `{apiName}` you can reference in API calls.

**Structure:**
```json
"DownstreamApis": {
  "ApiName": {
    "BaseUrl": "string",
    "Scopes": ["string"],
    "RelativePath": "string",
    "HttpMethod": "string",
    "RequestAppToken": boolean,
    "AcquireTokenOptions": {
      // Token acquisition options
    }
  }
}
```

| Property | Type | Required | Description |
|----------|------|----------|-------------|
| `BaseUrl` | string | Yes | Base URL of the downstream API |
| `Scopes` | array | No | Array of OAuth scopes to request |
| `RelativePath` | string | No | Default relative path appended to BaseUrl |
| `HttpMethod` | string | No | Default HTTP method (GET, POST, etc.) |
| `RequestAppToken` | boolean | No | If `true`, use app token instead of user token |
| `AcceptHeader` | string | No | Default Accept header value |
| `ContentType` | string | No | Default Content-Type for requests |
| `AcquireTokenOptions` | object | No | Token acquisition options (Tenant, ForceRefresh, Claims, etc.) |

**Example - Microsoft Graph:**
```json
"DownstreamApis": {
  "GraphApi": {
    "BaseUrl": "https://graph.microsoft.com/v1.0",
    "Scopes": ["User.Read", "Mail.Read"]
  }
}
```

**Example - Custom API with app token:**
```json
"DownstreamApis": {
  "BackendService": {
    "BaseUrl": "https://backend.contoso.com",
    "Scopes": ["api://backend-api/.default"],
    "RequestAppToken": true,
    "HttpMethod": "POST",
    "ContentType": "application/json"
  }
}
```

**Example - API with tenant override:**
```json
"DownstreamApis": {
  "MultiTenantApi": {
    "BaseUrl": "https://api.contoso.com",
    "Scopes": ["api://multi-tenant/.default"],
    "AcquireTokenOptions": {
      "Tenant": "specific-tenant-id"
    }
  }
}
```

### Environment Variables

Configuration can also be provided via environment variables using double underscore (`__`) notation:

```bash
AzureAd__Instance=https://login.microsoftonline.com
AzureAd__TenantId=common
AzureAd__ClientId=your-client-id
AzureAd__ClientCredentials__0__SourceType=ClientSecret
AzureAd__ClientCredentials__0__ClientSecret=your-secret
DownstreamApis__GraphApi__BaseUrl=https://graph.microsoft.com/v1.0
DownstreamApis__GraphApi__Scopes__0=User.Read
```

---

## Base URL

```
http://localhost:<port>
```

> **Note:** Configure the port and base URL according to your deployment environment.

---

## Endpoints

### 1. Validate Authorization Header

**Endpoint:** `GET /Validate`

**Description:** Validates the bearer token in the incoming request's `Authorization` header and returns the protocol, token, and extracted claims.

**Use cases:**
- Verify token validity before processing requests
- Extract user claims for authorization decisions
- Debug authentication issues

#### Request

**Headers:**
```
Authorization: Bearer <token>
```

#### cURL Example

```bash
curl -X GET "http://localhost:5000/Validate" \
  -H "Authorization: Bearer eyJ0eXAiOiJKV1QiLCJhbGc..."
```

#### Response

**Success (200 OK):**
```json
{
  "protocol": "Bearer",
  "token": "eyJ0eXAiOiJKV1QiLCJhbGc...",
  "claims": {
    "aud": "api://your-api-client-id",
    "iss": "https://login.microsoftonline.com/{tenant}/v2.0",
    "oid": "00000000-0000-0000-0000-000000000000",
    "preferred_username": "user@contoso.com",
    "scp": "access_as_user",
    "sub": "...",
    "tid": "...",
    "uti": "...",
    "ver": "2.0"
  }
}
```

**Error Responses:**
- `400 Bad Request` - Missing or malformed authorization header
- `401 Unauthorized` - Invalid or expired token

---

### 2. Get Authorization Header (Authenticated)

**Endpoint:** `GET /AuthorizationHeader/{apiName}`

**Description:** Acquires an authorization header for calling a configured downstream API. Uses the authenticated user's identity (On-Behalf-Of flow) from the incoming request.

**Use cases:**
- Get a token to call Microsoft Graph on behalf of a user
- Chain API calls while maintaining user context
- Implement delegated permission scenarios

#### Path Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `apiName` | string | Yes | Name of the downstream API as configured in the sidecar settings (e.g., `GraphApi` from the `DownstreamApis` section) |

#### Query Parameters

##### Agent Identity Parameters

| Parameter | Type | Description |
|-----------|------|-------------|
| `AgentIdentity` | string | The identity of the agent making the request |
| `AgentUsername` | string | The username (UPN) of the user agent identity |
| `AgentUserId` | string | The Object ID (OID) of the agent |

##### Options Override Parameters

All override parameters use the `optionsOverride.` prefix:

| Parameter | Type | Description |
|-----------|------|-------------|
| `optionsOverride.Scopes` | string | **Repeatable.** Each occurrence adds one scope. Example: `optionsOverride.Scopes=User.Read&optionsOverride.Scopes=Mail.Read` |
| `optionsOverride.RequestAppToken` | boolean | `true` = acquire an app (client credentials) token instead of user token |
| `optionsOverride.BaseUrl` | string | Override downstream API base URL |
| `optionsOverride.RelativePath` | string | Override relative path appended to BaseUrl |
| `optionsOverride.HttpMethod` | string | Override HTTP method (GET, POST, PATCH, etc.) |
| `optionsOverride.AcceptHeader` | string | Sets Accept header (e.g., `application/json`) |
| `optionsOverride.ContentType` | string | Sets Content-Type for serialized body |

##### Token Acquisition Options

| Parameter | Type | Description |
|-----------|------|-------------|
| `optionsOverride.AcquireTokenOptions.Tenant` | string | Override tenant (GUID or 'common') |
| `optionsOverride.AcquireTokenOptions.ForceRefresh` | boolean | `true` = bypass token cache |
| `optionsOverride.AcquireTokenOptions.Claims` | string | JSON claims challenge or extra claims |
| `optionsOverride.AcquireTokenOptions.CorrelationId` | string | GUID correlation ID for token acquisition |
| `optionsOverride.AcquireTokenOptions.LongRunningWebApiSessionKey` | string | Session key for long-running OBO flows |
| `optionsOverride.AcquireTokenOptions.FmiPath` | string | Federated Managed Identity path |
| `optionsOverride.AcquireTokenOptions.PopPublicKey` | string | Public key or JWK for PoP/AT-PoP requests |
| `optionsOverride.AcquireTokenOptions.ManagedIdentity.UserAssignedClientId` | string | Managed Identity client ID (user-assigned) |

#### Request Headers

```
Authorization: Bearer <user_token>
```

#### cURL Examples

**Basic usage (using configured API):**
```bash
curl -X GET "http://localhost:5000/AuthorizationHeader/GraphApi" \
  -H "Authorization: Bearer eyJ0eXAiOiJKV1QiLCJhbGc..."
```

**With scope override:**
```bash
curl -X GET "http://localhost:5000/AuthorizationHeader/GraphApi?optionsOverride.Scopes=User.Read&optionsOverride.Scopes=Mail.Read" \
  -H "Authorization: Bearer eyJ0eXAiOiJKV1QiLCJhbGc..."
```

**With multiple overrides:**
```bash
curl -X GET "http://localhost:5000/AuthorizationHeader/CustomApi?optionsOverride.Tenant=common&optionsOverride.AcquireTokenOptions.ForceRefresh=true" \
  -H "Authorization: Bearer eyJ0eXAiOiJKV1QiLCJhbGc..."
```

**With agent identity:**
```bash
curl -X GET "http://localhost:5000/AuthorizationHeader/GraphApi?AgentUsername=admin@contoso.com&AgentUserId=12345678-1234-1234-1234-123456789012" \
  -H "Authorization: Bearer eyJ0eXAiOiJKV1QiLCJhbGc..."
```

#### Response

**Success (200 OK):**
```json
{
  "authorizationHeader": "Bearer eyJ0eXAiOiJKV1QiLCJhbGc..."
}
```

**Error Responses:**
- `400 Bad Request` - Invalid parameters or API name not configured
- `401 Unauthorized` - Missing or invalid authorization header, or token acquisition failed

---

### 3. Get Authorization Header (Unauthenticated)

**Endpoint:** `GET /AuthorizationHeaderUnauthenticated/{apiName}`

**Description:** Acquires an authorization header for calling a configured downstream API using the sidecar's configured client credentials (application token). No user context required.

**Use cases:**
- Background jobs and daemon services
- Application-level API access
- Server-to-server scenarios without user interaction

#### Path Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `apiName` | string | Yes | Name of the downstream API as configured in the sidecar settings |

#### Query Parameters

Same as [Get Authorization Header (Authenticated)](#query-parameters), including:
- Agent Identity Parameters
- Options Override Parameters  
- Token Acquisition Options

> **Note:** The `optionsOverride.RequestAppToken` parameter is implicit (`true`) for this endpoint.

#### cURL Examples

**Basic usage:**
```bash
curl -X GET "http://localhost:5000/AuthorizationHeaderUnauthenticated/GraphApi"
```

**With scope override:**
```bash
curl -X GET "http://localhost:5000/AuthorizationHeaderUnauthenticated/GraphApi?optionsOverride.Scopes=https://graph.microsoft.com/.default"
```

**With managed identity (user-assigned):**
```bash
curl -X GET "http://localhost:5000/AuthorizationHeaderUnauthenticated/AzureResource?optionsOverride.AcquireTokenOptions.ManagedIdentity.UserAssignedClientId=12345678-1234-1234-1234-123456789012"
```

**Force token refresh:**
```bash
curl -X GET "http://localhost:5000/AuthorizationHeaderUnauthenticated/CustomApi?optionsOverride.AcquireTokenOptions.ForceRefresh=true"
```

#### Response

**Success (200 OK):**
```json
{
  "authorizationHeader": "Bearer eyJ0eXAiOiJKV1QiLCJhbGc..."
}
```

**Error Responses:**
- `400 Bad Request` - Invalid parameters or API name not configured
- `401 Unauthorized` - Client credential authentication failed

---

### 4. Call Downstream API (Authenticated)

**Endpoint:** `POST /DownstreamApi/{apiName}`

**Description:** Proxies a call to a configured downstream API using the authenticated user's identity. Automatically acquires the necessary token and forwards the request.

**Use cases:**
- Simplify downstream API calls by offloading token acquisition
- Call Microsoft Graph or custom APIs on behalf of users
- Maintain consistent error handling and logging

#### Path Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `apiName` | string | Yes | Name of the downstream API as configured in the sidecar settings |

#### Query Parameters

Same as [Get Authorization Header (Authenticated)](#query-parameters), including:
- Agent Identity Parameters
- Options Override Parameters
- Token Acquisition Options

#### Request Headers

```
Authorization: Bearer <user_token>
Content-Type: application/json
```

#### Request Body

The request body (optional) is forwarded to the downstream API as-is.

```json
{
  "key": "value",
  "data": "to send to downstream API"
}
```

#### cURL Examples

**Basic GET call:**
```bash
curl -X POST "http://localhost:5000/DownstreamApi/GraphApi?optionsOverride.HttpMethod=GET&optionsOverride.RelativePath=/me" \
  -H "Authorization: Bearer eyJ0eXAiOiJKV1QiLCJhbGc..."
```

**POST with body:**
```bash
curl -X POST "http://localhost:5000/DownstreamApi/CustomApi?optionsOverride.HttpMethod=POST&optionsOverride.RelativePath=/users" \
  -H "Authorization: Bearer eyJ0eXAiOiJKV1QiLCJhbGc..." \
  -H "Content-Type: application/json" \
  -d '{"name": "John Doe", "email": "john@contoso.com"}'
```

**Override base URL and path:**
```bash
curl -X POST "http://localhost:5000/DownstreamApi/CustomApi?optionsOverride.BaseUrl=https://api.example.com&optionsOverride.RelativePath=/v2/data&optionsOverride.HttpMethod=GET" \
  -H "Authorization: Bearer eyJ0eXAiOiJKV1QiLCJhbGc..."
```

**With custom headers:**
```bash
curl -X POST "http://localhost:5000/DownstreamApi/GraphApi?optionsOverride.HttpMethod=GET&optionsOverride.RelativePath=/me&optionsOverride.AcceptHeader=application/json" \
  -H "Authorization: Bearer eyJ0eXAiOiJKV1QiLCJhbGc..."
```

**PATCH request:**
```bash
curl -X POST "http://localhost:5000/DownstreamApi/GraphApi?optionsOverride.HttpMethod=PATCH&optionsOverride.RelativePath=/me" \
  -H "Authorization: Bearer eyJ0eXAiOiJKV1QiLCJhbGc..." \
  -H "Content-Type: application/json" \
  -d '{"jobTitle": "Senior Engineer"}'
```

#### Response

**Success (200 OK):**
```json
{
  "statusCode": 200,
  "headers": {
    "Content-Type": ["application/json; charset=utf-8"],
    "Date": ["Fri, 10 Oct 2025 02:56:31 GMT"],
    "X-Request-Id": ["12345678-1234-1234-1234-123456789012"]
  },
  "content": "{\"displayName\":\"John Doe\",\"mail\":\"john@contoso.com\",\"id\":\"...\"}"
}
```

> **Note:** The `content` field contains the raw response body from the downstream API as a string. Parse it according to the `Content-Type` header.

**Error Responses:**
- `400 Bad Request` - Invalid parameters or API name not configured
- `401 Unauthorized` - Missing/invalid authorization header or token acquisition failed
- Other status codes reflect the downstream API's response

---

### 5. Call Downstream API (Unauthenticated)

**Endpoint:** `POST /DownstreamApiUnauthenticated/{apiName}`

**Description:** Proxies a call to a configured downstream API using the sidecar's configured client credentials (application token). No user context required.

**Use cases:**
- Background jobs calling APIs
- Scheduled tasks accessing resources
- Service-to-service communication

#### Path Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `apiName` | string | Yes | Name of the downstream API as configured in the sidecar settings |

#### Query Parameters

Same as [Get Authorization Header (Authenticated)](#query-parameters), including:
- Agent Identity Parameters
- Options Override Parameters
- Token Acquisition Options

#### Request Body

The request body (optional) is forwarded to the downstream API as-is.

```json
{
  "key": "value",
  "data": "to send to downstream API"
}
```

#### cURL Examples

**Basic GET call:**
```bash
curl -X POST "http://localhost:5000/DownstreamApiUnauthenticated/GraphApi?optionsOverride.HttpMethod=GET&optionsOverride.RelativePath=/users"
```

**POST with application permissions:**
```bash
curl -X POST "http://localhost:5000/DownstreamApiUnauthenticated/GraphApi?optionsOverride.HttpMethod=POST&optionsOverride.RelativePath=/users&optionsOverride.Scopes=https://graph.microsoft.com/.default" \
  -H "Content-Type: application/json" \
  -d '{"accountEnabled": true, "displayName": "Service Account", "mailNickname": "serviceacct", "userPrincipalName": "serviceacct@contoso.com", "passwordProfile": {"password": "TempPass123!"}}'
```

**Using managed identity:**
```bash
curl -X POST "http://localhost:5000/DownstreamApiUnauthenticated/StorageApi?optionsOverride.HttpMethod=GET&optionsOverride.AcquireTokenOptions.ManagedIdentity.UserAssignedClientId=12345678-1234-1234-1234-123456789012"
```

**Background job example:**
```bash
curl -X POST "http://localhost:5000/DownstreamApiUnauthenticated/CustomBackendApi?optionsOverride.HttpMethod=POST&optionsOverride.RelativePath=/batch/process" \
  -H "Content-Type: application/json" \
  -d '{"jobId": "job-12345", "action": "process"}'
```

#### Response

**Success (200 OK):**
```json
{
  "statusCode": 200,
  "headers": {
    "Content-Type": ["application/json; charset=utf-8"],
    "Date": ["Fri, 10 Oct 2025 02:56:31 GMT"]
  },
  "content": "{\"value\": [{\"id\": \"...\", \"displayName\": \"User 1\"}, ...]}"
}
```

**Error Responses:**
- `400 Bad Request` - Invalid parameters or API name not configured
- `401 Unauthorized` - Client credential authentication failed
- Other status codes reflect the downstream API's response

---

## Response Schemas

### ValidateAuthorizationHeaderResult

```json
{
  "protocol": "Bearer",
  "token": "string",
  "claims": {
    // JSON object containing token claims
  }
}
```

| Field | Type | Description |
|-------|------|-------------|
| `protocol` | string | Authentication protocol (typically "Bearer") |
| `token` | string | The raw token value |
| `claims` | object | Decoded JWT claims as a JSON object |

---

### AuthorizationHeaderResult

```json
{
  "authorizationHeader": "Bearer eyJ0eXAiOiJKV1QiLCJhbGc..."
}
```

| Field | Type | Description |
|-------|------|-------------|
| `authorizationHeader` | string | Complete authorization header value to use in downstream requests |

---

### DownstreamApiResult

```json
{
  "statusCode": 200,
  "headers": {
    "Header-Name": ["value1", "value2"]
  },
  "content": "string or null"
}
```

| Field | Type | Description |
|-------|------|-------------|
| `statusCode` | integer | HTTP status code from the downstream API |
| `headers` | object | Dictionary of response headers (values are arrays of strings) |
| `content` | string \| null | Raw response body from downstream API (may be null) |

---

### ProblemDetails (Error Response)

```json
{
  "type": "string or null",
  "title": "string or null",
  "status": 400,
  "detail": "string or null",
  "instance": "string or null"
}
```

| Field | Type | Description |
|-------|------|-------------|
| `type` | string | URI reference identifying the problem type |
| `title` | string | Short, human-readable summary |
| `status` | integer | HTTP status code |
| `detail` | string | Human-readable explanation specific to this occurrence |
| `instance` | string | URI reference identifying this specific occurrence |

---

## Common Patterns

### Multiple Scopes

To specify multiple scopes, repeat the `optionsOverride.Scopes` parameter:

```bash
curl -X GET "http://localhost:5000/AuthorizationHeader/GraphApi?optionsOverride.Scopes=User.Read&optionsOverride.Scopes=Mail.Read&optionsOverride.Scopes=Calendars.Read" \
  -H "Authorization: Bearer eyJ0eXAiOiJKV1QiLCJhbGc..."
```

### Claims Challenge

When handling conditional access or claims challenges:

```bash
curl -X GET "http://localhost:5000/AuthorizationHeader/GraphApi?optionsOverride.AcquireTokenOptions.Claims=%7B%22access_token%22%3A%7B%22acrs%22%3A%7B%22essential%22%3Atrue%2C%22value%22%3A%22c1%22%7D%7D%7D" \
  -H "Authorization: Bearer eyJ0eXAiOiJKV1QiLCJhbGc..."
```

> **Note:** URL-encode the JSON claims parameter.

### Long-Running OBO

For long-running web API sessions:

```bash
curl -X GET "http://localhost:5000/AuthorizationHeader/BackendApi?optionsOverride.AcquireTokenOptions.LongRunningWebApiSessionKey=session-abc-123" \
  -H "Authorization: Bearer eyJ0eXAiOiJKV1QiLCJhbGc..."
```

### PoP Tokens

For Proof-of-Possession tokens:

```bash
curl -X GET "http://localhost:5000/AuthorizationHeader/SecureApi?optionsOverride.AcquireTokenOptions.PopPublicKey=%7B%22kty%22%3A%22RSA%22%2C...%7D" \
  -H "Authorization: Bearer eyJ0eXAiOiJKV1QiLCJhbGc..."
```

---

## HTTP Status Codes

| Code | Meaning | Common Causes |
|------|---------|---------------|
| 200 | Success | Request completed successfully |
| 400 | Bad Request | Invalid parameters, malformed request, unknown API name |
| 401 | Unauthorized | Missing/invalid token, token acquisition failed, insufficient permissions |

---

## Best Practices

1. **Configure APIs in the sidecar:** Pre-configure downstream APIs in the sidecar's configuration file rather than overriding everything via query parameters.

2. **Use appropriate authentication:** Choose authenticated endpoints for user-delegated scenarios, unauthenticated for app-only scenarios.

3. **Leverage token caching:** The sidecar caches tokens automatically. Use `ForceRefresh=true` only when necessary.

4. **Handle errors gracefully:** Parse `ProblemDetails` responses to understand failures.

5. **Secure the sidecar:** Deploy the sidecar in a trusted network environment. It should not be directly exposed to the internet.

6. **Monitor correlation IDs:** Use `AcquireTokenOptions.CorrelationId` for tracking requests across systems.

7. **Agent identity tracking:** Use agent parameters (`AgentIdentity`, `AgentUsername`, `AgentUserId`) for auditing and logging purposes.

8. **Use environment-specific configuration:** Leverage environment variables or different `appsettings.{Environment}.json` files for different deployment environments.

---
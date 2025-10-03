# Microsoft.Identity.Web.Sidecar

## Overview

`Microsoft.Identity.Web.Sidecar` hosts a minimal ASP.NET Core Web API that
enables Microsoft Entra token acquisition and downstream API calls, and token validation.

### Key capabilities

- Validates incoming tokens and surfaces their claims.
- Decrypts tokens if applicable.
- Acquires User OBO or Application tokens for configured downstream APIs.

## Configuration

Settings are supplied via `appsettings.json`, environment variables, or any standard [ASP.NET Core configuration source](https://learn.microsoft.com/aspnet/core/fundamentals/configuration/).

```jsonc
{
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "TenantId": "<tenant-guid>",
    "ClientId": "<sidecar-client-id>",
    "ClientCredentials": [
      { "SourceType": "...", }
    ],
    "AllowWebApiToBeAuthorizedByACL": true
  },
  "DownstreamApis": {
    "graph": {
      "BaseUrl": "https://graph.microsoft.com/v1.0/",
      "RelativePath": "me",
      "Scopes": [ "User.Read" ]
    }
  },
  "TokenDecryptionCredentialsDescription" : [
    // If applicable
    { "SourceType": "...", }
  ]
}
```

*Important sections*

- **AzureAd**: Standard Microsoft.Identity.Web web API registration; client credentials are optional if only delegated flows are required.
- **DownstreamApis**: Named profiles for endpoints resolved via `{apiName}`.
- **Data protection**: In production the app persists keys to `DATA_PROTECTION_KEYS_PATH` (default `/app/keys`) and optionally protects them with a certificate referenced via `DATA_PROTECTION_CERT_PATH` and `DATA_PROTECTION_CERT_PASSWORD`.

## Running the sidecar

### Prerequisites

- .NET SDK 9.0 or later.
- A Microsoft Entra application registration for the sidecar and any downstream APIs.

### Local execution

```pwsh
dotnet restore
dotnet run -f net9.0
```

### Containers

- [Dockerfile](./Dockerfile) is used for building images within Visual Studio
- [DockerFile.NanoServer](./DockerFile.NanoServer) is used for building a nanoserver image from previously build binaries
- [DockerFile.AzureLinux](./Dockerfile.AzureLinux) is used for building an azure linux 3.0 image from previously build binaries

## HTTP surface

| Endpoint                                        | Method | Auth     | Description                                                                                      |
| ----------------------------------------------- | ------ | -------- | ------------------------------------------------------------------------------------------------ |
| `/Validate`                                     | GET    | Required | Returns the raw bearer token and claims. Enforces `AzureAd:Scopes` when configured.              |
| `/AuthorizationHeader/{apiName}`                | GET    | Required | Returns an `Authorization` header for the named downstream API using the caller’s identity.      |
| `/AuthorizationHeaderUnauthenticated/{apiName}` | GET    | Optional | Uses the sidecar’s application identity to obtain a token.                                       |
| `/DownstreamApi/{apiName}`                      | POST   | Required | Invokes the downstream API profile with the caller’s identity, forwarding body and content-type. |
| `/DownstreamApiUnauthenticated/{apiName}`       | POST   | Optional | Invokes the downstream API using the sidecar’s application identity.                             |
| `/healthz`                                      | GET    | NA       | Combined liveness/readiness check.                                                               |
| `/openapi/v1.json`                              | GET    | NA       | When ASPNETCORE_ENVIRONMENT=Development                                                          |

Complete documentation is provided [here](./OpenAPI/Microsoft.Identity.Web.Sidecar.json)

### Options overrides

All token-acquisition endpoints accept dotted query parameters prefixed with `optionsOverride.`; they merge into a `DownstreamApis` profile through [`BindableDownstreamApiOptions`](Models/BindableDownstreamApiOptions.cs).

Examples:
- `?optionsOverride.Scopes=User.Read&optionsOverride.Scopes=Mail.Read`
- `?optionsOverride.RequestAppToken=true`
- `?optionsOverride.AcquireTokenOptions.Tenant=<tenant-guid>`
- `?optionsOverride.RelativePath=me/messages`

Agent impersonation hints:
- `AgentIdentity=<client-id>`
- `AgentUsername=upn@contoso.com`
- `AgentUserId=<oid>`

### Response contract

- `/AuthorizationHeader*` returns `{ "authorizationHeader": "Bearer ey..." }`.
- `/DownstreamApi*` returns `{ "statusCode": 200, "headers": { ... }, "content": "..." }`.
- `/Validate` returns `{ "protocol": "Bearer", "token": "ey...", "claims": { ... } }`.

## Security considerations

- This API is only for usage as a sidecar. This API should not be publicly callable as it
  allows the caller to acquire tokens on behalf of the applications identity.

## Runtime composition

| Concern                        | Implementation                                                                                                                                                                                                     |
| ------------------------------ | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| Authentication & authorization | [`Program`](Program.cs) wires `AddMicrosoftIdentityWebApi`, optional scope enforcement, and agent identity overrides.                                                                                              |
| Endpoints                      | [`ValidateRequestEndpoints`](Endpoints/ValidateRequestEndpoints.cs), [`AuthorizationHeaderEndpoint`](Endpoints/AuthorizationHeaderEndpoint.cs), and [`DownstreamApiEndpoint`](Endpoints/DownstreamApiEndpoint.cs). |
| Downstream API                 | [`BindableDownstreamApiOptions`](Models/BindableDownstreamApiOptions.cs) merges per-request overrides into per call `DownstreamApis` configuration.                                                                |
| Agent Identities               | [`AgentOverrides`](AgentOverrides.cs) binds agent identity, userPrincipalName, or user object ID when present.                                                                                                     |


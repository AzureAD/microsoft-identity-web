# Configuration

Configuration follows the schema in [microsoft-identity-web.json](../../JsonSchemas/microsoft-identity-web.json). No dedicated `Agents` section is required—agent identity is chosen per request via query parameters.

## Sections

| Section | Purpose |
|---------|---------|
| `AzureAd` | Tenant + application (primary) credentials |
| `DownstreamApis` | Logical API configurations keyed by name |

Schema permalink: [microsoft-identity-web.json](https://github.com/AzureAD/microsoft-identity-web/blob/9c3eb358bb5781f93c268b6ae125ad5ed0d531a9/JsonSchemas/microsoft-identity-web.json)

## Example (Excerpt)

```json
{
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com",
    "TenantId": "<tenant-guid>",
    "ClientId": "<client-id>",
    "ClientCredentials": [
      {
        "SourceType": "X509Certificate",
        "Certificate": {
          "SourceType": "StoreWithThumbprint",
          "StoreWithThumbprint": {
            "StoreLocation": "CurrentUser",
            "StoreName": "My",
            "Thumbprint": "<thumbprint>"
          }
        }
      }
    ],
    "SendX5C": false
  },
  "DownstreamApis": {
    "Graph": {
      "Scopes": [ "https://graph.microsoft.com/User.Read" ],
      "BaseUrl": "https://graph.microsoft.com",
      "RelativePath": "/v1.0/me",
      "HttpMethod": "GET"
    }
  }
}
```

Reference file: [appsettings.json](https://github.com/AzureAD/microsoft-identity-web/blob/df7376df7d8773b99f054c81c62bf24d697c3ae1/src/Microsoft.Identity.Web.Sidecar/appsettings.json)

## Environment Variable Mapping

| JSON | Env Var |
|------|---------|
| `AzureAd:TenantId` | `AzureAd__TenantId` |
| `AzureAd:ClientId` | `AzureAd__ClientId` |
| `DownstreamApis:Graph:Scopes:0` | `DownstreamApis__Graph__Scopes__0` |
| `DownstreamApis:Graph:BaseUrl` | `DownstreamApis__Graph__BaseUrl` |

Dotnet configuration binder uses `__` as a section separator.

## Credentials Priority

1. X.509 certificate
2. Managed / workload identity (credential-less)
3. Client secret (avoid for production if possible)

## Logging

Standard .NET logging pipeline. See [.NET logging docs](https://learn.microsoft.com/dotnet/core/extensions/logging). Avoid enabling `ShowPII` outside controlled debugging sessions.

## Runtime Overrides

Per-request query parameters `optionsOverride.*` adjust scopes, tenant, method, base path, force refresh, claims challenges, managed identity user-assigned client ID, long-running OBO session key, SHR public key, etc. See [endpoints.md](endpoints.md).

## Signed HTTP Request (SHR)

Provide `optionsOverride.AcquireTokenOptions.PopPublicKey=<url-encoded-jwk>` to request a Signed HTTP Request token. (We intentionally avoid the internal AT-POP terminology.)

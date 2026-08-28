# Microsoft.Identity.Web.Sidecar

## Overview

`Microsoft.Identity.Web.Sidecar` hosts a minimal ASP.NET Core Web API that
enables Microsoft Entra token acquisition and downstream API calls, and token validation including for agents

### Key capabilities

- Validates incoming tokens and surfaces their claims.
- Validates inbound app-only Proof-of-Possession (PoP) tokens (Signed HTTP Request) in addition to bearer tokens.
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

`AllowWebApiToBeAuthorizedByACL` will be set to true by the application. No action is required from the user to configure this.

`AllowedHosts` is not used by the sidecar. It will always be `localhost` outside of development environments.

*Important sections*

- **AzureAd**: Standard Microsoft.Identity.Web web API registration; client credentials are optional if only delegated flows are required.
- **DownstreamApis**: Named profiles for endpoints resolved via `{apiName}`.

### Outbound redirects

`Sidecar:AllowOutboundRedirects` controls whether the sidecar's outbound `HttpClient`
follows HTTP redirect (3xx) responses returned by a downstream API. It defaults to
`false`, so a redirect response is returned to the caller as-is rather than followed.
Set it to `true` to opt in to following redirects.

> **Note:** Outbound redirects are not followed by default (`AllowOutboundRedirects`
> defaults to `false`). To follow redirects, set it to `true`:
>
> ```jsonc
> {
>   "Sidecar": {
>     "AllowOutboundRedirects": true
>   }
> }
> ```

### Inbound Proof-of-Possession (PoP) validation

The `/Validate` endpoint also accepts a Signed HTTP Request (SHR) Proof-of-Possession credential using
the `PoP` authorization scheme, in addition to `Bearer`. PoP validation is scoped to **app-only
(client-credentials) tokens** and validates the embedded access token through the same `AzureAd`
configuration (issuer, audience, signing keys) used for bearer validation. Bearer behavior is unchanged.

Because the sidecar is invoked by its co-located application, the caller must supply the original
request line the SHR was signed over via two headers:

| Header            | Description                                     |
| ----------------- | ----------------------------------------------- |
| `original-method` | HTTP method of the signed request (e.g. `GET`). |
| `original-uri`    | Absolute URI of the signed request.             |

A validated PoP request returns `{ "protocol": "PoP", "token": "...", "claims": { ... } }`. A failed
PoP credential returns `401` with a `WWW-Authenticate: PoP error="invalid_token"` challenge.

Validation binds the `m` (method), `u` (host), `p` (path), and `ts` (timestamp) claims by default. The
timestamp provides freshness within a five-minute window; server nonce and replay caching are not
implemented. The parameters are operator-tunable under `Sidecar:PopValidation`:

```jsonc
{
  "Sidecar": {
    "PopValidation": {
      "ValidateTs": true,
      "ValidateM": true,
      "ValidateU": true,
      "ValidateP": true,
      "ValidateQ": false,
      "ValidateH": false,
      "SignedHttpRequestLifetime": "00:05:00"
    }
  }
}
```

See [`PopValidationOptions`](Configuration/PopValidationOptions.cs) for the full set of flags. When the
section is absent, secure defaults apply.

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

The sidecar is designed to run in containerized environments. Choose the appropriate Dockerfile for your target platform:

- [Dockerfile](./Dockerfile) is used for building images within Visual Studio
- [DockerFile.NanoServer](./DockerFile.NanoServer) is used for building a nanoserver image from previously build binaries
- [DockerFile.AzureLinux](./Dockerfile.AzureLinux) is used for building an azure linux 3.0 image from previously build binaries

**Configuring Client Credentials for Containers:**

When deploying the sidecar in containerized environments (Kubernetes, AKS, Docker) with **Azure AD Workload Identity**, configure client credentials using environment variables:

```yaml
# Example Kubernetes deployment configuration
env:
  - name: AzureAd__Instance
    value: "https://login.microsoftonline.com/"
  - name: AzureAd__TenantId
    value: "<tenant-guid>"
  - name: AzureAd__ClientId
    value: "<sidecar-client-id>"
  - name: AzureAd__ClientCredentials__0__SourceType
    value: "SignedAssertionFilePath"
  - name: AzureAd__ClientCredentials__0__SignedAssertionFilePath
    value: "/var/run/secrets/azure/tokens/azure-identity-token"
```

For **classic managed identity scenarios** (VMs, App Services), use:

```yaml
env:
  - name: AzureAd__ClientCredentials__0__SourceType
    value: "SignedAssertionFromManagedIdentity"
  - name: AzureAd__ClientCredentials__0__ManagedIdentityClientId
    value: "<managed-identity-client-id>"  # Omit for system-assigned
```

For all credential configuration options, see the [CredentialDescription documentation](https://aka.ms/ms-id-web/credential-description).

## HTTP surface

| Endpoint                                        | Method | Auth     | Description                                                                                      |
| ----------------------------------------------- | ------ | -------- | ------------------------------------------------------------------------------------------------ |
| `/Validate`                                     | GET    | Required | Returns the token and its claims. Accepts a `Bearer` or app-only `PoP` credential. Enforces `AzureAd:Scopes` when configured. |
| `/AuthorizationHeader/{apiName}`                | GET    | Required | Returns an `Authorization` header for the named downstream API using the caller’s identity.      |
| `/AuthorizationHeaderUnauthenticated/{apiName}` | GET    | Optional | Uses the sidecar’s application identity to obtain a token.                                       |
| `/DownstreamApi/{apiName}`                      | POST   | Required | Invokes the downstream API profile with the caller’s identity, forwarding body and content-type. |
| `/DownstreamApiUnauthenticated/{apiName}`       | POST   | Optional | Invokes the downstream API using the sidecar’s application identity.                             |
| `/healthz`                                      | GET    | NA       | Combined liveness/readiness check.                                                               |
| `/openapi/v1.json`                              | GET    | NA       | When ASPNETCORE_ENVIRONMENT=Development                                                          |

Complete documentation is provided [here](./OpenAPI/Microsoft.Identity.Web.Sidecar.json)

### Options overrides

All token-acquisition endpoints accept dotted query parameters prefixed with `optionsOverride.`; they merge into a `DownstreamApis` profile through [`BindableDownstreamApiOptions`](Models/BindableDownstreamApiOptions.cs).

Whether overrides are honoured is controlled by the per-route `Sidecar:AllowOverrides` configuration flags. Authenticated routes allow overrides by default; unauthenticated routes ignore them unless the operator explicitly opts in. See [`SidecarOptions`](Configuration/SidecarOptions.cs) for details.

`optionsOverride.BaseUrl` is always ignored regardless of the override flag.

Examples:
- `?optionsOverride.Scopes=User.Read&optionsOverride.Scopes=Mail.Read`
- `?optionsOverride.RequestAppToken=true`
- `?optionsOverride.AcquireTokenOptions.Tenant=<tenant-guid>`
- `?optionsOverride.RelativePath=me/messages`

Agent identity parameters are also subject to the per-route override flag:
- `AgentIdentity=<client-id>`
- `AgentUsername=upn@contoso.com`
- `AgentUserId=<oid>`

### Response contract

- `/AuthorizationHeader*` returns `{ "authorizationHeader": "Bearer ey..." }`.
- `/DownstreamApi*` returns `{ "statusCode": 200, "headers": { ... }, "content": "..." }`.
- `/Validate` returns `{ "protocol": "Bearer", "token": "ey...", "claims": { ... } }` (or `"protocol": "PoP"` for a validated PoP request).

## Security considerations

- This API is only for usage as a sidecar. This API should not be publicly callable as it
  allows the caller to acquire tokens on behalf of the applications identity.
- Inbound PoP binds to the `original-method`/`original-uri` headers supplied by the co-located
  calling application; the sidecar does not independently observe the downstream request. The
  `ts`-based freshness check (default five minutes) is not a replay cache, and server nonce is not
  implemented.

## Runtime composition

| Concern                        | Implementation                                                                                                                                                                                                     |
| ------------------------------ | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| Authentication & authorization | [`Program`](Program.cs) wires `AddMicrosoftIdentityWebApi`, optional scope enforcement, agent identity overrides, and additive inbound PoP validation ([`Pop`](Pop/)).                                                                                              |
| Endpoints                      | [`ValidateRequestEndpoints`](Endpoints/ValidateRequestEndpoints.cs), [`AuthorizationHeaderEndpoint`](Endpoints/AuthorizationHeaderEndpoint.cs), and [`DownstreamApiEndpoint`](Endpoints/DownstreamApiEndpoint.cs). |
| Downstream API                 | [`BindableDownstreamApiOptions`](Models/BindableDownstreamApiOptions.cs) merges per-request overrides into per call `DownstreamApis` configuration.                                                                |
| Agent Identities               | [`AgentOverrides`](AgentOverrides.cs) binds agent identity, userPrincipalName, or user object ID when present and allowed by the per-route override flag.                                                          |


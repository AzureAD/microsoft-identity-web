# Bearer-over-mTLS client auth for user flows (`UseBoundCredential`)

## Summary

`UseBoundCredential: true` configures certificate-based mTLS client
authentication for token acquisition. The access token remains a plain
**bearer** token, and downstream API calls remain unchanged. This maps to MSAL
`CertificateOptions.SendCertificateOverMtls`.

The knob **already ships** (abstractions 12.1.0) and already works for **app
tokens** — see the merged sample
[`daemon-app-cert-bound`](../../tests/DevApps/daemon-app/daemon-app-cert-bound).
This spec **extends the same knob to user flows** (authorization-code
redemption, OBO, and refresh-token redemption).

**No new public API.** MSAL [#6009](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/pull/6009)
makes the delegated flows honor the flag through IdWeb's existing credential
configuration.

## Not to be confused with `ProtocolScheme = "MTLS_POP"`

| | `UseBoundCredential: true` with a certificate credential | `ProtocolScheme: "MTLS_POP"` ([#3832](./msi-fic-pure-mtls-pop-devex.md)) |
|---|---|---|
| Set on | the **credential** (`ClientCredentials`) | the **downstream API** options |
| mTLS applies to | app → ESTS **client auth** | the **downstream API** call |
| Access token | **bearer** | **sender-constrained** (PoP) |
| Credential | certificate only | certificate, MI, FIC-with-MI |

Independent; may both be set.

## Flow coverage

| Flow | IdWeb usage | Bearer-over-mTLS |
|---|---|---|
| App token (daemon) | `IDownstreamApi.CallApiForAppAsync` / `IAuthorizationHeaderProvider.CreateAuthorizationHeaderForAppAsync` | ✅ shipped |
| Authorization-code redemption | `AddMicrosoftIdentityWebApp(...).EnableTokenAcquisitionToCallDownstreamApi()` | ✅ via #6009 |
| On-behalf-of | `IDownstreamApi.CallApiForUserAsync` / `IAuthorizationHeaderProvider.CreateAuthorizationHeaderForUserAsync` | ✅ via #6009 |
| Silent refresh | Handled internally when user-token acquisition requires refresh | ✅ via #6009 |

## Config — web API (OBO)

```json
{
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "TenantId": "<tenant>",
    "ClientId": "<api-client-id>",
    "Audience": "api://<api-client-id>",
    "ClientCredentials": [
      {
        "SourceType": "StoreWithThumbprint",
        "CertificateStorePath": "CurrentUser/My",
        "CertificateThumbprint": "<thumbprint>",
        "UseBoundCredential": true
      }
    ]
  },
  "DownstreamApis": {
    "GraphAPI": {
      "BaseUrl": "https://graph.microsoft.com/v1.0",
      "Scopes": [ "https://graph.microsoft.com/.default" ]
    }
  }
}
```

## Config — web app (sign-in + call Graph)

```json
{
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "TenantId": "<tenant>",
    "ClientId": "<client-id>",
    "CallbackPath": "/signin-oidc",
    "ClientCredentials": [
      {
        "SourceType": "StoreWithThumbprint",
        "CertificateStorePath": "CurrentUser/My",
        "CertificateThumbprint": "<thumbprint>",
        "UseBoundCredential": true
      }
    ]
  },
  "DownstreamApi": {
    "BaseUrl": "https://graph.microsoft.com/v1.0",
    "Scopes": [ "user.read" ]
  }
}
```

Only delta vs. a plain-bearer config: the `"UseBoundCredential": true` line, and
a **certificate** source (not `ClientSecret` — bearer-over-mTLS is cert-only).
`DownstreamApi(s)` is unchanged — no `ProtocolScheme`, no extra headers.

## Wiring (unchanged from any cert app)

```csharp
// Web app
builder.Services
    .AddMicrosoftIdentityWebAppAuthentication(builder.Configuration)
    .EnableTokenAcquisitionToCallDownstreamApi()
        .AddDownstreamApi("DownstreamApi", builder.Configuration.GetSection("DownstreamApi"))
        .AddInMemoryTokenCaches();

// Web API (OBO)
builder.Services
    .AddMicrosoftIdentityWebApiAuthentication(builder.Configuration)
    .EnableTokenAcquisitionToCallDownstreamApi()
        .AddDownstreamApis(builder.Configuration.GetSection("DownstreamApis"))
        .AddInMemoryTokenCaches();
```

Then `await api.CallApiForUserAsync("GraphAPI")`. No mTLS-specific C#.

## How it works

1. IdWeb maps `UseBoundCredential: true` to MSAL's certificate-bound
   configuration.
2. MSAL performs token acquisition using mTLS client authentication and returns
   a **bearer** token.
3. `IDownstreamApi` calls the downstream API using `Authorization: Bearer` as
   usual.

## Implementation status

* ✅ `Microsoft.Identity.Client` 4.87.0, containing #6009, is referenced.
* ✅ Authorization-code, OBO, and refresh-token tests were added in [#3996](https://github.com/AzureAD/microsoft-identity-web/pull/3996).
* ⬜ Add coverage for silent acquisition when refresh-token redemption is
  required.
* ⬜ Add `web-app-bound-credential` and `web-api-obo-bound-credential` samples.
* ⬜ Update the certificate-credential documentation and add a note in
  `token-binding.md` contrasting this flow with `MTLS_POP`.

## Prerequisites

* MSAL with #6009 (user-flow bearer-over-mTLS).
* Abstractions ≥ 12.1.0 (already referenced — `UseBoundCredential` shipped in [PR #255](https://github.com/AzureAD/microsoft-identity-abstractions-for-dotnet/pull/255)).
* A **certificate** credential. Bound signed-assertion credentials are outside
  the scope of this document.
* Cert registered on the app; tenant/app allow-listed for mTLS client auth (else `AADSTS700027`).

## References

* MSAL [#6009](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/pull/6009) — extends `SendCertificateOverMtls` to OBO / RT / auth-code.
* MSAL [#5791](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/5791) — origin issue.
* [`MTLS_POP` devex](./msi-fic-pure-mtls-pop-devex.md) — the sender-constrained counterpart.

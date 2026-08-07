# Bearer-over-mTLS client auth for user flows (`UseBoundCredential`)

## Summary

`UseBoundCredential: true` presents the app's certificate at the TLS layer
(`mtlsauth.microsoft.com`) to authenticate to Entra ID, instead of a
`client_assertion` JWT in the body. The access token is still a plain **bearer**
token; only the app→ESTS leg changes. Maps to MSAL
`CertificateOptions.SendCertificateOverMtls`.

The knob **already ships** (abstractions 12.1.0) and already works for **app
tokens** — see the merged sample
[`daemon-app-cert-bound`](../../tests/DevApps/daemon-app/daemon-app-cert-bound).
This spec **extends the same knob to user flows** (auth code, OBO, silent).

**No new public API.** MSAL [#6009](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/pull/6009)
makes the delegated flows honor the flag; IdWeb just needs the MSAL bump +
tests + docs/samples.

## Not to be confused with `ProtocolScheme = "MTLS_POP"`

| | `UseBoundCredential: true` | `ProtocolScheme: "MTLS_POP"` ([#3832](./msi-fic-pure-mtls-pop-devex.md)) |
|---|---|---|
| Set on | the **credential** (`ClientCredentials`) | the **downstream API** options |
| mTLS applies to | app → ESTS **client auth** | the **downstream API** call |
| Access token | **bearer** | **sender-constrained** (PoP) |
| Credential | certificate only | certificate, MI, FIC-with-MI |

Independent; may both be set.

## Flow coverage

| Flow | Entry point | Bearer-over-mTLS |
|---|---|---|
| App token (daemon) | `RequestAppToken` / `CallApiForApp` | ✅ shipped |
| Web-app sign-in | `AddMicrosoftIdentityWebApp` | ✅ via #6009 |
| On-behalf-of | `EnableTokenAcquisitionToCallDownstreamApi` | ✅ via #6009 |
| Silent / refresh-token | `AcquireTokenSilent` | ✅ via #6009 |

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

1. `UseBoundCredential: true` → IdWeb builds the CCA with
   `WithCertificate(cert, new CertificateOptions { SendCertificateOverMtls = true })`
   (`ConfidentialClientApplicationBuilderExtension.WithClientCredentialsAsync`).
2. Post-#6009, auth-code / OBO / silent route to `mtlsauth.microsoft.com`,
   present the cert at TLS (x5c auto-enabled).
3. MSAL returns a **bearer** token; IdWeb's default `MsalMtlsHttpClientFactory`
   supplies the mTLS transport — no extra wiring.
4. `IDownstreamApi` calls downstream with `Authorization: Bearer` as usual.

## Work items

* Bump `Microsoft.Identity.Client` to the build containing #6009.
* Delegated-flow tests (auth-code, OBO, silent).
* Two samples: `web-app-bound-credential`, `web-api-obo-bound-credential`.
* Docs: cert-credentials page + a note in `token-binding.md` contrasting with `MTLS_POP`.

## Prerequisites

* MSAL with #6009 (user-flow bearer-over-mTLS).
* Abstractions ≥ 12.1.0 (already referenced — `UseBoundCredential` shipped in [PR #255](https://github.com/AzureAD/microsoft-identity-abstractions-for-dotnet/pull/255)).
* A **certificate** credential (other credential types ignore the flag).
* Cert registered on the app; tenant/app allow-listed for mTLS client auth (else `AADSTS700027`).

## References

* MSAL [#6009](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/pull/6009) — extends `SendCertificateOverMtls` to OBO / RT / auth-code.
* MSAL [#5791](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/5791) — origin issue.
* [`MTLS_POP` devex](./msi-fic-pure-mtls-pop-devex.md) — the sender-constrained counterpart.

# Microsoft.Identity.Web – Bearer tokens with Bound credentials

> **Status:** Draft. The final API surface (property name and type) is being
> decided in
> [microsoft-identity-abstractions-for-dotnet #252](https://github.com/AzureAD/microsoft-identity-abstractions-for-dotnet/pull/252).
> The current proposal on that PR is `public bool UseBoundCredential { get; set; }`;
> reviewer feedback has suggested renaming to `PreferBoundCredential` and
> changing the type to `bool?` so the default can evolve over time. This
> IdWeb spec will be updated once the abstractions PR is finalized.

## Why bound credentials for Bearer tokens?

Today, when a confidential client app requests a Bearer access token, the
*credential* presented to Entra is also a Bearer artifact: either a client
assertion JWT signed locally with an X.509 certificate, or a signed assertion
issued by a federation provider (e.g. Managed Identity).

A **bound credential** is a sender-constrained variant:

* For an **X.509 certificate**, MSAL calls the Entra mTLS endpoint and presents
  the certificate over mTLS. No client assertion JWT is created.
* For a **Federated Identity Credential (FIC) with Managed Identity**, MSAL
  fetches a bound credential bundle (signed assertion + binding certificate)
  from MSI and calls Entra over mTLS using the binding certificate.

In **both** cases the access token returned to the app is a regular **Bearer
token**. The downstream API is unaffected.

This spec adds the developer experience for opting credentials into the
bound-credential flow in Microsoft.Identity.Web.

## What this spec adds to **Microsoft.Identity.Web**

* **Per-credential opt-in** – one configuration knob
  (`"UseBoundCredential": true` — final name pending the discussion on
  [microsoft-identity-abstractions-for-dotnet #252](https://github.com/AzureAD/microsoft-identity-abstractions-for-dotnet/pull/252)).
* **Per-credential scope** – each entry in `AzureAd.ClientCredentials[]`
  decides for itself. An app can declare a bound primary credential and a
  non-bound fallback in the same array, and IdWeb will honor each entry
  independently.
* **Two credential paths** – the opt-in is honored for `Certificate` (and the
  certificate-flavored sources) and `SignedAssertionFromManagedIdentity`.
  Other source types ignore the flag.
* **No change to downstream APIs** – the access token returned is a regular
  Bearer; the `DownstreamApi` section is untouched.

The goal is **zero-touch** for downstream consumers and **one-line** for the
app developer at the credential level.

## Support matrix

| Credential source                                              | Bearer + bound credential after this change |
|----------------------------------------------------------------|---------------------------------------------|
| Certificate (`Certificate`, `KeyVault`, `Path`, `Base64Encoded`, `StoreWith*`, `ManagedCertificate`) | ✅                                          |
| `SignedAssertionFromManagedIdentity` (FIC with MI)             | ✅                                          |
| `SignedAssertion` from OIDC IdP (`Microsoft.Identity.Web.OidcFIC`) | Planned — tracked in [#3851](https://github.com/AzureAD/microsoft-identity-web/issues/3851) |
| `ClientSecret`                                                 | n/a                                         |
| `SignedAssertion*` (non-MI)                                    | Ignored                                     |

### OIDC FIC as a first-class binding-capable source
 
 `OidcIdpSignedAssertionProvider` (in `Microsoft.Identity.Web.OidcFIC`) currently
 produces bearer-only JWT assertions from an external OIDC IdP. The same
 `SupportsTokenBinding` / `GetSignedAssertionWithBindingAsync` extension point
 that `ManagedIdentityClientAssertion` opts into can be extended to OIDC FIC
 once the cert-sourcing model is settled. That work is tracked in
 [#3851](https://github.com/AzureAD/microsoft-identity-web/issues/3851); this
 spec will be revised alongside that issue.

## How developers wire things up today (non-bound Bearer)

### Certificate

```json
{
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "TenantId": "<tenant>",
    "ClientId": "<app-registration-client-id>",
    "ClientCredentials": [
      {
        "SourceType": "StoreWithDistinguishedName",
        "CertificateStorePath": "CurrentUser/My",
        "CertificateDistinguishedName": "CN=MyAppCert"
      }
    ]
  }
}
```

### FIC with Managed Identity

```json
{
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "TenantId": "<tenant>",
    "ClientId": "<app-registration-client-id>",
    "ClientCredentials": [
      {
        "SourceType": "SignedAssertionFromManagedIdentity",
        "ManagedIdentityClientId": "<UAMI-client-id>",
        "TokenExchangeUrl": "api://AzureADTokenExchange"
      }
    ]
  }
}
```

In both cases the call to `/token` carries a client assertion JWT on the
wire.

## Design Goals

| #   | Goal                                                                            | Success Metric                                                                                  |
|-----|---------------------------------------------------------------------------------|-------------------------------------------------------------------------------------------------|
| G1  | Honor `UseBoundCredential` for `Certificate`-flavored credentials.              | Token call goes to the Entra mTLS endpoint over mTLS; no JWT assertion on the wire.             |
| G2  | Honor `UseBoundCredential` for `SignedAssertionFromManagedIdentity`.            | Inner MI fetches a bound credential bundle; outer call goes to Entra over mTLS.                 |
| G3  | Per-credential opt-in.                                                          | Two credentials in the same `ClientCredentials[]` can have different settings.                  |
| G4  | No downstream changes.                                                          | Existing `DownstreamApi` sections and `IDownstreamApi` calls work unchanged.                    |
| G5  | Clear behavior when the platform cannot provide a binding certificate.          | Either silent fallback to non-bound or a clear exception, depending on the final property name. |

## Public API Impact

The abstractions surface
([microsoft-identity-abstractions-for-dotnet #252](https://github.com/AzureAD/microsoft-identity-abstractions-for-dotnet/pull/252))
already adds the property:

```csharp
public bool UseBoundCredential { get; set; }
```
(Final name and type subject to review on that PR.)

No new types or extension methods on the IdWeb surface itself. IdWeb reads
the property at credential-load time and forwards it to MSAL.

## Configuration Example

### Certificate + bound credential

```json
{
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "TenantId": "<tenant>",
    "ClientId": "<app-registration-client-id>",
    "ClientCredentials": [
      {
        "SourceType": "StoreWithDistinguishedName",
        "CertificateStorePath": "CurrentUser/My",
        "CertificateDistinguishedName": "CN=MyAppCert",
        "UseBoundCredential": true
      }
    ]
  }
}
```

### FIC with MI + bound credential

```json
{
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "TenantId": "<tenant>",
    "ClientId": "<app-registration-client-id>",
    "ClientCredentials": [
      {
        "SourceType": "SignedAssertionFromManagedIdentity",
        "ManagedIdentityClientId": "<UAMI-client-id>",
        "TokenExchangeUrl": "api://AzureADTokenExchange",
        "UseBoundCredential": true
      }
    ]
  }
}
```

### Mixed (bound primary, non-bound fallback)

```json
{
  "AzureAd": {
    "ClientCredentials": [
      {
        "SourceType": "StoreWithDistinguishedName",
        "CertificateDistinguishedName": "CN=Primary",
        "UseBoundCredential": true
      },
      {
        "SourceType": "StoreWithDistinguishedName",
        "CertificateDistinguishedName": "CN=Fallback"
      }
    ]
  }
}
```

> **Note** : The same configuration block works in *appsettings.json* or can
> be supplied programmatically via
> `MicrosoftIdentityApplicationOptions.ClientCredentials`.

## Code Snippets

### Acquiring a Bearer token with a bound credential

```csharp
// 1 – set up the TokenAcquirerFactory
var factory = TokenAcquirerFactory.GetDefaultInstance();

// 2 – register the downstream API (unchanged from today)
factory.Services.AddDownstreamApi("Contoso",
    factory.Configuration.GetSection("Contoso"));

IServiceProvider sp = factory.Build();
IDownstreamApi api   = sp.GetRequiredService<IDownstreamApi>();

// 3 – call the API. Id.Web reads ClientCredentials[].UseBoundCredential
//     and wires the bound-credential flow into the MSAL builder.
HttpResponseMessage resp = await api.CallApiForAppAsync("Contoso");
```

The application code is **identical** to the Bearer-only case. The opt-in
lives entirely in `appsettings.json`.

### Using **IAuthorizationHeaderProvider**

`IAuthorizationHeaderProvider` is fully supported. The returned header is a
standard `Bearer <token>` header:

```csharp
var headerProvider = sp.GetRequiredService<IAuthorizationHeaderProvider>();
string header = await headerProvider.CreateAuthorizationHeaderForAppAsync(
    scope: "https://contoso.com/.default");
// header => "Bearer eyJ0eXAi..."
```

## How it works

At credential-load time, IdWeb inspects `credential.UseBoundCredential` on
each `CredentialDescription` and forwards it to MSAL:

1. **Certificate-flavored credential, `UseBoundCredential == true`** —
   `ConfidentialClientApplicationBuilderExtension.WithClientCredentialsAsync`
   calls `WithCertificate(cert, new CertificateOptions { SendCertificateOverMtls = true })`.
   MSAL routes the `/token` call to the Entra mTLS endpoint over mTLS using
   the certificate; no JWT assertion is created.
2. **`SignedAssertionFromManagedIdentity`, `UseBoundCredential == true`** —
   `ManagedIdentityClientAssertion` calls the V2 MI credential endpoint to
   obtain a bound credential bundle (signed assertion + binding certificate),
   and IdWeb passes the bundle to MSAL via
   `WithClientAssertion(Func<AssertionRequestOptions, CancellationToken, Task<ClientSignedAssertion>>)`.
   MSAL uses the binding certificate to talk to Entra over mTLS.
3. **Other source types, or `UseBoundCredential == false`** — current
   behavior is preserved. No change.

In all cases the access token returned to the app is a standard Bearer
token. The downstream HTTP call is the existing `IDownstreamApi` /
`IAuthorizationHeaderProvider` flow.

## Samples

Two samples will be added, modeled on the existing `daemon-app-msi`:

| Folder                                                       | Demonstrates                                                |
|--------------------------------------------------------------|-------------------------------------------------------------|
| `tests/DevApps/daemon-app/daemon-app-cert-bound`             | Certificate credential opted into Bearer-over-mTLS          |
| `tests/DevApps/daemon-app/daemon-app-fic-bound`              | FIC + MI credential opted into Bearer-over-mTLS             |

Each sample is one `Program.cs` (boilerplate `TokenAcquirerFactory` +
`IDownstreamApi.CallApiForAppAsync`) plus one `appsettings.json` matching
the configuration example above.

## Documentation updates

| File                                                       | Update                                                                                                  |
|------------------------------------------------------------|---------------------------------------------------------------------------------------------------------|
| `docs/authentication/credentials/certificates.md`          | Add a "Use as a bound credential" subsection showing the `UseBoundCredential: true` opt-in.             |
| `docs/authentication/credentials/certificateless.md`       | Add a "Use as a bound credential" subsection for FIC with MI, side-by-side with the existing config.    |

## Prerequisites

* `Microsoft.Identity.Web` takes a dependency on the abstractions version
  that ships `CredentialDescription.UseBoundCredential` (merge of
  [microsoft-identity-abstractions-for-dotnet #252](https://github.com/AzureAD/microsoft-identity-abstractions-for-dotnet/pull/252)).
* MSAL.NET already shipped the underlying capability in
  [microsoft-authentication-library-for-dotnet #5849](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/pull/5849).
* The flow currently covers `client_credentials` and is **Entra-only**
  (not enabled in all clouds).
* The application's tenant and client do **not** need to be on any allow-list
  for this flow.
* Platform support for the binding certificate today:
  * Certificate flow: any platform — uses the app's existing certificate.
  * FIC flow: requires the V2 MI credential endpoint, currently Windows
    Confidential VMs (Key Guard + attestation).

## Open questions

1. **Naming** – per
   [the discussion on #5791](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/5791),
   should the property be `UseBoundCredential` (binary, explicit) or
   `PreferBoundCredential` (best-effort, with silent fallback when the
   platform cannot provide a binding certificate)? Decision lives in
   abstractions PR #252; IdWeb consumes whatever ships.
2. **Default-flip strategy** – if the property is renamed to
   `PreferBoundCredential` and the type changes to `bool?`, IdWeb can pass a
   "host default" through to MSAL when the value is `null`. Worth deciding
   whether IdWeb opts apps in by default in a future release, or stays
   off-by-default and requires explicit opt-in.
 3. **OIDC FIC cert source** – when the assertion is issued by an external OIDC
    IdP, where does the binding certificate come from? Tracked in
    [#3851](https://github.com/AzureAD/microsoft-identity-web/issues/3851).
    
### reference

* [microsoft-authentication-library-for-dotnet issue #5791](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/5791)
  – feature request.
* [microsoft-authentication-library-for-dotnet PR #5849](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/pull/5849)
  – MSAL.NET implementation (shipped).
* [microsoft-identity-abstractions-for-dotnet PR #252](https://github.com/AzureAD/microsoft-identity-abstractions-for-dotnet/pull/252)
  – `CredentialDescription.UseBoundCredential` (in review).

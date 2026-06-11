# Microsoft.Identity.Web – mTLS Proof-of-Possession for Managed Identity and FIC

## Why mTLS Proof-of-Possession?

mTLS Proof-of-Possession (mTLS PoP) is a sender-constrained token format. The
access token is bound to a client certificate and the call to the downstream
API is made over mTLS using that certificate. A leaked or replayed bearer
token is useless to an attacker who does not also hold the private key.

Microsoft.Identity.Web already supports mTLS PoP for confidential client
applications that authenticate with a certificate
([`docs/calling-downstream-apis/token-binding.md`](../calling-downstream-apis/token-binding.md)).
The same `"ProtocolScheme": "MTLS_POP"` opt-in is silently ignored by two
other credential sources that app developers rely on heavily:

* `AcquireTokenOptions.ManagedIdentity` (pure Managed Identity)
* `SignedAssertionFromManagedIdentity` (Federated Identity Credential signed
  by a Managed Identity)

This spec closes that gap.

## What this spec adds to **Microsoft.Identity.Web**

* **Declarative opt-in** – one configuration knob
  (`"ProtocolScheme": "MTLS_POP"` on the downstream API), identical to the
  existing certificate-based flow.
* **Two new credential paths** – the same opt-in now works with Managed
  Identity and FIC-with-MI.
* **Transparent binding** – IdWeb forwards the request to MSAL, MSAL
  provisions the binding certificate via the V2 Managed Identity credential
  API (Key Guard + attestation), and the downstream HTTP call uses that
  certificate over mTLS.

The goal is **zero-touch** for developers who already use Managed Identity or
FIC for bearer tokens.

## Support matrix

| Credential source | Bearer | mTLS PoP today | mTLS PoP after this change |
|---|---|---|---|
| Certificate (Key Vault, Path, Store, Base64) | ✅ | ✅ | ✅ |
| Client secret | ✅ | n/a | n/a |
| `SignedAssertionFromManagedIdentity` (FIC) | ✅ | ❌ | ✅ |
| `AcquireTokenOptions.ManagedIdentity` (MSI) | ✅ | ❌ | ✅ |

## How developers wire things up today (Bearer)

### Managed Identity

```json
{
  "AzureKeyVault": {
    "BaseUrl": "https://contoso.vault.azure.net/",
    "RelativePath": "secrets/mysecret?api-version=7.4",
    "Scopes": [ "https://vault.azure.net/.default" ],
    "RequestAppToken": true,
    "AcquireTokenOptions": {
      "ManagedIdentity": {
        "UserAssignedClientId": "<UAMI-client-id>"
      }
    }
  }
}
```

Sample: [`tests/DevApps/daemon-app/daemon-app-msi`](../../tests/DevApps/daemon-app/daemon-app-msi).

### Federated Identity Credential with Managed Identity

```json
{
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "TenantId": "<tenant>",
    "ClientId": "<app-registration-client-id>",
    "ClientCredentials": [
      {
        "SourceType": "SignedAssertionFromManagedIdentity",
        "ManagedIdentityClientId": "<UAMI-client-id>"
      }
    ]
  },
  "AzureKeyVault": {
    "BaseUrl": "https://contoso.vault.azure.net/",
    "Scopes": [ "https://vault.azure.net/.default" ],
    "RequestAppToken": true
  }
}
```

Docs: [`certificateless.md`](../authentication/credentials/certificateless.md).

## What is missing today

### Managed Identity + `"ProtocolScheme": "MTLS_POP"`

The Managed Identity code path in `TokenAcquisition` does not check the
`IsTokenBinding` flag that the downstream-API binder sets when
`ProtocolScheme` is `"MTLS_POP"`. A plain bearer token is returned with no
error. If the downstream API enforces mTLS PoP, the call fails downstream
with nothing in the IdWeb logs to explain why.

### FIC + `"ProtocolScheme": "MTLS_POP"`

`ConfidentialClientApplicationBuilderExtension.WithClientCredentialsAsync`
requires a `Certificate` on the credential when `isTokenBinding` is `true`. A
signed assertion has no certificate, so the call throws:

```
InvalidOperationException: A certificate, which is required for token binding,
is missing in loaded credentials.
```

The error is accurate but the underlying cause is "this combination was never
implemented."

## Design Goals

| #   | Goal                                                                  | Success Metric                                                       |
|-----|-----------------------------------------------------------------------|----------------------------------------------------------------------|
| G1  | Honor `"ProtocolScheme": "MTLS_POP"` on the Managed Identity path.    | MSI request issues an mTLS-bound token; downstream call is over mTLS. |
| G2  | Honor `"ProtocolScheme": "MTLS_POP"` on the FIC-with-MI path.         | FIC request issues an mTLS-bound token; downstream call is over mTLS. |
| G3  | Configuration parity with the certificate flow.                       | One knob (`ProtocolScheme`) for all three credential sources.        |
| G4  | No new typed public API on `DownstreamApiOptions`.                    | Opt-in stays declarative; no C# wiring required by app developers.   |

## Public API Impact

No changes to the public API. The behavior change is entirely behind the
existing `"ProtocolScheme": "MTLS_POP"` configuration value.

## Configuration Example

### Managed Identity + mTLS PoP

```json
{
  "AzureKeyVault": {
    "BaseUrl": "https://contoso.vault.azure.net/",
    "Scopes": [ "https://vault.azure.net/.default" ],
    "RequestAppToken": true,
    "ProtocolScheme": "MTLS_POP",
    "AcquireTokenOptions": {
      "ManagedIdentity": {
        "UserAssignedClientId": "<UAMI-client-id>"
      }
    }
  }
}
```

Omit `UserAssignedClientId` for system-assigned Managed Identity.

### FIC + mTLS PoP

```json
{
  "AzureAd": {
    "TenantId": "<tenant>",
    "ClientId": "<app-registration-client-id>",
    "ClientCredentials": [
      {
        "SourceType": "SignedAssertionFromManagedIdentity",
        "ManagedIdentityClientId": "<UAMI-client-id>"
      }
    ]
  },
  "AzureKeyVault": {
    "BaseUrl": "https://contoso.vault.azure.net/",
    "Scopes": [ "https://vault.azure.net/.default" ],
    "RequestAppToken": true,
    "ProtocolScheme": "MTLS_POP"
  }
}
```

> **Note** : The same configuration block works in *appsettings.json* or can
> be supplied programmatically.

## Code Snippets

### Registering & Calling a Downstream API

```csharp
// 1 – set up the TokenAcquirerFactory
var factory = TokenAcquirerFactory.GetDefaultInstance();

// 2 – register the downstream API using section "AzureKeyVault"
factory.Services.AddDownstreamApi("AzureKeyVault",
    factory.Configuration.GetSection("AzureKeyVault"));

IServiceProvider sp = factory.Build();
IDownstreamApi api   = sp.GetRequiredService<IDownstreamApi>();

// 3 – call the API (Id.Web handles mTLS PoP automatically)
HttpResponseMessage resp = await api.CallApiForAppAsync("AzureKeyVault");
```

### Using **IAuthorizationHeaderProvider** (advanced)

`IAuthorizationHeaderProvider` works with Managed Identity and FIC mTLS PoP
tokens the same way it works with the certificate flow:

```csharp
var headerProvider = sp.GetRequiredService<IAuthorizationHeaderProvider>();
string header = await headerProvider.CreateAuthorizationHeaderForAppAsync(
    scope: "https://vault.azure.net/.default",
    options: new AuthorizationHeaderProviderOptions
    {
        ProtocolScheme = "MTLS_POP",
        AcquireTokenOptions = new AcquireTokenOptions
        {
            ManagedIdentity = new ManagedIdentityOptions()
        }
    });
```

## How it works

For both new paths, `TokenAcquisition` reads the `IsTokenBinding` flag the
downstream-API binder set when `ProtocolScheme` is `"MTLS_POP"` and forwards
it to MSAL:

1. **Managed Identity path** – the MSAL `ManagedIdentityApplication` builder
   is configured with `WithMtlsProofOfPossession()`. MSAL calls the V2
   Managed Identity credential endpoint, which returns an mTLS-bound token
   plus the binding certificate.
2. **FIC-with-MI path** – the inner Managed Identity client used to sign the
   FIC assertion produces the binding certificate; the outer
   `ConfidentialClientApplication` request is bound to that certificate.

In both cases, `IDownstreamApi` issues the HTTP call over mTLS using the
binding certificate. App developers see exactly the same `IDownstreamApi` /
`IAuthorizationHeaderProvider` surface they use today.

## Samples

Two samples will be added, modeled on the existing `daemon-app-msi`:

| Folder                                              | Demonstrates                       |
|-----------------------------------------------------|------------------------------------|
| `tests/DevApps/daemon-app/daemon-app-msi-mtls`      | Managed Identity + mTLS PoP        |
| `tests/DevApps/daemon-app/daemon-app-fic-mtls`      | FIC-with-MI + mTLS PoP             |

Each sample is one `Program.cs` (boilerplate `TokenAcquirerFactory` +
`IDownstreamApi.CallApiForAppAsync`) plus one `appsettings.json` that matches
the configuration example above.

## Documentation updates

| File                                                           | Update                                                                                                  |
|----------------------------------------------------------------|---------------------------------------------------------------------------------------------------------|
| `docs/calling-downstream-apis/token-binding.md`                | Add MI and FIC subsections after the certificate config. Same `"ProtocolScheme": "MTLS_POP"` opt-in.    |
| `docs/authentication/credentials/certificateless.md`           | Add a one-paragraph "mTLS Proof-of-Possession" note linking back to `token-binding.md`.                 |
| `tests/DevApps/daemon-app/daemon-app-msi/readme.md`            | Add a one-line note pointing at `daemon-app-msi-mtls`.                                                  |

## Prerequisites

* `Microsoft.Identity.Web` takes a dependency on
  `Microsoft.Identity.Client.KeyAttestation` (GA in the next MSAL release).
* The downstream resource (Microsoft Graph, Azure Key Vault, a custom API)
  must accept mTLS PoP tokens. This is configured per-resource in ESTS.
* The application's tenant and client must be on the ESTS allow-list for
  mTLS-bound token issuance.

### reference

* [Token Binding with mTLS Proof-of-Possession](../calling-downstream-apis/token-binding.md)
  – existing certificate-based flow.
* [Certificateless Authentication](../authentication/credentials/certificateless.md)
  – existing FIC + Managed Identity (bearer) flow.

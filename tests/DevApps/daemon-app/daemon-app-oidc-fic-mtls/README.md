# daemon-app-oidc-fic-mtls (spike)

> **Spike sample** for [issue #3851](https://github.com/AzureAD/microsoft-identity-web/issues/3851). Demonstrates the **paired-credential** design (Option 1 in the issue) — *not yet approved*. Do not ship.

Daemon (console) sample that calls Azure Key Vault using a **Federated Identity Credential issued by an external OIDC IdP**, with the resulting app token bound to a configured certificate via **mTLS Proof-of-Possession** (PoP).

## What this demonstrates

Two-leg flow:

1. **Leg 1 — External OIDC IdP** (`Microsoft.Identity.Web.OidcFIC`)
   - `OidcIdpSignedAssertionProvider` authenticates to the external OIDC IdP using the *first* `ClientCredentials` entry under the `OidcFicIdp` section (here: a `ClientSecret`).
   - The IdP returns a federated JWT whose audience is `api://AzureADTokenExchange`.
2. **Leg 2 — Confidential Client → Entra ID**
   - Microsoft.Identity.Web sends that assertion to the application's tenant and asks for the downstream resource (Key Vault) access token.
   - `ProtocolScheme = "MTLS_POP"` on the `AzureKeyVault` downstream API options pins the outer token to the **binding certificate**, which the provider resolved from the `OidcFicIdp.ClientCredentials` entry flagged `UseBoundCredential = true`.

## Devex (the entire dev-facing surface)

```jsonc
"AzureAd": {
    "ClientCredentials": [{
        "SourceType": "CustomSignedAssertion",
        "CustomSignedAssertionProviderName": "OidcIdpSignedAssertion",
        "CustomSignedAssertionProviderData": { "ConfigurationSection": "OidcFicIdp" }
    }]
},
"OidcFicIdp": {
    "Instance": "https://external-idp.example.com/",
    "ClientId": "...",
    "ClientCredentials": [
        { "SourceType": "ClientSecret",        "ClientSecret":         "..."  },   // ← inner-leg auth (to external IdP)
        { "SourceType": "StoreWithThumbprint", "CertificateThumbprint": "...",
          "UseBoundCredential": true                                              // ← outer-leg binding cert (PR #3835 flag)
        }
    ]
},
"AzureKeyVault": {
    "RequestAppToken": true,
    "ProtocolScheme": "MTLS_POP",
    ...
}
```

No new public API on the IdWeb consumer surface — reuses `CredentialDescription.UseBoundCredential` from PR #3835 and the `SupportsTokenBinding` / `GetSignedAssertionWithBindingAsync` extension points from PR #3839.

## Prerequisites

- The `LabAuth.MSIDLab.com` certificate installed in `CurrentUser/My` — already present on dev boxes and IdWeb CI build agents.
- The MSI team's ESTS-allowlisted-for-mtls_pop app (`163ffef9-a313-45b4-ab2f-c7e2f5e0e23e` in tenant `bea21ebe-...`) — same coordinates the existing OidcFic_WithMtlsPop E2E test uses; LabAuth is already registered there as both a key credential and an mTLS PoP binding key.

## Run

```bash
dotnet run
```

Expected output on success: `SUCCESS: Got a bound mTLS-PoP token via OIDC FIC.`

---
name: mtls-pop
description: |
  Guide for configuring mTLS Proof-of-Possession (PoP) using Microsoft.Identity.Web.
  Covers all credential types: certificate, pure managed identity, and federated identity credentials (FIC).
  Use this when asked about mTLS PoP, MTLS_POP protocol scheme, token-bound authentication,
  or calling downstream APIs with proof-of-possession tokens.
license: MIT
---

# mTLS Proof-of-Possession (PoP)

This skill helps you configure **mTLS Proof-of-Possession** in applications that use **Microsoft.Identity.Web**. mTLS PoP cryptographically binds the access token to a client certificate, proving possession of the private key at the transport layer.

## When to Use This Skill

- Configuring `ProtocolScheme = "MTLS_POP"` in DownstreamApi options
- Calling downstream APIs with proof-of-possession tokens
- Troubleshooting 401 errors related to mTLS PoP
- Adding `x-ms-tokenboundauth` header for Azure Key Vault
- Understanding which credential type to use for mTLS PoP

---

## Credential Types

mTLS PoP supports three credential types. Each binds the token to a certificate differently:

| Credential Type | Certificate Source | Use Case |
|----------------|-------------------|----------|
| **Certificate** | App's own cert (from Key Vault, store, or file) | Confidential client apps with their own cert |
| **Pure MSI** | Binding cert from IMDS v2 | Azure VM/VMSS workloads using managed identity |
| **FIC** | MSI signed assertion + app cert | Apps using federated identity credentials |

---

## 1. Certificate Credential

The app uses its own certificate for both client authentication and PoP token binding.

```json
{
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "TenantId": "<tenant-id>",
    "ClientId": "<client-id>",
    "ClientCredentials": [
      {
        "SourceType": "KeyVault",
        "KeyVaultUrl": "https://myvault.vault.azure.net",
        "KeyVaultCertificateName": "my-app-cert"
      }
    ]
  },

  "DownstreamApi": {
    "BaseUrl": "https://myapi.contoso.com/",
    "RequestAppToken": true,
    "ProtocolScheme": "MTLS_POP",
    "Scopes": [ "api://<api-client-id>/.default" ]
  }
}
```

---

## 2. Pure Managed Identity (MSI)

The binding certificate comes from the **IMDS v2 credential endpoint** on the VM/VMSS. No app-owned cert needed.

**Requirements:** Azure VM or VMSS with Managed Identity v2 credential endpoint.

### System-Assigned MSI

```json
{
  "DownstreamApi": {
    "BaseUrl": "https://myapi.contoso.com/",
    "RequestAppToken": true,
    "ProtocolScheme": "MTLS_POP",
    "Scopes": [ "api://<api-client-id>/.default" ],
    "AcquireTokenOptions": {
      "ManagedIdentity": { }
    }
  }
}
```

### User-Assigned MSI

```json
{
  "DownstreamApi": {
    "BaseUrl": "https://myapi.contoso.com/",
    "RequestAppToken": true,
    "ProtocolScheme": "MTLS_POP",
    "Scopes": [ "api://<api-client-id>/.default" ],
    "AcquireTokenOptions": {
      "ManagedIdentity": {
        "UserAssignedClientId": "<user-assigned-mi-client-id>"
      }
    }
  }
}
```

---

## 3. Federated Identity Credential (FIC)

Uses a managed identity's signed assertion as a client credential for a regular Entra ID app. The downstream call configuration is the same as other types.

```json
{
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "TenantId": "<tenant-id>",
    "ClientId": "<app-client-id>",
    "ClientCredentials": [
      {
        "SourceType": "SignedAssertionFromManagedIdentity",
        "ManagedIdentityClientId": "<user-assigned-mi-client-id>"
      }
    ]
  },

  "DownstreamApi": {
    "BaseUrl": "https://myapi.contoso.com/",
    "RequestAppToken": true,
    "ProtocolScheme": "MTLS_POP",
    "Scopes": [ "api://<api-client-id>/.default" ]
  }
}
```

---

## Azure Key Vault: Extra Header Required

Azure Key Vault uses TLS renegotiation for client certificate presentation. It requires `x-ms-tokenboundauth: true` to trigger renegotiation. Without it, AKV returns 401: *"Client certificate required for using MTLS_POP token."*

**Other resources (ARM, Storage, Graph) do NOT need this header** — they bind the cert on the initial TLS handshake.

Add `ExtraHeaderParameters` to your AKV service config:

```json
{
  "AzureKeyVault": {
    "BaseUrl": "https://myvault.vault.azure.net/",
    "RelativePath": "secrets/mysecret?api-version=7.4",
    "RequestAppToken": true,
    "ProtocolScheme": "MTLS_POP",
    "Scopes": [ "https://vault.azure.net/.default" ],
    "AcquireTokenOptions": {
      "ManagedIdentity": {
        "UserAssignedClientId": "<your-mi-client-id>"
      }
    },
    "ExtraHeaderParameters": {
      "x-ms-tokenboundauth": "true"
    }
  }
}
```

### Sovereign Clouds

| Cloud | Vault Suffix | Scope |
|-------|-------------|-------|
| Commercial | `.vault.azure.net` | `https://vault.azure.net/.default` |
| China (21Vianet) | `.vault.azure.cn` | `https://vault.azure.cn/.default` |
| US Government | `.vault.usgovcloudapi.net` | `https://vault.usgovcloudapi.net/.default` |

All require `"x-ms-tokenboundauth": "true"`.

---

## How It Works

1. **Token Acquisition:** `ProtocolScheme = "MTLS_POP"` tells MSAL to request a PoP token. For MSI, IMDS v2 returns both the token and a binding certificate.

2. **HTTP Client:** `MsalMtlsHttpClientFactory` creates an `HttpClient` with the binding certificate in `HttpClientHandler.ClientCertificates`.

3. **Request:** The outbound request carries:
   - `Authorization: MTLS_POP <token>` — the proof-of-possession token
   - Client certificate via TLS — proves possession of the private key
   - `ExtraHeaderParameters` — any additional headers (e.g., `x-ms-tokenboundauth` for AKV)

4. **Validation:** The resource verifies the token's `cnf` claim matches the presented certificate thumbprint.

---

## Troubleshooting

| Issue | Cause | Solution |
|-------|-------|----------|
| 401 "Client certificate required for using MTLS_POP token" | Missing `x-ms-tokenboundauth` header | Add `ExtraHeaderParameters` with the header (AKV only) |
| 401 from AKV with valid token | Cert not bound to token | Verify VM has v2 IMDS endpoint returning binding cert |
| `MsalServiceException` "MTLS_POP not supported" | Platform doesn't support mTLS PoP | Only Azure VM/VMSS support MSI v2 with binding cert |
| Works on ARM but not AKV | Different cert binding mechanisms | AKV needs extra header; ARM doesn't |
| `ExtraHeaderParameters` silently dropped | Header is in reserved list | `x-ms-tokenboundauth` is NOT reserved — check spelling |
| Token works locally but not in prod | Local lacks MSI | mTLS PoP with MSI requires actual VM/VMSS; use cert credential locally |

---

## References

- [mTLS PoP support (PR #3839)](https://github.com/AzureAD/microsoft-identity-web/pull/3839)
- [ExtraHeaderParameters tests (PR #3864)](https://github.com/AzureAD/microsoft-identity-web/pull/3864)
- [Sample: daemon-app-msi-mtls](../../tests/DevApps/daemon-app/daemon-app-msi-mtls/)
- [Managed Identity docs](https://learn.microsoft.com/entra/identity/managed-identities-azure-resources/)
- [DownstreamApi options](https://learn.microsoft.com/entra/msal/dotnet/microsoft-identity-web/downstream-api)

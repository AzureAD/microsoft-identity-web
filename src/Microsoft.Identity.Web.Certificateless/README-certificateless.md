# Microsoft.Identity.Web.Certificateless

Enable confidential client flows in Microsoft Entra ID (Azure AD) **without managing X.509 certificates or static client secrets**.  
`Microsoft.Identity.Web.Certificateless` adds a pluggable client assertion provider that uses a trusted workload identity (Managed Identity, Azure AD Workload Identity for Kubernetes, etc.) to generate ephemeral JWT client assertions instead of loading a certificate specified in `AzureAd:ClientCredentials`.

> Conceptual background: [Certificateless overview](https://aka.ms/ms-id-web/certificateless)

---
## Table of contents
1. [Motivation](#1-motivation)
2. [How it fits with existing credential configuration](#2-how-it-fits-with-existing-credential-configuration)
3. [Configuration building blocks](#3-configuration-building-blocks)
4. [Common configuration patterns](#4-common-configuration-patterns)
5. [Hybrid (fallback) examples](#5-hybrid-fallback-examples)
6. [Kubernetes / Workload Identity](#6-kubernetes--azure-ad-workload-identity)
7. [Runtime precedence & decision logic](#7-runtime-precedence--decision-logic)
8. [Migration guidance](#8-migration-guidance)
9. [Troubleshooting & diagnostics](#9-troubleshooting--diagnostics)
10. [Security considerations](#10-security-considerations)
11. [FAQ](#11-faq)
12. [Appendix: CredentialDescription SourceType quick reference](#12-appendix-credentialdescription-sourcetype-quick-reference)

---
## 1. Motivation
Traditional confidential client setups require either secrets (discouraged) or certificates (which you must provision, secure, rotate). In Azure hosting environments you already *have* an identity (Managed Identity or a federated workload identity) whose lifecycle and key rotation are handled by the platform. Certificateless mode leverages that identity to build a valid OAuth2 client assertion (JWT bearer) per token request—no certificate objects, no Key Vault retrieval for signing material, no rotation tasks.

---
## 2. How it fits with existing credential configuration
`Microsoft.Identity.Web` normally discovers credentials via `AzureAd:ClientCredentials` (array of `CredentialDescription` objects). Each entry declares a `SourceType` describing where a certificate or pre-signed assertion comes from (KeyVault, StoreWithThumbprint, Path, Base64Encoded, SignedAssertionFilePath, etc.).

When `Certificateless:IsEnabled` is `true`, the certificateless assertion provider takes precedence. All `AzureAd:ClientCredentials` entries are ignored for client assertion generation. If `IsEnabled` is `false` (or absent), the existing `ClientCredentials` resolution logic applies unchanged.

This lets you keep certificate configuration as a fallback or for local development while enabling certificateless in production.

---
## 3. Configuration building blocks
### 3.1 Certificateless options (from `CertificatelessOptions`)
- `IsEnabled` (bool, default `false`): Activates certificateless mode.
- `ManagedIdentityClientId` (string?): Required only for *user-assigned* Managed Identity. Omit for system-assigned.

### 3.2 Existing credential descriptions (fallback surface)
Example (retained for rollback):
```jsonc
"AzureAd": {
  "TenantId": "TENANT_ID",
  "ClientId": "APP_CLIENT_ID",
  "ClientCredentials": [
    {
      "SourceType": "KeyVault",
      "KeyVaultUrl": "https://myvault.vault.azure.net/",
      "KeyVaultCertificateName": "my-app-cert"
    }
  ]
}
```
While certificateless is enabled these entries are not used.

---
## 4. Common configuration patterns
### A. Pure certificateless (system-assigned Managed Identity)
```jsonc
{
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "TenantId": "YOUR_TENANT_ID",
    "ClientId": "YOUR_APP_REGISTRATION_CLIENT_ID"
  },
  "Certificateless": { "IsEnabled": true }
}
```
### B. User-assigned Managed Identity
```jsonc
"Certificateless": {
  "IsEnabled": true,
  "ManagedIdentityClientId": "USER_ASSIGNED_MI_CLIENT_ID"
}
```
### C. Hybrid with certificate fallback (prod prefers certificateless)
`appsettings.json` (shared):
```jsonc
{
  "AzureAd": {
    "TenantId": "TENANT_ID",
    "ClientId": "APP_ID",
    "ClientCredentials": [
      { "SourceType": "KeyVault", "KeyVaultUrl": "https://myvault.vault.azure.net/", "KeyVaultCertificateName": "legacy-fallback-cert" }
    ]
  },
  "Certificateless": { "IsEnabled": false }
}
```
`appsettings.Production.json`:
```jsonc
{ "Certificateless": { "IsEnabled": true } }
```
### D. Environment variable enablement
```bash
export Certificateless__IsEnabled=true
# Only if user-assigned MI:
export Certificateless__ManagedIdentityClientId=00000000-0000-0000-0000-000000000000
```
### E. Fallback to a pre-signed assertion file (rare) kept as DR
Include a `SignedAssertionFilePath` entry but leave `IsEnabled:true` for normal operation.

---
## 5. Hybrid (fallback) examples
Keep your original certificate config. Toggle `IsEnabled` off to revert immediately—no code changes required.

---
## 6. Kubernetes / Azure AD Workload Identity
1. Configure a federated identity credential on the app registration matching the AKS OIDC issuer + ServiceAccount subject.
2. Deploy with the annotated ServiceAccount (projected token provided automatically).
3. Set `Certificateless:IsEnabled:true`.
4. Provide `ManagedIdentityClientId` only if targeting a separate user-assigned Managed Identity.
Logs (Information/Debug) trace assertion generation; a Kubernetes-specific provider type is used internally.

---
## 7. Runtime precedence & decision logic (simplified)
1. Read `Certificateless:IsEnabled`.
2. If `true`: use certificateless provider; ignore `AzureAd:ClientCredentials`.
3. Else: iterate credential descriptions by order; first resolvable credential wins (existing behavior).

---
## 8. Migration guidance
| Step | Action | Notes |
|------|--------|-------|
| 1 | Enable Managed or Workload Identity | Grant required roles/permissions |
| 2 | Add package reference `Microsoft.Identity.Web.Certificateless` | Keep existing `Microsoft.Identity.Web` |
| 3 | Add `Certificateless` section with `IsEnabled:false` | Safe dark launch |
| 4 | Deploy & verify baseline | Still using certificate |
| 5 | Flip `IsEnabled:true` in target env | Certificateless now active |
| 6 | Monitor logs, token acquisition | Watch for 401/invalid_client |
| 7 | Retire old certificate artifacts (optional) | Keep for DR if desired |

Rollback = set `IsEnabled:false`.

---
## 9. Troubleshooting & diagnostics
| Symptom | Likely cause | Resolution |
|---------|--------------|-----------|
| 401 invalid_client | MI / federated identity missing | Ensure identity assigned & federated mapping correct |
| 400 AADSTS7000215 | `IsEnabled` false and no valid credential entries | Enable certificateless or supply certificate |
| Timeout contacting IMDS | Running locally without MI | Disable certificateless locally |
| Ambiguous identity | User-assigned MI not specified | Set `ManagedIdentityClientId` |
| Works locally, fails in prod | Prod config not applied or no MI there | Verify deployment slot identity & config |

Enable console logging:
```csharp
builder.Logging.AddConsole().SetMinimumLevel(LogLevel.Information);
```
Look for messages about assertion acquisition and Managed Identity token retrieval.

---
## 10. Security considerations
- Removes static certificates/private keys from your operational surface.
- Assertions are ephemeral, reducing replay risk.
- Platform handles key rotation (Managed Identity / federated identity).
- Maintain least privilege on assigned roles.

---
## 11. FAQ
**Do I still need `AzureAd:ClientId`?** Yes—the logical application registration still applies.
**Can I keep a certificate fallback?** Yes; leave `ClientCredentials` in place and toggle `IsEnabled`.
**Is this the same as Azure SDK Managed Identity usage?** Similar trust anchor, but here it yields an OAuth2 client assertion for confidential client flows.
**Do I need `ManagedIdentityClientId` for system-assigned MI?** No.
**How do I revert?** Set `IsEnabled:false`.

---
## 12. Appendix: CredentialDescription SourceType quick reference
(Existing values ignored while certificateless is active.)

| SourceType | Description |
|------------|-------------|
| KeyVault | Certificate retrieved from Azure Key Vault |
| StoreWithThumbprint | Certificate from local cert store by thumbprint |
| StoreWithDistinguishedName | Certificate from local store by DN |
| Path | Certificate file on disk |
| Base64Encoded | Base64 certificate payload (optionally password) |
| Certificate | Already-instantiated certificate (in code) |
| SignedAssertionFilePath | Pre-signed static client assertion from file |
| SignedAssertionFromVault | Pre-signed assertion/secret from Key Vault |

---
## Summary
`Microsoft.Identity.Web.Certificateless` replaces manual certificate management with platform-managed workload identities. Enable it with a single configuration flag while retaining seamless fallback. This reduces operational overhead, improves secret hygiene, and simplifies rotation.

Happy coding!
# daemon-app-fic-mtls — FIC + mTLS PoP

This dev app demonstrates **mTLS Proof-of-Possession (PoP)** token acquisition
using **Federated Identity Credentials (FIC)** backed by a managed identity
signed assertion.

## Prerequisites

- Run on an Azure VM (or environment) that has a **user-assigned managed identity**.
- An **Entra ID app registration** configured with:
  - A **Federated Identity Credential** that trusts the managed identity.
  - Appropriate API permissions (e.g., Key Vault access).
- Update `appsettings.json` with:
  - `TenantId`: Your Entra ID tenant.
  - `ClientId`: The app registration's client ID.
  - `ManagedIdentityClientId`: The UAMI client ID used for the FIC assertion.

## How it works

1. `ProtocolScheme: "MTLS_POP"` in the downstream API config triggers token binding.
2. ID Web sets `IsTokenBinding = true` on the token acquisition options.
3. The CCA is built with the FIC signed assertion (from managed identity).
4. `WithMtlsProofOfPossession()` on the token request tells MSAL to:
   - Use the mTLS token endpoint
   - Provision a binding certificate via the V2 credential API
   - Bind the token to the certificate
5. The bound token and binding certificate are used to call Key Vault via mTLS.

## Running

```bash
dotnet run
```

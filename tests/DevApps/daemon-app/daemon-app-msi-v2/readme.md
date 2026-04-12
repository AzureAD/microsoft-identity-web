# daemon-app-msi-v2 — Pure MSI mTLS PoP

This dev app demonstrates **mTLS Proof-of-Possession (PoP)** token acquisition
using **pure Managed Identity** (MSI V2 credential API with attestation).

## Prerequisites

- Run on an Azure VM (or environment) that has a **user-assigned managed identity**
  with the client ID configured in `appsettings.json`.
- The managed identity must have **Key Vault Secrets User** (or equivalent) role
  on the target Key Vault.
- The environment must support the V2 credential API (IMDS with `cred-api-version=2.0`).

## How it works

1. `ProtocolScheme: "MTLS_POP"` in the downstream API config triggers token binding.
2. ID Web sets `IsTokenBinding = true` on the token acquisition options.
3. MSAL's managed identity flow uses the V2 credential API:
   - Obtains platform metadata from IMDS
   - Generates a key pair via KeyGuard
   - Creates a CSR and submits it for attestation
   - Receives a signed certificate
   - Acquires an mTLS-bound token from Entra ID
4. The bound token and binding certificate are used to call Key Vault via mTLS.

## Running

```bash
dotnet run
```

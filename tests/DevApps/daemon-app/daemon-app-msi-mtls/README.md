# daemon-app-msi-mtls

Daemon (console) sample that calls Azure Key Vault using a **pure Managed Identity**
access token with **mTLS Proof-of-Possession** (PoP).

## What this demonstrates

- `AuthorizationHeaderProviderOptions.ProtocolScheme = "MTLS_POP"` on the downstream API
  options triggers Microsoft.Identity.Web to:
  1. Request a binding certificate + access token from the V2 managed identity credential endpoint.
  2. Pin the outgoing HTTPS call to the resource (here, Key Vault) using that binding certificate.
- No app registration, no client secret, no certificate to rotate.

## Prerequisites

- Azure VM, Arc-enabled server, or any host that exposes the V2 managed identity credential
  endpoint (returns a binding certificate alongside the access token).
- MI principal granted **Key Vault Secrets User** (or higher) on the target vault.

## Run

```bash
dotnet run
```

Expected output: `Secret retrieved successfully via MSI mTLS PoP.`

## Switching to system-assigned MI

Remove the `UserAssignedClientId` field from `appsettings.json`:

```json
"AcquireTokenOptions": {
    "ManagedIdentity": { }
}
```

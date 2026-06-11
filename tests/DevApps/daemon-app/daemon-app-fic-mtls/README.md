# daemon-app-fic-mtls

Daemon (console) sample that calls Azure Key Vault using **Federated Identity Credentials (FIC)
backed by Managed Identity**, with the resulting app token bound to the MI's binding certificate
via **mTLS Proof-of-Possession** (PoP).

## What this demonstrates

Two-leg flow:

1. **Leg 1 — Managed Identity** (`SignedAssertionFromManagedIdentity`)
   - V2 managed identity credential endpoint returns a binding certificate + a signed assertion
     whose audience is `api://AzureADTokenExchange`.
2. **Leg 2 — Confidential Client → Entra ID**
   - Microsoft.Identity.Web sends that assertion to the application's tenant and asks for the
     downstream resource (Key Vault) access token.
   - `ProtocolScheme = "MTLS_POP"` pins the outer token to the same binding certificate, so the
     downstream HTTPS call to Key Vault uses mTLS.

No app secret, no rotatable certificate.

## Prerequisites

- App registration (`AzureAd:ClientId`) with a **Federated Identity Credential** that trusts the
  managed identity in `AzureAd:ClientCredentials[0].ManagedIdentityClientId`.
- The app registration must be granted **Key Vault Secrets User** (or higher) on the target vault.
- Host must expose the V2 managed identity credential endpoint (Azure VM, Arc-enabled server, etc.).

## Run

```bash
dotnet run
```

Expected output: `Secret retrieved successfully via FIC + mTLS PoP.`

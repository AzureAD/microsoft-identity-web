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

- Supported Windows IMDSv2 host with Credential Guard/KeyGuard available for attestation.
- MI principal granted **Key Vault Secrets User** (or higher) on the target vault.
- Key attestation is required by default for managed identity mTLS acquisition. Reference
  `Microsoft.Identity.Web.KeyAttestation` and register
  `services.AddMicrosoftIdentityWebKeyAttestation()`, or explicitly set
  `"KeyAttestation": "Disabled"` inside `AcquireTokenOptions` to opt out.
  This sample already references and registers the attestation provider.

## Run

```bash
dotnet run
```

Expected output: `Secret retrieved successfully via MSI mTLS PoP.`

## Switching to system-assigned MI

Set `UserAssignedClientId` to an empty value so configuration binding constructs the
managed identity options:

```json
"AcquireTokenOptions": {
    "ManagedIdentity": { "UserAssignedClientId": "" }
}
```

## Acquire a Bearer token over mTLS

To use mTLS for managed identity token acquisition without binding the returned token,
use `Bearer` instead of `MTLS_POP` and set the typed `RequestOverMtls` option:

```json
{
  "ProtocolScheme": "Bearer",
  "RequestAppToken": true,
  "AcquireTokenOptions": {
    "ManagedIdentity": { "UserAssignedClientId": "" },
    "RequestOverMtls": true
  }
}
```

Keep `UserAssignedClientId` empty for system-assigned identity, or set it to the
user-assigned identity's client ID. Include the field so configuration binding creates
the managed identity options.
This requires a supported Windows IMDSv2 host. Attestation defaults to `Required` for
both bearer-over-mTLS and mTLS PoP. Missing registration or an attestation failure
fails acquisition instead of falling back to an unattested request.

For either mode, explicitly add `"KeyAttestation": "Disabled"` to skip attestation,
even when the attestation provider is registered. This does not relax MSAL's platform
or the service's policy requirements.

The result is a standard Bearer token with no binding certificate; the downstream API
call does not use the managed identity certificate. `RequestOverMtls` and `MTLS_POP`
are mutually exclusive. Ordinary bearer acquisition remains unchanged when the flag
is omitted or false. Attested and unattested mTLS requests use separate token-cache
partitions.

These typed options depend on Abstractions PR #279 and its unreleased 12.8.1 package.
The normal restore/build remains blocked until that contract is published.

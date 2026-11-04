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
- This sample explicitly sets `AcquireTokenOptions.MinimumTokenBindingStrength` to `KeyGuard`.
  That requirement needs the `Microsoft.Identity.Web.KeyAttestation` package and
  `services.AddMicrosoftIdentityWebKeyAttestation()`. The sample already references and
  registers the provider. Missing support, insufficient host strength, or attestation failure
  fails acquisition without a weaker-token fallback.

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
    "ManagedIdentity": { "UserAssignedClientId": "" },
    "MinimumTokenBindingStrength": "KeyGuard"
}
```

## Choose a minimum PoP binding strength

The shared `MtlsBindingStrength` enum follows MSAL: `None = 0`, `Software = 1`, and
`KeyGuard = 3` (`2` is reserved). The option is a minimum, not a key selector:

- Omit `MinimumTokenBindingStrength` or use `None` for no minimum requirement.
- Use `Software` to require at least software-backed binding.
- Use `KeyGuard` to require KeyGuard capability and key attestation, as this sample does.

Lowering the minimum does not disable a registered attestation provider or force weaker keys.
There is no required-attestation default and no separate per-request attestation-disable switch.
`MinimumTokenBindingStrength` applies only to direct managed identity `MTLS_POP`.

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
This requires a supported Windows IMDSv2 host. Remove the sample's
`MinimumTokenBindingStrength: KeyGuard` setting when switching to this configuration:
bearer tokens have no token binding strength, so a non-`None` minimum is rejected.
The sample's registered attestation provider is still used for the mTLS acquisition.
Without registration, IdWeb does not add attestation; MSAL's platform and service-policy
requirements still apply. Provider failures are propagated, not retried without attestation.

The result is a standard Bearer token with no binding certificate; the downstream API
call does not use the managed identity certificate. `RequestOverMtls` and `MTLS_POP`
are mutually exclusive. Ordinary bearer acquisition remains unchanged when the flag
is omitted or false. MSAL separates token-cache entries by mTLS mode, attestation
support, and the requested PoP minimum strength.

These typed options depend on Abstractions PR #279 and its unreleased 12.8.1 package.
The normal restore/build remains blocked until that contract is published.

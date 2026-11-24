# Microsoft.Identity.Web.NativeAOT

> Note: Microsoft.Identity.Web Native AOT is under construction and not ready
> for production consumption yet. API and configuration are for informational
> purposes only.

A native shared library for C and C++ applications to acquire authorization
headers and call downstream APIs using Microsoft.Identity.Web and MSAL.
An installed .NET runtime is not required; the library includes compiled runtime
code and still requires the target OS's TLS, cryptography and ICU dependencies.

## Getting started

1. Deploy the library and dependencies for the host OS and architecture.
2. Include [`EntraAuth.h`](include/EntraAuth.h) and link or load the library.
3. Call `EntraAuthConfigure` with UTF-8 JSON; a `NULL` section selects `AzureAd`.
4. Request a header or HTTP response and free the complete returned result.

```json
{
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "TenantId": "<tenant-id>",
    "ClientId": "<application-client-id>",
    "ClientCredentials": [
      {
        "SourceType": "KeyVault",
        "KeyVaultUrl": "https://<vault-name>.vault.azure.net",
        "KeyVaultCertificateName": "<certificate-name>"
      }
    ]
  },
  "DownstreamApis": {
    "Graph": {
      "BaseUrl": "https://graph.microsoft.com/v1.0/",
      "RelativePath": "users",
      "Scopes": ["https://graph.microsoft.com/.default"],
      "RequestAppToken": true
    }
  }
}
```

Grant the host identity access to the Key Vault certificate and its backing
secret. Key Vault authentication follows the managed credential loader's
environment requirements. Do not commit private keys or credentials to source
control.
Configuration success does not establish that credentials or endpoints will
accept an acquisition.

```cpp
EntraAuthAuthorizationHeaderInput input{};
EntraAuth::ResultPtr<EntraAuthAuthorizationHeaderResult> result(
    EntraAuthGetAuthorizationHeaderUnauthenticated(&input, "Graph"));
if (result->token.errorCategory == ENTRA_AUTH_ERROR_NONE)
{
    // Use result->authorizationHeader and token expiration before result is freed.
}
```

## API and ownership

The header defines the complete C ABI, field limits and ownership contracts.
Use matching headers and binaries, default compiler packing and 32-bit enum
storage. Zero-initialize every complete input. No runtime ABI negotiation is
provided.

| Operation | Synchronous export |
|---|---|
| Header with caller-supplied user context | `EntraAuthGetAuthorizationHeader` |
| Header without inbound context | `EntraAuthGetAuthorizationHeaderUnauthenticated` |
| HTTP with caller-supplied user context | `EntraAuthCallDownstreamApi` |
| HTTP without inbound context | `EntraAuthCallDownstreamApiUnauthenticated` |

Each operation also has an `Async` export. **Unauthenticated means no inbound
user context**, not necessarily an anonymous outbound request. This library
does not validate inbound tokens, authorize callers or provide interactive UI.
The host must authenticate and authorize the caller before supplying a context;
JWT parsing does not validate its signature, issuer, audience or expiration.

Strings are zero-terminated UTF-8; bodies are length-delimited binary data.
Input pointers remain readable until synchronous return or async submission
return. Async submission deep-copies inputs.

Each result owns its strings and bytes in one allocation. Free the complete
result with the matching export:

| Result | Free export |
|---|---|
| Authorization header | `EntraAuthFreeAuthorizationHeaderResult` |
| Downstream HTTP | `EntraAuthFreeDownstreamApiResult` |
| Async submission | `EntraAuthFreeAsyncSubmissionResult` |
| Configuration/control | `EntraAuthFreeControlResult` |
| Log message | `EntraAuthFreeLoggingCallbackContext` |

These exports wipe the allocation. Alternatively, `CoTaskMemFree` on Windows,
`free` on Linux or C++ `EntraAuth::ResultPtr<T>` releases it **without wiping**.
Never free embedded pointers or a token prefix separately. Returned Windows
binding certificates have independent ownership, described below.

Rejected async submissions have no operation ID and never invoke completion.
Accepted submissions invoke completion exactly once, possibly inline before
submission returns. Free both the submission and completion result. Callbacks
can run concurrently, have no thread affinity, may reenter APIs, and must not
throw or block indefinitely. Tracking retires automatically after callback
return. `EntraAuthCancelOperation` is best-effort, not a callback-drain barrier.

Recoverable failures return owned error results; managed exceptions do not
unwind through the ABI. If allocation or callback delivery makes safe reporting
impossible, the library terminates the process. Invalid pointers and double
frees are not recoverable input errors.

## Configuration and acquisition

JSON is flattened into `IConfiguration` keys and bound to managed IdWeb options.
Objects use colon-delimited paths, arrays use numeric indexes, JSON `null` values
become empty strings, and duplicate properties use the last value. Unknown
settings follow managed binding semantics. Arbitrary managed plugin activation
is not supported under AOT.

Configure additional applications as root sections with credentials and
`Instance` or `Authority`, or under `Applications`. `Name`, when supplied on a
root application, is its selector; otherwise use the section key. Set typed
`overrides.authenticationOptionsName` to select it. Names are
case-insensitive and must be unique; unknown names fail.
Applications sharing the same managed authority/client/region identity share
credentials and cache partitions. Names alone do not isolate credentials.

Header and HTTP inputs each contain a top-level `correlationId` and shared,
embedded `overrides`. `EntraAuthDownstreamApiOptionsOverrides` and
`EntraAuthAuthorizationHeaderOptionsOverrides` are C aliases for the same
layout; their .NET counterpart is `NativeDownstreamApiOptionsOverrides`.
Header inputs are not nested in HTTP inputs. HTTP-specific `contentType`,
`body`, `bodyLength`, `acceptHeader`, `relativePath` and `method` are top-level
fields of `EntraAuthDownstreamApiInput`. Copying overrides copies values and
borrowed pointers, not the memory referenced by those pointers.

Named `DownstreamApis` entries use managed `DownstreamApiOptions`, including
`AcquireTokenOptions` defaults. Request values override those defaults;
parameter dictionaries replace rather than merge. In the typed ABI, NULL/0
inherits a dictionary and a non-NULL entries pointer with count zero clears it.
JSON `null` inherits defaults; an empty JSON object does not explicitly clear a
dictionary. `overrides.forceRefresh` always overrides the configured value,
including false. Explicit tenant/claims fields take precedence over acquisition
defaults. Native `overrides.claims` is URL-decoded once; encode literal `+` and `%`.

Header requests can omit the service name when explicit scopes are supplied.
HTTP requests require a named service with an HTTPS `BaseUrl`. NULL scopes/count
zero inherit service scopes; a non-NULL pointer/count zero explicitly clears
them. Application acquisition requires exactly one `resource/.default` scope.

`RequestAppToken` selects application or user acquisition. When absent, a user
assertion, cached-user selector or long-running OBO session selects user
acquisition; otherwise application acquisition is used. Managed identity is
application-only. A user flow without user context fails rather than falling
back to an application token. HTTP services with no effective scopes are
anonymous and do not acquire a token.

Typed `EntraAuthDownstreamApiOptionsOverrides` supports named application
selection, B2C user flow, cached users, username/password, token-endpoint
parameters, managed identity, PoP, agent identity, FMI and request-local
credentials. Additional HTTP settings are nested in
`overrides.downstreamApiOptions`; header-only calls reject them.
Supplied nested boolean fields explicitly override defaults, including false.
Unless qualified otherwise, the request fields below belong to `input.overrides`.

| Scenario | Configuration or request setting |
|---|---|
| OBO | Supply an authenticated caller's compact JWT in `EntraAuthRequestContext.userAssertion`. |
| Cached user | Set `homeAccountId` to the complete returned account ID. Unavailable renewal returns `InteractionRequired`. |
| Username/password | Supply both fields; this legacy flow does not support MFA or interactive sign-in. |
| Long-running OBO | Start with a user assertion and `longRunningWebApiSessionKey`; `"AllocateForMe"` generates a key. Resume using the returned key without an assertion. |
| Pure managed identity | Configure `ManagedIdentity: {}` on the selected application or supply typed `managedIdentity`; set JSON `ManagedIdentity.UserAssignedClientId` or native `managedIdentity->userAssignedClientId` for a user-assigned identity. |
| Request-local client | Set `clientId` and exactly one of `clientCertificate` (base64 private-key container) or `clientAssertion`; optional `azureRegion`. |
| Agent | Set `agentIdentity->clientId` and optionally one user selector, `userPrincipalName` or `userObjectId`. Select the blueprint through `authenticationOptionsName`. |
| Assertion FMI | Set `clientAssertionFmiPath`; JSON defaults use `ExtraParameters.IDWEB_FMI_PATH_FOR_CLIENT_ASSERTION`. `fmiPath` separately selects the acquired token's path. |

Request-local clients have isolated managed credential graphs and caches.
At most 128 distinct request configurations are retained per environment;
reconfigure to retire them. Signed assertions remain request-local and are
not part of graph identity. User-token acquisition is serialized per canonical
managed application to protect cache partitions; app acquisition and downstream
HTTP remain independent.

### Credentials and caching

`ClientCredentials` uses ordered managed `CredentialDescription` entries.
Supported sources include `ClientSecret`, `Path`, `Base64Encoded`,
`StoreWithThumbprint`, `StoreWithSubjectName`, `StoreWithDistinguishedName`,
`KeyVault`, `SignedAssertionFromManagedIdentity` and `SignedAssertionFilePath`.
The managed loader selects a loadable credential; this does not promise retry
with another credential after endpoint rejection.

`CustomSignedAssertion` supports these statically registered provider names:

| Provider | Settings |
|---|---|
| `NativeSuppliedClientAssertion` | `CustomSignedAssertionProviderData.SignedAssertion` contains a host-signed JWT. |
| `NativePerRequestClientAssertion` | Supply `overrides.clientAssertion` on every acquisition. |
| `OidcIdpSignedAssertion` | `CustomSignedAssertionProviderData.ConfigurationSection` identifies the bootstrap application section. |

Assertions require a signed, non-encrypted compact JWT with future `exp`.
Per-request assertions must match the selected client ID, are copied before
async return and are rechecked before use. The identity provider validates
signatures. The host owns renewal; assertion rotation alone does not bypass the
access-token cache. Certificate loading is lazy and follows managed ownership.

OIDC federation uses managed `AddOidcFic` behavior, including bootstrap warm-up,
FMI, `TokenExchangeUrl` and root `CloudMetadata` audience overrides. Provision
federation trust separately. Root `KeyAttestation.Enabled: true` registers the
managed key-attestation provider; its OS, Credential Guard and environment
requirements still apply.

Only process-local caching is configured. Optional root `TokenCache.Memory`
settings bind to the managed cache options:

```json
{
  "TokenCache": {
    "Memory": {
      "AbsoluteExpirationRelativeToNow": "14.00:00:00",
      "SizeLimit": 536870912,
      "CompactionPercentage": 0.05,
      "ExpirationScanFrequency": "00:01:00"
    }
  }
}
```

`SizeLimit` is measured in serialized-cache bytes; zero prevents retention.
These settings do not configure MSAL's managed-identity cache. There are no
native cache callbacks, distributed-cache providers, account-removal exports
or raw refresh-token import/export.

### PoP, mTLS and downstream HTTP

| Mode | Selection and ownership |
|---|---|
| Bearer | Default token/header mode. |
| mTLS-bound token | `overrides.protocolScheme = "MTLS_POP"` for application headers or HTTP. Requires an actual bound token and usable private-key certificate; no ordinary HTTP fallback. |
| Certificate-only mTLS | HTTP `overrides.protocolScheme = "MTLS"` uses the selected application's certificate without acquiring a token. |
| Externally bound PoP | Set `popPublicKey`; the host owns the proof key. App acquisition with `popClaim` selects MSAuth/AtPoP and uses a Bearer authorization prefix. |
| SDK signed request | Supply `poPConfiguration` with `httpMethod`, `httpHost`, `httpPath`, `nonce` and `signHttpRequest = true`. Bind it to the actual outgoing request. |

Windows `MTLS_POP` header results return a duplicated `PCCERT_CONTEXT`.
It outlives the result and must be freed independently with
`CertFreeCertificateContext` after TLS use. Other platforms return no native
certificate context; use downstream exports for SDK-owned mTLS HTTP.
Certificate credentials can also use managed `UseBoundCredential` to authenticate
at the token endpoint while acquiring a Bearer token.

HTTP request overrides select method, relative path, Accept, content type,
binary body, extra headers and query parameters. The initial destination must
remain on the configured HTTPS origin. Framework redirects can reach another
HTTPS origin and clear Authorization. Reserved request headers follow managed
IdWeb filtering; handlers retain their default cookie behavior.

Managed downstream processing handles recognized 401 claims challenges and
replays the copied request body. Signed-request nonce challenges require the
caller to inspect `wwwAuthenticate` and retry with the nonce. Set
`GraphAuthentication` on a service or
`overrides.downstreamApiOptions->graphAuthentication` to use IdWeb's
Graph provider. Only its allowed Graph hosts receive a token; other HTTPS hosts
are still called anonymously. Hosts own Graph JSON serialization.

One deadline covers acquisition and HTTP processing:
`overrides.timeoutMilliseconds` zero selects 60 seconds,
with a maximum of 600000 milliseconds. Request bodies are limited to 1 MiB;
ordinary response bodies to 16 MiB and managed mTLS buffering to 1 MiB.
Response metadata contains multi-valued headers and trailers.

Managed forwarding limitations remain: OBO/password paths do not apply
request-local extra body parameters or the per-request client-assertion override.
Configured credentials remain available. The password path does not forward
cancellation to MSAL. Agent exchanges need not share one correlation ID.
No native fallback changes these behaviors.

## Results, diagnostics and lifetime

Use `token.errorCategory` and structured provider fields for decisions.
Successful results contain authoritative expiration and acquisition metadata;
HTTP results also contain status, binary body, media type and `WWW-Authenticate`.
Non-2xx HTTP responses retain the body and acquisition metadata with
`ENTRA_AUTH_ERROR_DOWNSTREAM_API`.

`tokenEndpointHttpStatus` is available only for an observed token-endpoint
response. Cache hits, local failures and SDK-owned mTLS acquisition can leave it
unavailable; success does not imply an observed HTTP 200. Downstream status is
separate. Use acquisition `tokenSource` metadata to distinguish fresh/cache
results.

Error descriptions, metadata and response bodies may contain sensitive data;
do not log them automatically. The process-wide log callback emits fixed facade
diagnostics and forwards managed IdWeb/MSAL/HttpClient messages, preserving
managed text/redaction and including category/event IDs. SDK events use standard
`Logging:LogLevel` configuration and category filters (default: Information).
Existing managed PII settings are passed through unchanged; there are no
native-specific logging switches or additional redaction rules.
SDK text can expose identifiers, URLs and provider diagnostics; use it only
in trusted diagnostic environments.
Free every delivered log message.
`EntraAuthUnassignLogMessageCallback` must drain successfully before callback
code or context is released.

Configuration replacement is transactional: failed replacement preserves the
current environment. Successful replacement publishes a new environment/cache
and disposes the previous host after **15 minutes**, not after exact operation
draining. Operations exceeding that delay can lose resources. Managed mTLS
clients also use a shared certificate pool whose disposal can affect other hosts.

Keep the initialized library loaded for the process lifetime: **never call
`FreeLibrary` or `dlclose` on it**. There is no shutdown/drain export. Before
retiring host callback state, stop submissions and wait for every completion
callback to finish; cancellation alone is insufficient. Unregister logging
separately. Service or roll back the library by restarting the process with a
complete, version-matched distribution, not by hot replacement.

## Building and verification

From the repository root, using the repository-configured package sources:

```powershell
dotnet publish .\src\Microsoft.Identity.Web.NativeAOT\Microsoft.Identity.Web.NativeAOT.csproj -c Release -f net10.0 -r win-x64 -p:TargetFrameworks=net10.0
```

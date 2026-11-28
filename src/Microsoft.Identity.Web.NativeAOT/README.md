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
EntraAuthDownstreamAuthorizationHeaderInput input{};
EntraAuth::ResultPtr<EntraAuthDownstreamAuthorizationHeaderResult> result(
    EntraAuthGetAuthorizationHeaderUnauthenticated(&input, "Graph"));
if (result->httpResponseStatusCode == 200 && result->authorizationHeader != NULL)
{
    // Use result->authorizationHeader before result is freed.
}
```

## API and ownership

The header defines the complete C ABI, field limits and ownership contracts.
Use matching headers and binaries, default compiler packing and 32-bit enum
storage. Zero-initialize every complete input. No runtime ABI negotiation is
provided.

Unused structure positions are reserved; leave every reserved pointer NULL
and reserved count zero.

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

Strings, request bodies and response bodies are zero-terminated UTF-8.
`inputBody` and `downstreamApiResult` cannot contain embedded zero bytes;
NULL omits the request body or indicates an unavailable response body.
Input pointers remain readable until synchronous return or async submission
return. Async submission deep-copies inputs.

Exported functions use `__stdcall` on Windows and the platform default calling
convention elsewhere. Callbacks use the platform default calling convention.

Each result owns its strings and bytes in one allocation. Free the complete
result with the matching export. Synchronous exports return read-only
`const Result*` pointers; free exports accept these pointers without casting:

| Result | Free export |
|---|---|
| Authorization header | `EntraAuthFreeDownstreamAuthorizationHeaderResult` |
| Downstream HTTP | `EntraAuthFreeDownstreamApiResult` |
| Configuration/control | `EntraAuthFreeControlResult` |
| Log message | `EntraAuthFreeLogMessageCallbackContext` |

Prefer the matching free exports for SDK-managed cleanup. Alternatively,
`CoTaskMemFree` on Windows, `free` on Linux or C++ `EntraAuth::ResultPtr<T>`
releases it **without wiping**.
Never free embedded pointers separately. Returned Windows
binding certificates have independent ownership, described below.

`ResultPtr<T>` is a const, noncopyable and nonmovable owner. Construct it
directly from a returned result pointer and let it release the allocation at
scope exit; its const qualification prevents `reset()` and `release()`.

Async exports return `void` and invoke completion exactly once on
success or failure with `(result, callerContext)`, possibly inline before
the export returns. Free the completion result with the same free export used
for synchronous results. Callbacks can run concurrently, have no thread affinity,
may reenter APIs, and must not throw or block indefinitely.

Recoverable failures return owned error results; managed exceptions do not
unwind through the ABI. If allocation or callback delivery makes safe reporting
impossible, the library terminates the process. Invalid pointers and double
frees are not recoverable input errors.

Control results use integer `errorCode` values, not an enum:
0 for success, 100 for configuration errors and 200 for logging callback
errors. These codes are separate from operation HTTP status codes.
A failed logging drain does not permit releasing callback
code or context.

## Configuration and acquisition

Configuration follows managed IdWeb options. Use `AzureAd` for the selected
application and `DownstreamApis` for named downstream services. Arbitrary
managed plugin activation is not supported under AOT.

Configure additional applications as root sections with credentials and
`Instance` or `Authority`, or under `Applications`. `Name`, when supplied on a
root application, is its selector; otherwise use the section key. Set the named
service's `AcquireTokenOptions.AuthenticationOptionsName` to select it. Names are
case-insensitive and must be unique; unknown names fail.

Header and HTTP inputs each contain a top-level `correlationId` and shared,
embedded `overrides`. `EntraAuthDownstreamApiOptionsOverrides` and
`EntraAuthAuthorizationHeaderOptionsOverrides` are C aliases for the same
layout.
Header inputs are not nested in HTTP inputs. HTTP-specific `contentType`,
`inputBody` and `acceptHeader` are top-level fields of
`EntraAuthDownstreamApiInput`; the HTTP path is `overrides.relativePath`.
The HTTP method is configured through the named service's `HttpMethod`.
Copying overrides copies values and borrowed pointers, not the memory
referenced by those pointers.

`EntraAuthValidationResult` is a borrowed host-supplied exchange context. Its
first six fields, in order, are `httpResponseStatusCode`, `correlationId`,
`errorDescription`, `wwwAuthenticate`, `subjectToken` and `subjectTokenType`.
Its `reserved1` through `reserved16` are unused layout placeholders.
The overrides likewise contain `reserved1` through `reserved8`. All reserved
fields are ignored placeholders, not supported options or validation data.
The facade exchanges `subjectToken` but does not perform inbound token
validation or produce owned validation results. The host remains responsible
for authenticating and authorizing the caller.

Named `DownstreamApis` entries use managed `DownstreamApiOptions`, including
`AcquireTokenOptions` defaults. The shared native overrides provide scopes,
tenant, claims, request-local client credentials, region, FMI, cache refresh,
relative path, long-running OBO session key and client assertion claims.
Where supported by managed options, additional acquisition and HTTP settings
are configured on named services, not supplied through extra native request
structures.
`overrides.forceRefresh` remains a four-byte C integer and must be 0 (permit
cached tokens) or 1 (force refresh), always overriding the configured value.
Explicit tenant/claims fields take precedence over acquisition defaults.
Native `overrides.claimsJson` is URL-decoded once; encode literal `+` and `%`.

Header requests can omit the service name when explicit scopes are supplied.
HTTP requests require a named service with an HTTPS `BaseUrl`. NULL scopes/count
zero inherit service scopes; a non-NULL pointer/count zero explicitly clears
them. Application acquisition requires exactly one `resource/.default` scope.

`RequestAppToken` selects application or user acquisition. When absent, a user
assertion or long-running OBO session selects user
acquisition; otherwise application acquisition is used. Managed identity is
application-only. A user flow without user context fails rather than falling
back to an application token. HTTP services with no effective scopes are
anonymous and do not acquire a token.

Unless qualified otherwise, the request fields below belong to `input.overrides`.

| Scenario | Configuration or request setting |
|---|---|
| OBO | Supply an authenticated caller's compact JWT in `EntraAuthValidationResult.subjectToken`. Independently supply `overrides.signedClientAssertion` when the middle tier authenticates with a caller-signed client assertion. |
| Long-running OBO | Start with a user assertion and `longRunningWebApiSessionKey`; `"AllocateForMe"` generates a key. Resume using the returned key without an assertion. |
| Pure managed identity | Configure the named service's `AcquireTokenOptions.ManagedIdentity`; set `UserAssignedClientId` for a user-assigned identity. |
| Request-local client | Set `managedIdentityClientId` and exactly one of `managedIdentityBase64encodedCertificate` (base64 private-key container) or `signedClientAssertion`; optional `azureRegion`. |
| Assertion FMI | JSON defaults use `ExtraParameters.IDWEB_FMI_PATH_FOR_CLIENT_ASSERTION`. `fmiPath` separately selects the acquired token's path. |

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
| `NativePerRequestClientAssertion` | Supply `overrides.signedClientAssertion` on every acquisition. |
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
| mTLS-bound token | Configure `ProtocolScheme = "MTLS_POP"` on the named service for application headers or HTTP. Requires an actual bound token and usable private-key certificate; no ordinary HTTP fallback. |
| Certificate-only mTLS | Configure `ProtocolScheme = "MTLS"` on the named HTTP service to use the selected application's certificate without acquiring a token. |

The native request ABI does not expose per-request protocol or PoP signing
settings.

Windows `MTLS_POP` header results return a duplicated `PCCERT_CONTEXT`.
It outlives the result and must be freed independently with
`CertFreeCertificateContext` after TLS use. Other platforms return no native
certificate context; use downstream exports for SDK-owned mTLS HTTP.
Certificate credentials can also use managed `UseBoundCredential` to authenticate
at the token endpoint while acquiring a Bearer token.

HTTP request inputs select relative path, Accept, content type and UTF-8 text
body. Method, extra headers and query parameters are configured on the named
service. The initial destination must
remain on the configured HTTPS origin. Framework redirects can reach another
HTTPS origin and clear Authorization. Reserved request headers follow managed
IdWeb filtering; handlers retain their default cookie behavior.

Managed downstream processing handles recognized 401 claims challenges and
replays the copied request body. Use `wwwAuthenticate` to forward a claims
challenge to the caller. Set `GraphAuthentication` on a service to use IdWeb's
Graph provider. Only its allowed Graph hosts receive a token; other HTTPS hosts
are still called anonymously. Hosts own Graph JSON serialization.

Request bodies are limited to 1 MiB;
ordinary response bodies to 16 MiB and managed mTLS buffering to 1 MiB.

OBO supports a caller-supplied signed client assertion for middle-tier
authentication independently of the inbound user assertion being exchanged.
`overrides.signedClientAssertion` authenticates the application and does not replace
`validationResult.subjectToken`; configured application credentials can also be used.

## Results, diagnostics and lifetime

Both result types expose `httpResponseStatusCode`, `correlationId`,
`errorDescription` and `wwwAuthenticate` at the top level. Header results then
contain `authorizationHeader`, `bindingCertificate`,
`longRunningWebApiSessionKey`, `metadata` and `failureDetails`. HTTP results
contain `downstreamApiResult`, the zero-terminated UTF-8 response body.
No additional typed token or HTTP diagnostics are embedded in these results.
Header `metadata` and `failureDetails` are optional diagnostic JSON; their
formats are not contractual and must not be used for programmatic decisions.
`failureDetails` is NULL on success or when unavailable.

Use `httpResponseStatusCode` for the operation outcome and `errorDescription`
for diagnostic messages. A failure status can be returned before any network
response; it does not establish that a token-endpoint or downstream HTTP
response was observed.
Both header and HTTP results expose `wwwAuthenticate` for forwarding an
acquisition challenge upstream. On an HTTP result, an acquisition challenge
takes precedence; otherwise `wwwAuthenticate` contains the downstream header.
HTTP results retain the zero-terminated UTF-8 response body when available,
including for non-2xx responses. They do not expose a response media type,
additional response metadata or a separate observed HTTP-status field.

Error descriptions, metadata and response bodies may contain sensitive data;
do not log them automatically. The process-wide log callback emits fixed facade
diagnostics and forwards managed IdWeb/MSAL/HttpClient messages, preserving
managed text/redaction and including category/event IDs. SDK events use standard
`Logging:LogLevel` configuration and category filters (default: Information).
Existing managed PII settings are passed through unchanged; there are no
native-specific logging switches or additional redaction rules.
SDK text can expose identifiers, URLs and provider diagnostics; use it only
in trusted diagnostic environments.
`EntraAuthLogMessageCallbackContext` contains `message` followed by `level`.
C severity names are `EntraAuthLogLevelTrace` through `EntraAuthLogLevelNone`,
with values 0 through 6.
Free every delivered log message.
`EntraAuthUnassignLogMessageCallback()` must drain successfully before callback
code or context is released.

Keep the initialized library loaded for the process lifetime: **never call
`FreeLibrary` or `dlclose` on it**. There is no shutdown/drain export. Before
retiring host callback state, stop submissions and wait for every completion
callback to finish. Unregister logging
separately. Service or roll back the library by restarting the process with a
complete, version-matched distribution, not by hot replacement.

## Building and verification

From the repository root, using the repository-configured package sources:

```powershell
dotnet publish .\src\Microsoft.Identity.Web.NativeAOT\Microsoft.Identity.Web.NativeAOT.csproj -c Release -f net10.0 -r win-x64 -p:TargetFrameworks=net10.0
```

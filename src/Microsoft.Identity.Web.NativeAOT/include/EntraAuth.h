/* Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. */

#ifndef ENTRA_AUTH_H
#define ENTRA_AUTH_H

/**
 * @file
 * Each complete control, log, header, HTTP or submission result is one allocation containing all its data.
 * Its returned pointer is the allocation base: CoTaskMemFree on Windows or free on Linux can release it directly.
 * Direct system deallocation and EntraAuth::ResultPtr (C++) do not wipe secrets; matching SDK free exports do.
 * Never free an embedded token prefix, string or body separately, or assume payload immediately follows the struct.
 * Use the matching SDK header and library; there is no ABI negotiation or structure-version detection.
 * Initialize every input field; {0} in C or {} in C++ supplies zero values and NULL pointers.
 * SDK/package versions identify the distributed implementation.
 * Supported targets use the compiler's default enum storage and structure packing.
 * Managed exceptions never unwind through the C ABI; recoverable failures return owned error results.
 * If an error result cannot be produced safely or completion delivery fails, the process terminates.
 * Callbacks must not throw. Invalid pointers, double frees and fatal runtime failures are not recoverable API errors.
 */

#include <stdint.h>

#ifndef __cplusplus
#include <stdbool.h>
#endif

#if defined(_WIN32) && !defined(ENTRA_AUTH_DYNAMIC_LOADING)
/** Imports an export from the Windows library; define ENTRA_AUTH_DYNAMIC_LOADING for manual symbol loading. */
#define ENTRA_AUTH_API __declspec(dllimport)
#else
/** Declares an export without Windows import decoration for dynamic loading or non-Windows targets. */
#define ENTRA_AUTH_API
#endif

#ifdef __cplusplus
extern "C" {
#endif

/** Severity values matching Microsoft.Extensions.Logging.LogLevel; declaring a level does not enable its emission. */
typedef enum EntraAuthLogLevel
{
    ENTRA_AUTH_LOG_TRACE = 0, /**< The most detailed diagnostic severity. */
    ENTRA_AUTH_LOG_DEBUG = 1, /**< Diagnostic information intended for development and investigation. */
    ENTRA_AUTH_LOG_INFORMATION = 2, /**< Normal configuration and operation outcomes. */
    ENTRA_AUTH_LOG_WARNING = 3, /**< An abnormal event that does not stop the current operation. */
    ENTRA_AUTH_LOG_ERROR = 4, /**< A failure of the current configuration or operation. */
    ENTRA_AUTH_LOG_CRITICAL = 5, /**< A catastrophic failure requiring immediate attention. */
    ENTRA_AUTH_LOG_NONE = 6 /**< Filtering sentinel; never an emitted severity. */
} EntraAuthLogLevel;

/** Stable operation/submission failure categories; descriptions are explanatory text, not additional error codes. */
typedef enum EntraAuthErrorCategory
{
    ENTRA_AUTH_ERROR_NONE = 0, /**< The operation or submission succeeded. */
    ENTRA_AUTH_ERROR_CONFIGURATION = 1, /**< Acquisition configuration or SDK client initialization is invalid. */
    ENTRA_AUTH_ERROR_CREDENTIAL = 2, /**< The configured credential could not be loaded or used. */
    ENTRA_AUTH_ERROR_TRANSPORT = 3, /**< Authority, token-endpoint or downstream communication failed. */
    ENTRA_AUTH_ERROR_TOKEN_ENDPOINT = 4, /**< An observed token-endpoint response rejected the request. */
    ENTRA_AUTH_ERROR_INTERACTION_REQUIRED = 5, /**< The host must handle required interaction or a claims challenge. */
    ENTRA_AUTH_ERROR_CANCELLED = 6, /**< Cancellation or the operation deadline stopped the request. */
    ENTRA_AUTH_ERROR_UNSUPPORTED = 7, /**< The requested protocol or operation is unsupported. */
    ENTRA_AUTH_ERROR_INTERNAL = 8, /**< An unexpected recoverable internal failure occurred. */
    ENTRA_AUTH_ERROR_NOT_CONFIGURED = 9, /**< No successful configuration is available. */
    ENTRA_AUTH_ERROR_INVALID_REQUEST = 10, /**< An input value, envelope or required context is invalid. */
    ENTRA_AUTH_ERROR_DOWNSTREAM_API = 11 /**< The downstream API returned a non-2xx response; inspect its separate status/body. */
} EntraAuthErrorCategory;

/** Result codes for configuration, logging registration and operation controls. */
typedef enum EntraAuthControlError
{
    ENTRA_AUTH_CONTROL_SUCCESS = 0, /**< The control operation succeeded. */
    ENTRA_AUTH_CONTROL_INVALID_OPERATION = 1, /**< Invalid input, configuration, operation ID or registration state. */
    ENTRA_AUTH_CONTROL_INTERNAL = 2 /**< An internal control failure occurred; a failed drain does not permit retiring callbacks. */
} EntraAuthControlError;

/** Owned control outcome; free the complete allocation with EntraAuthFreeControlResult. */
typedef struct EntraAuthControlResult
{
    EntraAuthControlError errorCode; /**< A control result code; ENTRA_AUTH_CONTROL_SUCCESS indicates success. */
    const char* errorDescription; /**< Owned redacted UTF-8 explanation, or NULL; valid until the result is freed. */
} EntraAuthControlResult;

/** Owned log message transferred to a callback; free with EntraAuthFreeLoggingCallbackContext. */
typedef struct EntraAuthLoggingCallbackContext
{
    const char* message; /**< Owned zero-terminated UTF-8 text containing only redacted facade diagnostics. */
    EntraAuthLogLevel level; /**< Diagnostic severity; ENTRA_AUTH_LOG_NONE is never emitted. */
} EntraAuthLoggingCallbackContext;

/**
 * Host-supplied exchange context, not proof of inbound authentication.
 * The host authenticates/authorizes the caller; the SDK only parses this JWT,
 * without validating its signature, issuer, audience or expiration, or decrypting it.
 * Borrowed inputs must remain readable until sync return or async submission return.
 */
typedef struct EntraAuthRequestContext
{
    const char* userAssertion; /**< Required zero-terminated UTF-8 compact JWT, at most 1 MiB; copied on submission. */
    const char* correlationId; /**< Optional preceding host correlation GUID, at most 128 UTF-8 bytes; copied on submission. */
} EntraAuthRequestContext;

/** Borrowed UTF-8 parameter name and value; neither may be NULL. */
typedef struct EntraAuthStringPair
{
    const char* name;
    const char* value;
} EntraAuthStringPair;

/** NULL/0 inherits defaults; non-NULL/0 explicitly selects an empty dictionary. */
typedef struct EntraAuthStringParameters
{
    const EntraAuthStringPair* entries; /**< At most 128 pairs; names at most 1024 bytes and values at most 8192 bytes. */
    uint32_t count;
} EntraAuthStringParameters;

/** Selects system-assigned identity when userAssignedClientId is NULL. */
typedef struct EntraAuthManagedIdentityOptions
{
    const char* userAssignedClientId;
} EntraAuthManagedIdentityOptions;

/** Borrowed signed HTTP request settings. */
typedef struct EntraAuthPoPOptions
{
    const char* httpMethod;
    const char* httpHost;
    const char* httpPath;
    const char* nonce;
    bool signHttpRequest; /**< True signs the request; false returns the unsigned bound token. */
} EntraAuthPoPOptions;

/** Borrowed agent application/user selector; supply at most one user selector. */
typedef struct EntraAuthAgentIdentityOptions
{
    const char* clientId; /**< Required nonempty application GUID. */
    const char* userPrincipalName;
    const char* userObjectId; /**< Optional nonempty user GUID. */
} EntraAuthAgentIdentityOptions;

/** Request-only HTTP overrides; not valid for header-only acquisition. */
typedef struct EntraAuthDownstreamRequestOptions
{
    bool graphAuthentication; /**< Overrides the service default when this structure is supplied. */
    EntraAuthStringParameters extraHeaderParameters;
    EntraAuthStringParameters extraQueryParameters;
} EntraAuthDownstreamRequestOptions;

/**
 * Shared token and acquisition overrides embedded by value in each complete input.
 * Zero-initialize the input. All strings are borrowed, zero-terminated UTF-8.
 * NULL fields inherit defaults where supported.
 * The SDK copies the entire pointer graph before async submission returns.
 * The named service's RequestAppToken selects application or user acquisition.
 * Without that setting, user context selects user acquisition; otherwise application acquisition is used.
 * Managed identity supports only application acquisition.
 */
typedef struct EntraAuthDownstreamApiOptionsOverrides
{
    const char* const* scopes; /**< Borrowed scope strings; NULL/count 0 inherits named scopes. Non-NULL/count 0 explicitly clears them. */
    uint32_t scopesCount; /**< Number of scopes, otherwise 1..64; application acquisition requires one resource/.default scope. */
    bool forceRefresh; /**< false permits cached tokens; true forces refresh. Overrides the service default. */
    const char* tenant; /**< Optional tenant GUID or DNS-name override for this request. */
    const char* claims; /**< Optional URL-encoded JSON-object claims request, at most 1 MiB before decoding; decoded exactly once. */
    uint32_t timeoutMilliseconds; /**< Shared operation deadline: 0 selects 60000 ms; otherwise 1..600000 ms. */
    const char* clientAssertion; /**< Optional host-signed JWT, max 1 MiB; requires matching ClientId and future exp. Can override a configured credential per request.
                                     Not signature-validated, logged or provider-cached; rotation alone does not bypass the access-token cache. */
    const char* protocolScheme; /**< Optional UTF-8 scheme; NULL inherits the service/default Bearer. Bearer/MTLS_POP headers and MTLS HTTP are supported case-insensitively. */
    const char* authenticationOptionsName;
    const char* userFlow;
    const char* homeAccountId;
    const char* username;
    const char* password;
    EntraAuthStringParameters extraQueryParameters;
    EntraAuthStringParameters extraHeadersParameters;
    EntraAuthStringParameters extraBodyParameters;
    const char* longRunningWebApiSessionKey;
    const EntraAuthManagedIdentityOptions* managedIdentity;
    const char* popPublicKey;
    const char* popClaim;
    const EntraAuthPoPOptions* poPConfiguration;
    const char* fmiPath;
    const char* clientAssertionFmiPath;
    const EntraAuthAgentIdentityOptions* agentIdentity;
    const EntraAuthDownstreamRequestOptions* downstreamApiOptions;
    const char* clientAssertionClaims; /**< Optional JSON claims forwarded unchanged to IdWeb for client assertion creation, at most 1 MiB. */
    const char* clientId; /**< Optional confidential-client override; requires exactly one certificate or signed client assertion. */
    const char* clientCertificate; /**< Optional base64 certificate with private key, at most 1 MiB; never log. */
    const char* azureRegion; /**< Optional region for the overridden confidential client; requires clientId. */
} EntraAuthDownstreamApiOptionsOverrides, EntraAuthAuthorizationHeaderOptionsOverrides;

/**
 * Borrowed header request.
 * Header-only application MTLS_POP returns its binding certificate automatically.
 * Header and HTTP inputs are peers that embed the same overrides layout.
 * Their correlation IDs are request-local, not part of the shared overrides.
 */
typedef struct EntraAuthAuthorizationHeaderInput
{
    const char* correlationId; /**< Optional GUID string; NULL/blank inherits valid context correlation, otherwise generates an ID. Invalid nonblank values generate a fresh ID. */
    EntraAuthAuthorizationHeaderOptionsOverrides overrides; /**< Embedded token/acquisition settings shared with HTTP inputs. */
} EntraAuthAuthorizationHeaderInput;

/**
 * Borrowed HTTPS request for a named service with configured BaseUrl.
 * Token and acquisition settings are supplied through the embedded overrides.
 * overrides.protocolScheme also accepts case-insensitive MTLS_POP for authenticated
 * application acquisition and MTLS for certificate-only authentication without token acquisition.
 * HTTPS redirects follow HTTP-client defaults and clear Authorization; the shared deadline covers acquisition/response.
 * App-only MTLS_POP requires an acquired private-key binding certificate; the SDK retains
 * it for TLS internally. Anonymous/user-bound calls fail rather than falling back.
 * MTLS instead selects the configured application's certificate, sends no Authorization
 * header and returns no token/expiration/STS metadata. A missing certificate fails without
 * ordinary-HTTP fallback. Certificate-related status codes 400/401/403/495/496 retry once;
 * a 401 claims retry uses the selected client and bypasses certificate-result notifications.
 */
typedef struct EntraAuthDownstreamApiInput
{
    const char* correlationId; /**< Optional GUID string; NULL/blank inherits valid context correlation, otherwise generates an ID. Invalid nonblank values generate a fresh ID. */
    const char* contentType; /**< Optional UTF-8 request media type; content defaults to application/json when present. */
    const uint8_t* body; /**< Borrowed binary bytes, at most 1 MiB; may contain zero bytes and non-UTF-8 values. */
    uint32_t bodyLength; /**< Byte count, not string length; must be zero when body is NULL. */
    const char* acceptHeader; /**< Optional UTF-8 Accept header value. */
    const char* relativePath; /**< Optional UTF-8 path override; initial destination must remain on configured HTTPS origin without userinfo/fragment. */
    const char* method; /**< Optional UTF-8 HTTP method override; otherwise configured HttpMethod or GET. */
    EntraAuthDownstreamApiOptionsOverrides overrides; /**< Embedded token/acquisition settings; the acquired header is applied to the initial HTTP request. */
} EntraAuthDownstreamApiInput;

/** Owned acquisition metadata embedded in a header or HTTP result; never free its fields separately. */
typedef struct EntraAuthTokenResult
{
    EntraAuthErrorCategory errorCategory; /**< Describes the complete operation outcome. */
    uint32_t hasExpiresOn; /**< 1 when authoritative token expiration is available; otherwise 0. */
    int64_t expiresOnUnixTimeMilliseconds; /**< Absolute UTC Unix-epoch expiration in ms; use only when hasExpiresOn is 1. */
    uint32_t hasTokenEndpointHttpStatus; /**< 1 when an STS HTTP response status was observed; otherwise 0. */
    int32_t tokenEndpointHttpStatus; /**< Observed STS status, never downstream status; use only when its availability flag is 1. */
    const char* accessToken; /**< Acquired secret token, or NULL when unavailable; never log it or infer expiration by parsing it. */
    const char* tokenType; /**< Acquired token type, or NULL when unavailable. */
    const char* correlationId; /**< Correlation GUID for this operation. */
    const char* providerError; /**< Provider error code when available, otherwise NULL. */
    const char* providerSubError; /**< Provider suberror code when available, otherwise NULL. */
    const char* errorDescription; /**< Managed type/message or downstream error body; may contain sensitive data. */
    const char* claimsChallenge; /**< Provider claims challenge for host handling, or NULL; no UI or automatic retry is performed. */
} EntraAuthTokenResult;

/**
 * Owned header and metadata from one acquisition; free with EntraAuthFreeAuthorizationHeaderResult.
 * All non-NULL strings are UTF-8 and share the result allocation.
 * On Windows, bindingCertificate is a caller-owned duplicated PCCERT_CONTEXT.
 * Free it separately with CertFreeCertificateContext after native certificate/TLS use has finished.
 * Result disposal, operation completion and configuration retirement do not free this context.
 * On other platforms bindingCertificate is NULL; use the downstream API exports for SDK-owned mTLS HTTP.
 * Failures do not return a certificate context.
 */
typedef struct EntraAuthAuthorizationHeaderResult
{
    EntraAuthTokenResult token; /**< Operation outcome and associated token/expiry metadata; not independently allocated. */
    const char* authorizationHeader; /**< Owned zero-terminated UTF-8 Bearer, PoP or MTLS_POP header on success; otherwise NULL. */
    void* bindingCertificate; /**< Optional caller-owned Windows PCCERT_CONTEXT; NULL on other platforms or when not returned. */
    const char* longRunningWebApiSessionKey; /**< Effective OBO session key, including an automatically allocated key; NULL when not applicable. Secret; never log. */
    const char* metadataJson; /**< Optional owned JSON: TenantId, IdToken, Scopes, CorrelationId, HomeAccountId, LongRunningWebApiSessionKey, AdditionalResponseParameters
                                  and Acquisition metadata. May contain secrets; never log. */
} EntraAuthAuthorizationHeaderResult;

/**
 * Owned HTTP outcome; free the complete allocation with EntraAuthFreeDownstreamApiResult.
 * Non-2xx responses retain acquisition metadata and actual response fields with
 * ENTRA_AUTH_ERROR_DOWNSTREAM_API; transport/cancellation may leave response data unavailable.
 */
typedef struct EntraAuthDownstreamApiResult
{
    EntraAuthTokenResult token; /**< Complete operation outcome and acquisition metadata, separate from downstream status. */
    uint32_t hasHttpResponseStatus; /**< 1 when downstream response status was observed, even if later body consumption fails. */
    int32_t httpResponseStatusCode; /**< Actual downstream status; use only when hasHttpResponseStatus is 1. */
    const uint8_t* body; /**< Owned binary response, max 16 MiB; NULL for zero length, not zero-terminated text. */
    uint32_t bodyLength; /**< Number of available response bytes; never use string functions to find the length. */
    const char* contentType; /**< Owned UTF-8 response media type, or NULL when absent/unavailable. */
    const char* wwwAuthenticate; /**< Owned UTF-8 downstream challenge, or NULL; untrusted input, not automatically retried. */
    const char* metadataJson; /**< Owned JSON adds HttpResponse (Version, ReasonPhrase, Headers, ContentHeaders, TrailingHeaders);
                                  header values are arrays. Anonymous calls omit acquisition metadata. May contain secrets; never log. */
} EntraAuthDownstreamApiResult;

/**
 * Owned submission receipt, independent of any callback result.
 * Rejection has no operation ID or callback; acceptance completes exactly once, possibly inline.
 * Free with EntraAuthFreeAsyncSubmissionResult; operation tracking is retired automatically after callback return.
 */
typedef struct EntraAuthAsyncSubmissionResult
{
    EntraAuthErrorCategory errorCategory; /**< Submission outcome, not the eventual completion outcome. */
    uint32_t hasOperationId; /**< 1 when accepted with an operation ID; 0 when rejected. */
    uint64_t operationId; /**< Accepted nonzero, process-local opaque ID, not a pointer; invalid after callback return. */
    const char* correlationId; /**< Owned zero-terminated UTF-8 correlation GUID for this submission. */
    const char* errorDescription; /**< Owned managed type/message on rejection, or NULL on acceptance; may contain sensitive data. */
} EntraAuthAsyncSubmissionResult;


/**
 * Receives exactly one owned header result for an accepted submission, possibly before submission returns.
 * Runs without facade locks or thread affinity; may reenter APIs, but must not throw or block indefinitely.
 * Keep callback code/context alive through return.
 * @param result Ownership transfers to the host; free with EntraAuthFreeAuthorizationHeaderResult.
 * @param operationId Accepted operation ID; automatically retired after this callback returns.
 * @param callerContext Unmodified host pointer supplied on submission; never freed by the facade.
 */
typedef void (*EntraAuthHeaderCompletionCallback)(
    EntraAuthAuthorizationHeaderResult* result, uint64_t operationId, void* callerContext);

/**
 * Receives exactly one owned HTTP result for an accepted submission, possibly before submission returns.
 * Runs without facade locks or thread affinity; may reenter APIs, but must not throw or block indefinitely.
 * Keep callback code/context alive through return.
 * @param result Ownership transfers to the host; free with EntraAuthFreeDownstreamApiResult.
 * @param operationId Accepted operation ID; automatically retired after this callback returns.
 * @param callerContext Unmodified host pointer supplied on submission; never freed by the facade.
 */
typedef void (*EntraAuthDownstreamCompletionCallback)(
    EntraAuthDownstreamApiResult* result, uint64_t operationId, void* callerContext);

/**
 * Receives an owned, redacted log message without facade locks; delivery may be concurrent.
 * Must not throw or block indefinitely; same-thread recursive delivery is suppressed.
 * Keep callback code/context alive until successful logging unassignment drains delivery.
 * @param message Ownership transfers to the host; free with EntraAuthFreeLoggingCallbackContext.
 * @param callerContext Unmodified registration pointer; never freed by the facade.
 */
typedef void (*EntraAuthLogMessageCallback)(
    EntraAuthLoggingCallbackContext* message, void* callerContext);

/**
 * Configures or transactionally replaces the acquisition environment and process-local cache.
 * Admitted operations retain their original environment; failed replacement preserves the previous one.
 * Success validates local configuration, not endpoint/credential acceptance.
 * The configured facade lives for the process lifetime; there is no global shutdown/reset.
 * Hosts coordinate outstanding callbacks before retiring their contexts and unregister logging separately.
 * Never FreeLibrary/dlclose an initialized Native AOT library.
 * @param configurationJson Required borrowed zero-terminated UTF-8 JSON, at most 1 MiB; valid until return.
 * @param sectionName Optional borrowed UTF-8 application-section name; NULL selects AzureAd.
 * @return Owned outcome; free with EntraAuthFreeControlResult.
 */
ENTRA_AUTH_API EntraAuthControlResult* EntraAuthConfigure(const char* configurationJson, const char* sectionName);

/**
 * Clears and frees a complete control result allocated by this library.
 * @param result Owned control result, or NULL for no action; embedded pointers must not be freed separately.
 */
ENTRA_AUTH_API void EntraAuthFreeControlResult(EntraAuthControlResult* result);

/**
 * Acquires a token and supported Authorization header synchronously with optional host-authenticated user context.
 * Input mode, then named RequestAppToken, then context presence select the flow; required OBO never falls back.
 * Token, expiry and header come from one acquisition. Borrowed inputs must remain readable until return.
 * @param context Optional exchange context; parsed, not authenticated or decrypted by this API.
 * @param input Required versioned header input; explicit scopes override named scopes.
 * @param downstreamApiName Optional borrowed UTF-8 entry name in DownstreamApis; NULL requires explicit scopes.
 * @return Owned header/outcome; free the complete result with EntraAuthFreeAuthorizationHeaderResult.
 */
ENTRA_AUTH_API EntraAuthAuthorizationHeaderResult* EntraAuthGetAuthorizationHeader(
    const EntraAuthRequestContext* context, const EntraAuthAuthorizationHeaderInput* input, const char* downstreamApiName);

/**
 * Acquires a token and supported Authorization header synchronously without inbound user context.
 * Unauthenticated does not mean anonymous outbound access: configured application credentials are used.
 * An input/configuration selecting OBO fails without falling back; borrowed inputs remain valid until return.
 * @param input Required versioned header input with acquisition options and optional explicit scopes.
 * @param downstreamApiName Optional borrowed UTF-8 entry name in DownstreamApis; NULL requires explicit scopes.
 * @return Owned header/outcome; free with EntraAuthFreeAuthorizationHeaderResult.
 */
ENTRA_AUTH_API EntraAuthAuthorizationHeaderResult* EntraAuthGetAuthorizationHeaderUnauthenticated(
    const EntraAuthAuthorizationHeaderInput* input, const char* downstreamApiName);

/**
 * Acquires a token and synchronously calls a named HTTPS API with optional host-authenticated user context.
 * Flow follows input/configuration/context selection; required OBO never falls back.
 * HTTPS redirects follow HTTP-client defaults and clear Authorization; one deadline covers acquisition and response.
 * Non-2xx results retain actual downstream status/body separately from STS status.
 * @param context Optional exchange context; parsed, not authenticated or decrypted by this API.
 * @param input Required versioned HTTP input; all borrowed strings/body bytes remain valid until return.
 * @param downstreamApiName Required borrowed UTF-8 DownstreamApis entry name selecting scopes and an HTTPS BaseUrl.
 * @return Owned HTTP/outcome, including binary response; free with EntraAuthFreeDownstreamApiResult.
 */
ENTRA_AUTH_API EntraAuthDownstreamApiResult* EntraAuthCallDownstreamApi(
    const EntraAuthRequestContext* context, const EntraAuthDownstreamApiInput* input, const char* downstreamApiName);

/**
 * Acquires an application token and synchronously calls a named HTTPS API without inbound user context.
 * Outbound access is authenticated with configured credentials; selecting OBO fails without fallback.
 * HTTPS redirects follow HTTP-client defaults and clear Authorization; one deadline covers acquisition and response.
 * Non-2xx results retain actual downstream status/body separately from STS status.
 * @param input Required versioned HTTP input; all borrowed strings/body bytes remain valid until return.
 * @param downstreamApiName Required borrowed UTF-8 DownstreamApis entry name selecting scopes and an HTTPS BaseUrl.
 * @return Owned HTTP/outcome, including binary response; free with EntraAuthFreeDownstreamApiResult.
 */
ENTRA_AUTH_API EntraAuthDownstreamApiResult* EntraAuthCallDownstreamApiUnauthenticated(
    const EntraAuthDownstreamApiInput* input, const char* downstreamApiName);

/**
 * Submits header acquisition with optional host-authenticated user context, copying inputs before return.
 * Input/configuration/context select the flow; required OBO never falls back.
 * Rejection has no ID or callback; acceptance completes once, possibly inline before this function returns.
 * @param context Optional exchange context; parsed, not authenticated or decrypted by this API.
 * @param input Required borrowed versioned header input; explicit scopes override named scopes.
 * @param downstreamApiName Optional borrowed UTF-8 entry name in DownstreamApis; NULL requires explicit scopes.
 * @param callback Required completion receiving an independently owned header result.
 * @param callerContext Host pointer returned unchanged; keep it and callback code alive through callback return.
 * @return Owned receipt; free with EntraAuthFreeAsyncSubmissionResult. Operation tracking is retired automatically.
 */
ENTRA_AUTH_API EntraAuthAsyncSubmissionResult* EntraAuthGetAuthorizationHeaderAsync(
    const EntraAuthRequestContext* context, const EntraAuthAuthorizationHeaderInput* input, const char* downstreamApiName,
    EntraAuthHeaderCompletionCallback callback, void* callerContext);

/**
 * Submits header acquisition without inbound user context, copying inputs before return.
 * Uses configured application credentials, not anonymous access; OBO selection fails without fallback.
 * Rejection has no ID or callback; acceptance completes once, possibly inline before this function returns.
 * @param input Required borrowed versioned header input with acquisition options and optional explicit scopes.
 * @param downstreamApiName Optional borrowed UTF-8 entry name in DownstreamApis; NULL requires explicit scopes.
 * @param callback Required completion receiving an independently owned header result.
 * @param callerContext Host pointer returned unchanged; keep it and callback code alive through callback return.
 * @return Owned receipt; free with EntraAuthFreeAsyncSubmissionResult. Operation tracking is retired automatically.
 */
ENTRA_AUTH_API EntraAuthAsyncSubmissionResult* EntraAuthGetAuthorizationHeaderUnauthenticatedAsync(
    const EntraAuthAuthorizationHeaderInput* input, const char* downstreamApiName,
    EntraAuthHeaderCompletionCallback callback, void* callerContext);

/**
 * Submits an authenticated HTTPS call with optional user context, copying strings/body before return.
 * Flow follows input/configuration/context; one deadline covers acquisition, redirects and response.
 * HTTPS redirects follow HTTP-client defaults and clear Authorization.
 * Rejection has no ID or callback; acceptance completes once, possibly inline before this function returns.
 * @param context Optional host-authenticated exchange context; this API only parses its assertion.
 * @param input Required borrowed versioned HTTP input with optional same-origin path/method/body overrides.
 * @param downstreamApiName Required borrowed UTF-8 DownstreamApis entry name selecting scopes and an HTTPS BaseUrl.
 * @param callback Required completion receiving an independently owned HTTP result with separate downstream/STS statuses.
 * @param callerContext Host pointer returned unchanged; keep it and callback code alive through callback return.
 * @return Owned receipt; free with EntraAuthFreeAsyncSubmissionResult. Operation tracking is retired automatically.
 */
ENTRA_AUTH_API EntraAuthAsyncSubmissionResult* EntraAuthCallDownstreamApiAsync(
    const EntraAuthRequestContext* context, const EntraAuthDownstreamApiInput* input, const char* downstreamApiName,
    EntraAuthDownstreamCompletionCallback callback, void* callerContext);

/**
 * Submits an authenticated HTTPS call without user context, copying strings/body before return.
 * Uses configured application credentials; OBO selection fails without fallback.
 * HTTPS redirects follow HTTP-client defaults and clear Authorization; one deadline covers acquisition/redirects/response.
 * Accepted work completes once, possibly inline. Rejection never calls back.
 * @param input Required borrowed versioned HTTP input with optional same-origin path/method/body overrides.
 * @param downstreamApiName Required borrowed UTF-8 DownstreamApis entry name selecting scopes and an HTTPS BaseUrl.
 * @param callback Required completion receiving an independently owned HTTP result with separate downstream/STS statuses.
 * @param callerContext Host pointer returned unchanged; keep it and callback code alive through callback return.
 * @return Owned receipt; free with EntraAuthFreeAsyncSubmissionResult. Operation tracking is retired automatically.
 */
ENTRA_AUTH_API EntraAuthAsyncSubmissionResult* EntraAuthCallDownstreamApiUnauthenticatedAsync(
    const EntraAuthDownstreamApiInput* input, const char* downstreamApiName,
    EntraAuthDownstreamCompletionCallback callback, void* callerContext);

/**
 * Clears and frees a complete header result and all its embedded data.
 * @param result Owned header result from this library, or NULL; never pass its token prefix or an embedded string.
 */
ENTRA_AUTH_API void EntraAuthFreeAuthorizationHeaderResult(EntraAuthAuthorizationHeaderResult* result);

/**
 * Clears and frees a complete HTTP result, including body and strings.
 * @param result Owned HTTP result from this library, or NULL; never free its fields/prefix separately.
 */
ENTRA_AUTH_API void EntraAuthFreeDownstreamApiResult(EntraAuthDownstreamApiResult* result);

/**
 * Clears and frees a submission receipt without cancelling work or freeing callback results.
 * @param result Owned receipt from this library, or NULL.
 */
ENTRA_AUTH_API void EntraAuthFreeAsyncSubmissionResult(EntraAuthAsyncSubmissionResult* result);

/**
 * Requests best-effort cancellation without waiting for completion or callback return.
 * Success does not guarantee cancellation wins; accepted work still completes once with an owned result.
 * @param operationId An accepted operation ID; unknown IDs and IDs retired after callback return fail.
 * @return Owned control result; free with EntraAuthFreeControlResult.
 */
ENTRA_AUTH_API EntraAuthControlResult* EntraAuthCancelOperation(uint64_t operationId);

/**
 * Registers one process-wide logging callback for facade outcomes and managed SDK diagnostics.
 * NULL or duplicate registrations and calls from a logging callback fail without replacing the registration.
 * SDK messages follow standard Logging:LogLevel category filters (default: Information), preserving managed
 * text/redaction and PII settings. They include category/event IDs and may contain sensitive identifiers,
 * URLs and provider text. Protect diagnostic output accordingly.
 * @param callback Required callback receiving owned messages; delivery may be concurrent.
 * @param callerContext Host pointer kept unchanged; retain it/code until successful unassignment drains delivery.
 * @return Owned control result; free with EntraAuthFreeControlResult.
 */
ENTRA_AUTH_API EntraAuthControlResult* EntraAuthAssignLogMessageCallback(EntraAuthLogMessageCallback callback, void* callerContext);

/**
 * Closes log delivery and synchronously drains active logging callbacks before unregistering.
 * Idempotent when unassigned; calls from a logging callback fail.
 * Only successful drain permits retiring callback code/context. Owned messages may outlive unassignment.
 * @return Owned control result; free with EntraAuthFreeControlResult.
 */
ENTRA_AUTH_API EntraAuthControlResult* EntraAuthUnassignLogMessageCallback(void);

/**
 * Clears and frees one callback-owned log message, not the host's callerContext or registration.
 * @param message Complete owned message from this library, or NULL; callable after unassignment.
 */
ENTRA_AUTH_API void EntraAuthFreeLoggingCallbackContext(EntraAuthLoggingCallbackContext* message);

#ifdef __cplusplus
}

#include <memory>
#ifdef _WIN32
#include <objbase.h>
#else
#include <cstdlib>
#endif

/** C++ ownership utilities for complete SDK result allocations. */
namespace EntraAuth
{
/** Releases a complete result using the platform allocator without wiping its contents. */
template <typename T>
struct CoTaskMemDeleter
{
    /** Frees the allocation base with CoTaskMemFree on Windows or free on Linux; accepts NULL. */
    void operator()(const T* result) const noexcept
    {
        void* allocation = const_cast<void*>(static_cast<const void*>(result));
#ifdef _WIN32
        ::CoTaskMemFree(allocation);
#else
        std::free(allocation);
#endif
    }
};

/**
 * Movable, noncopyable owner of a read-only complete result; direct deallocation does not wipe secrets.
 * Construct from a returned result pointer; never own an embedded prefix, string or body independently.
 */
template <typename T>
using ResultPtr = std::unique_ptr<const T, CoTaskMemDeleter<T>>;

}
#endif
#endif

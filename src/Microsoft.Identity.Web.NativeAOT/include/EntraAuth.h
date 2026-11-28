/* Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. */

#ifndef ENTRA_AUTH_H
#define ENTRA_AUTH_H

/**
 * @file
 * Each complete control, log, header or HTTP result is one allocation containing all its data.
 * Its returned pointer is the allocation base: CoTaskMemFree on Windows or free on Linux can release it directly.
 * Direct system deallocation and EntraAuth::ResultPtr (C++) do not wipe secrets; matching SDK free exports do.
 * Never free embedded strings or bodies separately, or assume payload immediately follows the struct.
 * Use the matching SDK header and library.
 * Initialize every input field; {0} in C or {} in C++ supplies zero values and NULL pointers.
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

#ifdef _WIN32
/** Uses the Windows standard calling convention for exported functions. */
#define ENTRA_AUTH_CALLTYPE __stdcall
#else
/** Uses the platform default calling convention for exported functions. */
#define ENTRA_AUTH_CALLTYPE
#endif

#ifdef __cplusplus
extern "C" {
#endif

/** Severity values matching Microsoft.Extensions.Logging.LogLevel; declaring a level does not enable its emission. */
typedef enum EntraAuthLogLevel
{
    EntraAuthLogLevelTrace = 0, /**< The most detailed diagnostic severity. */
    EntraAuthLogLevelDebug = 1, /**< Diagnostic information intended for development and investigation. */
    EntraAuthLogLevelInformation = 2, /**< Normal configuration and operation outcomes. */
    EntraAuthLogLevelWarning = 3, /**< An abnormal event that does not stop the current operation. */
    EntraAuthLogLevelError = 4, /**< A failure of the current configuration or operation. */
    EntraAuthLogLevelCritical = 5, /**< A catastrophic failure requiring immediate attention. */
    EntraAuthLogLevelNone = 6 /**< Filtering sentinel; never an emitted severity. */
} EntraAuthLogLevel;

/** Owned control outcome; free the complete allocation with EntraAuthFreeControlResult. */
typedef struct EntraAuthControlResult
{
    int errorCode; /**< 0 indicates success, 100 a configuration error and 200 a logging callback error. */
    const char* errorDescription; /**< Owned redacted UTF-8 explanation, or NULL; valid until the result is freed. */
} EntraAuthControlResult;

/** Owned log message transferred to a callback; free with EntraAuthFreeLogMessageCallbackContext. */
typedef struct EntraAuthLogMessageCallbackContext
{
    const char* message; /**< Owned zero-terminated UTF-8 text containing only redacted facade diagnostics. */
    EntraAuthLogLevel level; /**< Diagnostic severity; EntraAuthLogLevelNone is never emitted. */
} EntraAuthLogMessageCallbackContext;

/**
 * Host-supplied exchange context, not proof of inbound authentication.
 * The host authenticates/authorizes the caller; the SDK only parses this JWT,
 * without validating its signature, issuer, audience or expiration, or decrypting it.
 * Borrowed inputs must remain readable until sync return or async submission return.
 * Reserved fields must remain NULL or zero and are ignored.
 */
typedef struct EntraAuthValidationResult
{
    unsigned int httpResponseStatusCode; /**< HTTP response status reported by the host's preceding authentication operation. */
    const char* correlationId; /**< Optional preceding host correlation GUID, at most 128 UTF-8 bytes; copied on submission. */
    const char* errorDescription; /**< Optional zero-terminated UTF-8 host authentication error description. */
    const char* wwwAuthenticate; /**< Optional zero-terminated UTF-8 host authentication WWW-Authenticate header. */
    const char* subjectToken; /**< Required zero-terminated UTF-8 compact JWT, at most 1 MiB; copied on submission. */
    const char* subjectTokenType; /**< Optional zero-terminated UTF-8 subject token type supplied by the host. */
    const char* reserved1; /**< Reserved; must be NULL. */
    const char* reserved2; /**< Reserved; must be NULL. */
    const char* reserved3; /**< Reserved; must be NULL. */
    const void* reserved4; /**< Reserved opaque pointer; must be NULL. */
    unsigned int reserved5; /**< Reserved; must be 0. */
    const void* reserved6; /**< Reserved opaque pointer; must be NULL. */
    unsigned int reserved7; /**< Reserved; must be 0. */
    const char* reserved8; /**< Reserved; must be NULL. */
    const void* reserved9; /**< Reserved opaque pointer; must be NULL. */
    const void* reserved10; /**< Reserved opaque pointer; must be NULL. */
    unsigned int reserved11; /**< Reserved; must be 0. */
    const void* reserved12; /**< Reserved opaque pointer; must be NULL. */
    const void* reserved13; /**< Reserved opaque pointer; must be NULL. */
    unsigned int reserved14; /**< Reserved; must be 0. */
    const char** reserved15; /**< Reserved; must be NULL. */
    unsigned int reserved16; /**< Reserved; must be 0. */
} EntraAuthValidationResult;

/**
 * Shared token and acquisition overrides embedded by value in each complete input.
 * Zero-initialize the input. All strings are borrowed, zero-terminated UTF-8.
 * NULL fields inherit defaults where supported.
 * Reserved fields must remain NULL or zero and are ignored.
 * The SDK copies the entire pointer graph before async submission returns.
 * The named service's RequestAppToken selects application or user acquisition.
 * Without that setting, user context selects user acquisition; otherwise application acquisition is used.
 * Managed identity supports only application acquisition.
 */
typedef struct EntraAuthDownstreamApiOptionsOverrides
{
    const char** scopes; /**< Borrowed scope strings; NULL/count 0 inherits named scopes. Non-NULL/count 0 explicitly clears them. */
    unsigned int scopesCount; /**< Number of scopes, otherwise 1..64; application acquisition requires one resource/.default scope. */
    const char* tenant; /**< Optional tenant GUID or DNS-name override for this request. */
    const char* claimsJson; /**< Optional URL-encoded JSON-object claims request, at most 1 MiB before decoding; decoded exactly once. */
    const char* managedIdentityClientId; /**< Optional confidential-client override; requires exactly one certificate or signed client assertion. */
    const char* managedIdentityBase64encodedCertificate; /**< Optional base64 certificate with private key, at most 1 MiB; never log. */
    const char* azureRegion; /**< Optional region for the overridden confidential client; requires managedIdentityClientId. */
    const char* signedClientAssertion; /**< Optional host-signed JWT, max 1 MiB; must match the selected client ID and have future exp. Can override a configured credential per request.
                                           Authenticates the application, including OBO, independently of validationResult.subjectToken.
                                           Not signature-validated, logged or provider-cached; rotation alone does not bypass the access-token cache. */
    const char* fmiPath;
    int forceRefresh; /**< Must be 0 (permit cached tokens) or 1 (force refresh). Overrides the service default. */
    const char* reserved1; /**< Reserved; must be NULL. */
    const char* reserved2; /**< Reserved; must be NULL. */
    const char* relativePath; /**< Optional HTTP path override; initial destination must remain on configured HTTPS origin without userinfo/fragment. */
    const char** reserved3; /**< Reserved; must be NULL. */
    unsigned int reserved4; /**< Reserved; must be 0. */
    const char** reserved5; /**< Reserved; must be NULL. */
    unsigned int reserved6; /**< Reserved; must be 0. */
    const char* reserved7; /**< Reserved; must be NULL. */
    const char* longRunningWebApiSessionKey;
    const char* clientAssertionClaims; /**< Optional JSON claims forwarded unchanged to IdWeb for client assertion creation, at most 1 MiB. */
    const char* reserved8; /**< Reserved; must be NULL. */
} EntraAuthDownstreamApiOptionsOverrides, EntraAuthAuthorizationHeaderOptionsOverrides;

/**
 * Borrowed header request.
 * Header-only application MTLS_POP returns its binding certificate automatically.
 * Header and HTTP inputs are peers that embed the same overrides layout.
 * Their correlation IDs are request-local, not part of the shared overrides.
 */
typedef struct EntraAuthDownstreamAuthorizationHeaderInput
{
    const char* correlationId; /**< Optional GUID string; NULL/blank inherits valid context correlation, otherwise generates an ID. Invalid nonblank values generate a fresh ID. */
    EntraAuthAuthorizationHeaderOptionsOverrides overrides; /**< Embedded token/acquisition settings shared with HTTP inputs. */
} EntraAuthDownstreamAuthorizationHeaderInput;

/**
 * Borrowed HTTPS request for a named service with configured BaseUrl.
 * Token and acquisition settings are supplied through the embedded overrides.
 * The named service's ProtocolScheme accepts case-insensitive MTLS_POP for authenticated
 * application acquisition and MTLS for certificate-only authentication without token acquisition.
 * HTTPS redirects follow HTTP-client defaults and clear Authorization; the shared deadline covers acquisition/response.
 * App-only MTLS_POP requires an acquired private-key binding certificate; the SDK retains
 * it for TLS internally. Anonymous/user-bound calls fail rather than falling back.
 * MTLS instead selects the configured application's certificate, sends no Authorization
 * header. A missing certificate fails without
 * ordinary-HTTP fallback. Certificate-related status codes 400/401/403/495/496 retry once;
 * a 401 claims retry uses the selected client and bypasses certificate-result notifications.
 */
typedef struct EntraAuthDownstreamApiInput
{
    const char* correlationId; /**< Optional GUID string; NULL/blank inherits valid context correlation, otherwise generates an ID. Invalid nonblank values generate a fresh ID. */
    const char* contentType; /**< Optional UTF-8 request media type; content defaults to application/json when present. */
    const char* inputBody; /**< Optional borrowed zero-terminated UTF-8 request body, at most 1 MiB; NULL omits the body. */
    const char* acceptHeader; /**< Optional UTF-8 Accept header value. */
    EntraAuthDownstreamApiOptionsOverrides overrides; /**< Embedded token/acquisition settings; the acquired header is applied to the initial HTTP request. */
} EntraAuthDownstreamApiInput;

/**
 * Owned header and metadata from one acquisition; free with EntraAuthFreeDownstreamAuthorizationHeaderResult.
 * All non-NULL strings are UTF-8 and share the result allocation.
 * On Windows, bindingCertificate is a caller-owned duplicated PCCERT_CONTEXT.
 * Free it separately with CertFreeCertificateContext after native certificate/TLS use has finished.
 * Result disposal, operation completion and configuration retirement do not free this context.
 * On other platforms bindingCertificate is NULL; use the downstream API exports for SDK-owned mTLS HTTP.
 * Failures do not return a certificate context.
 */
typedef struct EntraAuthDownstreamAuthorizationHeaderResult
{
    unsigned int httpResponseStatusCode; /**< Operation status for the host response, not observed token-endpoint status; 200 on success. */
    const char* correlationId; /**< Correlation GUID for this operation. */
    const char* errorDescription; /**< Optional managed type/message; may contain sensitive provider data. */
    const char* wwwAuthenticate; /**< Owned UTF-8 header for forwarding an acquisition claims challenge upstream; NULL when absent. */
    const char* authorizationHeader; /**< Owned zero-terminated UTF-8 Bearer, PoP or MTLS_POP header on success; otherwise NULL. */
    void* bindingCertificate; /**< Optional caller-owned Windows PCCERT_CONTEXT; NULL on other platforms or when not returned. */
    const char* longRunningWebApiSessionKey; /**< Effective OBO session key, including an automatically allocated key; NULL when not applicable. Secret; never log. */
    const char* metadata; /**< Optional owned acquisition metadata JSON; format is not contractual and may contain secrets; never log. */
    const char* failureDetails; /**< Optional owned failure diagnostics JSON; NULL on success or when unavailable. Format is not contractual; may contain sensitive data. */
} EntraAuthDownstreamAuthorizationHeaderResult;

/**
 * Owned HTTP outcome; free the complete allocation with EntraAuthFreeDownstreamApiResult.
 * Non-2xx responses retain the response body when available;
 * transport/cancellation may leave response data unavailable.
 */
typedef struct EntraAuthDownstreamApiResult
{
    unsigned int httpResponseStatusCode; /**< Operation HTTP status, including failures before a downstream response. */
    const char* correlationId; /**< Correlation GUID for this operation. */
    const char* errorDescription; /**< Optional managed type/message or downstream error body; may contain sensitive data. */
    const char* wwwAuthenticate; /**< Owned UTF-8 acquisition challenge for forwarding upstream, or downstream challenge; NULL when absent. */
    const char* downstreamApiResult; /**< Optional owned zero-terminated UTF-8 response body, max 16 MiB. */
} EntraAuthDownstreamApiResult;

/**
 * Receives exactly one owned header result on success or failure, possibly before the async export returns.
 * Runs without facade locks or thread affinity; may reenter APIs, but must not throw or block indefinitely.
 * Keep callback code/context alive through return.
 * @param entraAuthDownstreamAuthorizationHeaderResult Ownership transfers to the host; free with EntraAuthFreeDownstreamAuthorizationHeaderResult.
 * @param callerContext Unmodified host pointer supplied on submission; never freed by the facade.
 */
typedef void (*EntraAuthGetAuthorizationHeaderCompletionCallbackFunc)(
    EntraAuthDownstreamAuthorizationHeaderResult* entraAuthDownstreamAuthorizationHeaderResult, void* callerContext);

/**
 * Receives exactly one owned HTTP result on success or failure, possibly before the async export returns.
 * Runs without facade locks or thread affinity; may reenter APIs, but must not throw or block indefinitely.
 * Keep callback code/context alive through return.
 * @param entraAuthDownstreamApiResult Ownership transfers to the host; free with EntraAuthFreeDownstreamApiResult.
 * @param callerContext Unmodified host pointer supplied on submission; never freed by the facade.
 */
typedef void (*EntraAuthDownstreamApiCompletionCallbackFunc)(
    EntraAuthDownstreamApiResult* entraAuthDownstreamApiResult, void* callerContext);

/**
 * Receives an owned, redacted log message without facade locks; delivery may be concurrent.
 * Must not throw or block indefinitely; same-thread recursive delivery is suppressed.
 * Keep callback code/context alive until successful logging unassignment drains delivery.
 * @param managedContext Ownership transfers to the host; free with EntraAuthFreeLogMessageCallbackContext.
 * @param nativeContext Unmodified registration pointer; never freed by the facade.
 */
typedef void (*EntraAuthLogMessageCallbackFunc)(
    EntraAuthLogMessageCallbackContext* managedContext, void* nativeContext);

/**
 * Configures or transactionally replaces the acquisition environment and process-local cache.
 * Admitted operations retain their original environment; failed replacement preserves the previous one.
 * Success validates local configuration, not endpoint/credential acceptance.
 * The configured facade lives for the process lifetime; there is no global shutdown/reset.
 * Hosts coordinate outstanding callbacks before retiring their contexts and unregister logging separately.
 * Never FreeLibrary/dlclose an initialized Native AOT library.
 * @param configurationJson Required borrowed zero-terminated UTF-8 JSON, at most 1 MiB; valid until return.
 * @param configurationSection Optional borrowed UTF-8 application-section name; NULL selects AzureAd.
 * @return Owned outcome; free with EntraAuthFreeControlResult.
 */
ENTRA_AUTH_API const EntraAuthControlResult* ENTRA_AUTH_CALLTYPE EntraAuthConfigure(const char* configurationJson, const char* configurationSection);

/**
 * Clears and frees a complete control result allocated by this library.
 * @param result Owned control result, or NULL for no action; embedded pointers must not be freed separately.
 */
ENTRA_AUTH_API void ENTRA_AUTH_CALLTYPE EntraAuthFreeControlResult(const EntraAuthControlResult* result);

/**
 * Acquires a token and supported Authorization header synchronously with optional host-authenticated user context.
 * Named RequestAppToken selects the flow; without that setting, context presence selects the flow.
 * Required OBO never falls back.
 * Header and acquisition metadata come from one acquisition. Borrowed inputs must remain readable until return.
 * @param validationResult Optional exchange context; parsed, not authenticated or decrypted by this API.
 * @param input Required header input; explicit scopes override named scopes.
 * @param downstreamApiName Optional borrowed UTF-8 entry name in DownstreamApis; NULL requires explicit scopes.
 * @return Owned header/outcome; free the complete result with EntraAuthFreeDownstreamAuthorizationHeaderResult.
 */
ENTRA_AUTH_API const EntraAuthDownstreamAuthorizationHeaderResult* ENTRA_AUTH_CALLTYPE EntraAuthGetAuthorizationHeader(
    const EntraAuthValidationResult* validationResult, const EntraAuthDownstreamAuthorizationHeaderInput* input, const char* downstreamApiName);

/**
 * Acquires a token and supported Authorization header synchronously without inbound user context.
 * Unauthenticated does not mean anonymous outbound access: configured application credentials are used.
 * Configuration selecting OBO fails without falling back; borrowed inputs remain valid until return.
 * @param input Required header input with acquisition options and optional explicit scopes.
 * @param downstreamApiName Optional borrowed UTF-8 entry name in DownstreamApis; NULL requires explicit scopes.
 * @return Owned header/outcome; free with EntraAuthFreeDownstreamAuthorizationHeaderResult.
 */
ENTRA_AUTH_API const EntraAuthDownstreamAuthorizationHeaderResult* ENTRA_AUTH_CALLTYPE EntraAuthGetAuthorizationHeaderUnauthenticated(
    const EntraAuthDownstreamAuthorizationHeaderInput* input, const char* downstreamApiName);

/**
 * Acquires a token and synchronously calls a named HTTPS API with optional host-authenticated user context.
 * Flow follows named RequestAppToken, then context presence; required OBO never falls back.
 * HTTPS redirects follow HTTP-client defaults and clear Authorization; one deadline covers acquisition and response.
 * Non-2xx results retain the response body when available.
 * @param validationResult Optional exchange context; parsed, not authenticated or decrypted by this API.
 * @param input Required HTTP input; all borrowed UTF-8 strings remain valid until return.
 * @param downstreamApiName Required borrowed UTF-8 DownstreamApis entry name selecting scopes and an HTTPS BaseUrl.
 * @return Owned HTTP/outcome, including UTF-8 response text; free with EntraAuthFreeDownstreamApiResult.
 */
ENTRA_AUTH_API const EntraAuthDownstreamApiResult* ENTRA_AUTH_CALLTYPE EntraAuthCallDownstreamApi(
    const EntraAuthValidationResult* validationResult, const EntraAuthDownstreamApiInput* input, const char* downstreamApiName);

/**
 * Acquires an application token and synchronously calls a named HTTPS API without inbound user context.
 * Outbound access is authenticated with configured credentials; selecting OBO fails without fallback.
 * HTTPS redirects follow HTTP-client defaults and clear Authorization; one deadline covers acquisition and response.
 * Non-2xx results retain the response body when available.
 * @param input Required HTTP input; all borrowed UTF-8 strings remain valid until return.
 * @param downstreamApiName Required borrowed UTF-8 DownstreamApis entry name selecting scopes and an HTTPS BaseUrl.
 * @return Owned HTTP/outcome, including UTF-8 response text; free with EntraAuthFreeDownstreamApiResult.
 */
ENTRA_AUTH_API const EntraAuthDownstreamApiResult* ENTRA_AUTH_CALLTYPE EntraAuthCallDownstreamApiUnauthenticated(
    const EntraAuthDownstreamApiInput* input, const char* downstreamApiName);

/**
 * Submits header acquisition with optional host-authenticated user context, copying inputs before return.
 * Named RequestAppToken, then context presence select the flow; required OBO never falls back.
 * Completes exactly once on success or failure, possibly inline before this function returns.
 * @param validationResult Optional exchange context; parsed, not authenticated or decrypted by this API.
 * @param input Required borrowed header input; explicit scopes override named scopes.
 * @param downstreamApiName Optional borrowed UTF-8 entry name in DownstreamApis; NULL requires explicit scopes.
 * @param completionCallback Required completion receiving an independently owned header result.
 * @param nativeContext Host pointer returned unchanged; keep it and callback code alive through callback return.
 * Free the callback result with EntraAuthFreeDownstreamAuthorizationHeaderResult.
 */
ENTRA_AUTH_API void ENTRA_AUTH_CALLTYPE EntraAuthGetAuthorizationHeaderAsync(
    const EntraAuthValidationResult* validationResult, const EntraAuthDownstreamAuthorizationHeaderInput* input, const char* downstreamApiName,
    EntraAuthGetAuthorizationHeaderCompletionCallbackFunc completionCallback, void* nativeContext);

/**
 * Submits header acquisition without inbound user context, copying inputs before return.
 * Uses configured application credentials, not anonymous access; OBO selection fails without fallback.
 * Completes exactly once on success or failure, possibly inline before this function returns.
 * @param input Required borrowed header input with acquisition options and optional explicit scopes.
 * @param downstreamApiName Optional borrowed UTF-8 entry name in DownstreamApis; NULL requires explicit scopes.
 * @param completionCallback Required completion receiving an independently owned header result.
 * @param nativeContext Host pointer returned unchanged; keep it and callback code alive through callback return.
 * Free the callback result with EntraAuthFreeDownstreamAuthorizationHeaderResult.
 */
ENTRA_AUTH_API void ENTRA_AUTH_CALLTYPE EntraAuthGetAuthorizationHeaderUnauthenticatedAsync(
    const EntraAuthDownstreamAuthorizationHeaderInput* input, const char* downstreamApiName,
    EntraAuthGetAuthorizationHeaderCompletionCallbackFunc completionCallback, void* nativeContext);

/**
 * Submits an authenticated HTTPS call with optional user context, copying UTF-8 strings before return.
 * Flow follows named RequestAppToken, then context presence.
 * One deadline covers acquisition, redirects and response.
 * HTTPS redirects follow HTTP-client defaults and clear Authorization.
 * Completes exactly once on success or failure, possibly inline before this function returns.
 * @param validationResult Optional host-authenticated exchange context; this API only parses its assertion.
 * @param input Required borrowed HTTP input with optional same-origin path/body overrides.
 * @param downstreamApiName Required borrowed UTF-8 DownstreamApis entry name selecting scopes and an HTTPS BaseUrl.
 * @param completionCallback Required completion receiving an independently owned HTTP result.
 * @param nativeContext Host pointer returned unchanged; keep it and callback code alive through callback return.
 * Free the callback result with EntraAuthFreeDownstreamApiResult.
 */
ENTRA_AUTH_API void ENTRA_AUTH_CALLTYPE EntraAuthCallDownstreamApiAsync(
    const EntraAuthValidationResult* validationResult, const EntraAuthDownstreamApiInput* input, const char* downstreamApiName,
    EntraAuthDownstreamApiCompletionCallbackFunc completionCallback, void* nativeContext);

/**
 * Submits an authenticated HTTPS call without user context, copying UTF-8 strings before return.
 * Uses configured application credentials; OBO selection fails without fallback.
 * HTTPS redirects follow HTTP-client defaults and clear Authorization; one deadline covers acquisition/redirects/response.
 * Completes exactly once on success or failure, possibly inline.
 * @param input Required borrowed HTTP input with optional same-origin path/body overrides.
 * @param downstreamApiName Required borrowed UTF-8 DownstreamApis entry name selecting scopes and an HTTPS BaseUrl.
 * @param completionCallback Required completion receiving an independently owned HTTP result.
 * @param nativeContext Host pointer returned unchanged; keep it and callback code alive through callback return.
 * Free the callback result with EntraAuthFreeDownstreamApiResult.
 */
ENTRA_AUTH_API void ENTRA_AUTH_CALLTYPE EntraAuthCallDownstreamApiUnauthenticatedAsync(
    const EntraAuthDownstreamApiInput* input, const char* downstreamApiName,
    EntraAuthDownstreamApiCompletionCallbackFunc completionCallback, void* nativeContext);

/**
 * Clears and frees a complete header result and all its embedded data.
 * @param result Owned header result from this library, or NULL; never pass its embedded strings.
 */
ENTRA_AUTH_API void ENTRA_AUTH_CALLTYPE EntraAuthFreeDownstreamAuthorizationHeaderResult(const EntraAuthDownstreamAuthorizationHeaderResult* result);

/**
 * Clears and frees a complete HTTP result, including body and strings.
 * @param result Owned HTTP result from this library, or NULL; never free its embedded fields separately.
 */
ENTRA_AUTH_API void ENTRA_AUTH_CALLTYPE EntraAuthFreeDownstreamApiResult(const EntraAuthDownstreamApiResult* result);

/**
 * Registers one process-wide logging callback for facade outcomes and managed SDK diagnostics.
 * NULL or duplicate registrations and calls from a logging callback fail without replacing the registration.
 * SDK messages follow standard Logging:LogLevel category filters (default: Information), preserving managed
 * text/redaction and PII settings. They include category/event IDs and may contain sensitive identifiers,
 * URLs and provider text. Protect diagnostic output accordingly.
 * @param callback Required callback receiving owned messages; delivery may be concurrent.
 * @param nativeContext Host pointer kept unchanged; retain it/code until successful unassignment drains delivery.
 * @return Owned control result; free with EntraAuthFreeControlResult.
 */
ENTRA_AUTH_API const EntraAuthControlResult* ENTRA_AUTH_CALLTYPE EntraAuthAssignLogMessageCallback(EntraAuthLogMessageCallbackFunc callback, void* nativeContext);

/**
 * Closes log delivery and synchronously drains active logging callbacks before unregistering.
 * Idempotent when unassigned; calls from a logging callback fail.
 * Only successful drain permits retiring callback code/context. Owned messages may outlive unassignment.
 * @return Owned control result; free with EntraAuthFreeControlResult.
 */
ENTRA_AUTH_API const EntraAuthControlResult* ENTRA_AUTH_CALLTYPE EntraAuthUnassignLogMessageCallback();

/**
 * Clears and frees one callback-owned log message, not the host's nativeContext or registration.
 * @param context Complete owned message from this library, or NULL; callable after unassignment.
 */
ENTRA_AUTH_API void ENTRA_AUTH_CALLTYPE EntraAuthFreeLogMessageCallbackContext(const EntraAuthLogMessageCallbackContext* context);

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
    void operator()(T* ptr) const
    {
        if (ptr)
        {
#ifdef _WIN32
            ::CoTaskMemFree((void*)ptr);
#else
            free((void*)ptr);
#endif
        }
    }
};

/**
 * Const, noncopyable and nonmovable owner of a read-only complete result; direct deallocation does not wipe secrets.
 * Construct from a returned result pointer; never own embedded strings or bodies independently.
 */
template <typename T>
using ResultPtr = const std::unique_ptr<const T, CoTaskMemDeleter<const T>>;

}
#endif
#endif

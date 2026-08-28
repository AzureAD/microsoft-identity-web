// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.IdentityModel.Protocols;

namespace Microsoft.Identity.Web.Sidecar.Pop;

/// <summary>
/// SPIKE (throwaway): builds the Wilson <see cref="HttpRequestData"/> that SHR validation binds the
/// signature against, from the request-line contract headers (original-uri / original-method).
/// </summary>
/// <remarks>
/// Design decision (flagged for the design doc): the sidecar is app-invoked today (not Envoy-fronted).
/// The call to <c>/Validate</c> is NOT the request the SHR was signed over, so the calling app MUST
/// supply the original method + URI - there is no safe server-side derivation. Hence these headers are
/// REQUIRED for PoP, a deliberate deviation from MISE (which can fall back to the Envoy-provided
/// Host/method). The header names are kept identical to MISE so a later Envoy front-end and the MISE
/// container remain consistent. Only Uri + Method are needed because v1 validates m/u/p/ts (h and q are
/// OFF), so request headers, query string and body do not participate in the signature.
/// </remarks>
internal static class PopHttpRequestFactory
{
    public static bool TryCreate(HttpRequest request, out HttpRequestData requestData, out string? error)
    {
        requestData = null!;

        string uri = request.Headers[PopConstants.OriginalUriHeaderName].ToString();
        if (string.IsNullOrEmpty(uri))
        {
            error = $"Missing required '{PopConstants.OriginalUriHeaderName}' header for PoP validation.";
            return false;
        }

        string method = request.Headers[PopConstants.OriginalMethodHeaderName].ToString();
        if (string.IsNullOrEmpty(method))
        {
            error = $"Missing required '{PopConstants.OriginalMethodHeaderName}' header for PoP validation.";
            return false;
        }

        if (!Uri.TryCreate(uri, UriKind.Absolute, out Uri? parsedUri))
        {
            error = $"The '{PopConstants.OriginalUriHeaderName}' header must be an absolute URI.";
            return false;
        }

        requestData = new HttpRequestData
        {
            Uri = parsedUri,
            Method = method,
        };

        error = null;
        return true;
    }
}

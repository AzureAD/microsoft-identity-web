// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.IdentityModel.Protocols;

namespace Microsoft.Identity.Web.Sidecar.Pop;

/// <summary>
/// Builds the <see cref="HttpRequestData"/> that SHR validation binds the signature against, from the
/// request-line contract headers (<c>original-uri</c> / <c>original-method</c>).
/// </summary>
/// <remarks>
/// The sidecar is invoked by its co-located application, so the call to <c>/Validate</c> is not the
/// request the SHR was signed over; the caller must supply the original method and URI (they cannot be
/// derived server-side). Only <c>Uri</c> and <c>Method</c> are populated: header (<c>h</c>) and body
/// (<c>b</c>) binding are not supported (the sidecar never receives the signed headers or body), and
/// query (<c>q</c>) binding, when enabled, is validated from the query component of the absolute
/// <c>original-uri</c>. So request headers and body do not participate in validation.
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

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.IdentityModel.Protocols.SignedHttpRequest;

namespace Microsoft.Identity.Web.Sidecar.Pop;

/// <summary>
/// Constants for the inbound Signed HTTP Request (SHR) Proof-of-Possession path: the "PoP"
/// Authorization scheme token and the request-line contract header names.
/// </summary>
internal static class PopConstants
{
    /// <summary>The Authorization scheme token for SHR PoP ("PoP"). Sourced from Microsoft.IdentityModel so it never drifts.</summary>
    public const string SchemeName = SignedHttpRequestConstants.AuthorizationHeaderSchemeName;

    /// <summary>Protocol label returned to the caller for a validated PoP request.</summary>
    public const string ProtocolName = "PoP";

    /// <summary>
    /// Name of the authorization policy applied to the <c>/Validate</c> endpoint. It accepts both the
    /// "Bearer" and "PoP" schemes so a single endpoint handles either credential without changing the
    /// global default scheme (which stays "Bearer").
    /// </summary>
    public const string ValidatePolicyName = "SidecarValidateBearerOrPoP";

    /// <summary>Header carrying the original (signed) request absolute URI.</summary>
    public const string OriginalUriHeaderName = "original-uri";

    /// <summary>Header carrying the original (signed) request HTTP method.</summary>
    public const string OriginalMethodHeaderName = "original-method";

    /// <summary>HttpContext.Items key holding the validated inner access token (a JsonWebToken).</summary>
    public const string ValidatedAccessTokenItemKey = "Sidecar.Pop.ValidatedAccessToken";
}

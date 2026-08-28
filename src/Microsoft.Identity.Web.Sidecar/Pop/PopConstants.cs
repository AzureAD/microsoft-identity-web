// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.IdentityModel.Protocols.SignedHttpRequest;

namespace Microsoft.Identity.Web.Sidecar.Pop;

/// <summary>
/// SPIKE (throwaway): constants for the inbound Signed HTTP Request (SHR) Proof-of-Possession path.
/// The "PoP" scheme token and the request-line header names mirror the MISE container contract
/// (ContainerShared/RequestHeaderNames.cs) so a future shared Envoy front-end stays wire-compatible.
/// </summary>
internal static class PopConstants
{
    /// <summary>The Authorization scheme token for SHR PoP ("PoP"). Sourced from Wilson so it never drifts.</summary>
    public const string SchemeName = SignedHttpRequestConstants.AuthorizationHeaderSchemeName;

    /// <summary>Protocol label returned to the caller for a validated PoP request.</summary>
    public const string ProtocolName = "PoP";

    /// <summary>
    /// Name of the authorization policy applied to the <c>/Validate</c> endpoint. The policy accepts
    /// BOTH the "Bearer" and "PoP" authentication schemes, so a single endpoint transparently handles
    /// either credential without changing the global default scheme (which stays "Bearer"). This keeps
    /// the change scoped to /Validate; every other endpoint is completely unaffected.
    /// </summary>
    public const string ValidatePolicyName = "SidecarValidateBearerOrPoP";

    // --- Request-line contract headers (identical to MISE ContainerShared/RequestHeaderNames.cs) ---

    /// <summary>Header carrying the original (signed) request absolute URI.</summary>
    public const string OriginalUriHeaderName = "original-uri";

    /// <summary>Header carrying the original (signed) request HTTP method.</summary>
    public const string OriginalMethodHeaderName = "original-method";

    /// <summary>Header carrying the original client IP (reserved; not used by m/u/p/ts validation).</summary>
    public const string OriginalIpAddressHeaderName = "x-forwarded-for";

    /// <summary>HttpContext.Items key holding the validated inner access token (a JsonWebToken).</summary>
    public const string ValidatedAccessTokenItemKey = "Sidecar.Pop.ValidatedAccessToken";
}

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Security.Claims;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Microsoft.Identity.Web.Sidecar.Pop;

/// <summary>Outcome of inbound SHR PoP validation.</summary>
internal sealed class ShrPopValidationResult
{
    private ShrPopValidationResult()
    {
    }

    public bool Succeeded { get; private init; }

    public string? Error { get; private init; }

    /// <summary>The validated inner (embedded) access token, present on success.</summary>
    public JsonWebToken? ValidatedAccessToken { get; private init; }

    /// <summary>The claims identity built from the validated inner access token, present on success.</summary>
    public ClaimsIdentity? ClaimsIdentity { get; private init; }

    public static ShrPopValidationResult Fail(string error) =>
        new() { Succeeded = false, Error = error };

    public static ShrPopValidationResult Success(JsonWebToken validatedAccessToken, ClaimsIdentity claimsIdentity) =>
        new() { Succeeded = true, ValidatedAccessToken = validatedAccessToken, ClaimsIdentity = claimsIdentity };
}

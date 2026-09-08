// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Extensions.Options;

namespace Microsoft.Identity.Web.Sidecar.Configuration;

/// <summary>
/// Validates <see cref="SidecarOptions"/> at startup. Registered with <c>ValidateOnStart</c> so an
/// invalid value fails the host boot instead of surfacing as a per-request failure.
/// </summary>
internal sealed class ValidateSidecarOptions : IValidateOptions<SidecarOptions>
{
    public ValidateOptionsResult Validate(string? name, SidecarOptions options)
    {
        // The identity model rejects a non-positive SignedHttpRequestLifetime when the validation
        // parameters are built, which would otherwise throw on the first PoP request. Bound it here so
        // the misconfiguration is caught at boot.
        if (options.PopValidation.SignedHttpRequestLifetime <= TimeSpan.Zero)
        {
            return ValidateOptionsResult.Fail(
                "Sidecar:PopValidation:SignedHttpRequestLifetime must be greater than zero.");
        }

        return ValidateOptionsResult.Success;
    }
}

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// Provides information about the effective authentication scheme. If passing null
    /// or string.Empty, this returns the default authentication scheme.
    /// </summary>
    public interface AuthenticationSchemeInformationProvider
    {
        /// <summary>
        /// Get the effective authentication scheme based on the provided authentication scheme.
        /// </summary>
        /// <param name="authenticationScheme">intended authentication scheme.</param>
        /// <returns>Effective authentication scheme (default authentication scheme if the intended
        /// authentication scheme is null or an empty string.</returns>
        string GetEffectiveAuthenticationScheme(string? authenticationScheme);
    }
}

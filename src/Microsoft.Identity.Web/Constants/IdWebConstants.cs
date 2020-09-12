// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// Commonly used constants for Microsoft Identity Web.
    /// </summary>
    public static class IDWebConstants
    {
        /// <summary>
        /// LoginHint.
        /// Represents the preferred_username claim in the ID token.
        /// </summary>
        public const string LoginHint = "loginHint";

        /// <summary>
        /// DomainHint.
        /// Determined by the tenant Id.
        /// </summary>
        public const string DomainHint = "domainHint";

        /// <summary>
        /// Claims.
        /// Determined from the signed-in user.
        /// </summary>
        public const string Claims = "claims";

        /// <summary>
        /// Bearer.
        /// Predominant type of access token used with OAuth 2.0.
        /// </summary>
        public const string Bearer = "Bearer";

        /// <summary>
        /// AzureAd.
        /// Configuration section name for AzureAd.
        /// </summary>
        public const string AzureAd = "AzureAd";

        /// <summary>
        /// AzureAdB2C.
        /// Configuration section name for AzureAdB2C.
        /// </summary>
        public const string AzureAdB2C = "AzureAdB2C";

        /// <summary>
        /// Scope.
        /// </summary>
        public const string Scope = "scope";
    }
}

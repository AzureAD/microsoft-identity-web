// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Globalization;
using System.Security.Authentication;
using System.Security.Claims;

namespace Microsoft.Identity.Web
{
    /*
     * Used by Microsoft.Identity.Web (ASP.NET Core adapter).
     * Any changes to this member (including removal) can cause runtime failures.
     * Treat as a public member.
     */

    /// <summary>
    /// Adds MSAL home-account identifier claims to a <see cref="ClaimsIdentity"/>.
    /// </summary>
    internal static class HomeAccountIdentifierClaims
    {
        /// <summary>
        /// Validates existing reserved claims and adds the <c>uid</c>/<c>utid</c> pair.
        /// </summary>
        /// <param name="identity">The identity to stamp. No-op when <see langword="null"/>.</param>
        /// <param name="uniqueObjectIdentifier">The home account object id (<c>uid</c>) from the redeemed token.</param>
        /// <param name="uniqueTenantIdentifier">The home account tenant id (<c>utid</c>) from the redeemed token.</param>
        /// <exception cref="AuthenticationException">
        /// Thrown when the identity carries a conflicting reserved <c>uid</c> or <c>utid</c> claim.
        /// </exception>
        public static void AddOrValidate(
            ClaimsIdentity? identity,
            string? uniqueObjectIdentifier,
            string? uniqueTenantIdentifier)
        {
            if (identity == null)
            {
                return;
            }

            if (uniqueTenantIdentifier != null)
            {
                Claim? existing = identity.FindFirst(c => c.Type == ClaimConstants.UniqueTenantIdentifier);
                if (existing != null && !string.Equals(uniqueTenantIdentifier, existing.Value, StringComparison.OrdinalIgnoreCase))
                {
                    throw new AuthenticationException(string.Format(CultureInfo.InvariantCulture, IDWebErrorMessage.InternalClaimDetected, ClaimConstants.UniqueTenantIdentifier));
                }
            }

            if (uniqueObjectIdentifier != null)
            {
                Claim? existing = identity.FindFirst(c => c.Type == ClaimConstants.UniqueObjectIdentifier);
                if (existing != null && !string.Equals(uniqueObjectIdentifier, existing.Value, StringComparison.OrdinalIgnoreCase))
                {
                    throw new AuthenticationException(string.Format(CultureInfo.InvariantCulture, IDWebErrorMessage.InternalClaimDetected, ClaimConstants.UniqueObjectIdentifier));
                }
            }

            if (uniqueTenantIdentifier != null && uniqueObjectIdentifier != null)
            {
                identity.AddClaim(new Claim(ClaimConstants.UniqueTenantIdentifier, uniqueTenantIdentifier));
                identity.AddClaim(new Claim(ClaimConstants.UniqueObjectIdentifier, uniqueObjectIdentifier));
            }
        }
    }
}

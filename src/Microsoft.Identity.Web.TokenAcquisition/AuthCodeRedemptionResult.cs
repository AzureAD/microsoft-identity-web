// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Abstractions;

namespace Microsoft.Identity.Web
{
    /*
     * Used by Microsoft.Identity.Web, Microsoft.Identity.Web.OWIN
     * Any changes to this member (including removal) can cause runtime failures.
     * Treat as a public member.
     */

    /// <summary>
    /// Result of redeeming an authorization code. Carries the <see cref="AcquireTokenResult"/>
    /// together with the home account identifier (object id and tenant id) taken from the token
    /// that was actually acquired, so that callers can stamp the account-identifier claims from a
    /// value that is consistent with the redeemed token.
    /// </summary>
    internal class AuthCodeRedemptionResult
    {
        public AuthCodeRedemptionResult(AcquireTokenResult result, string? homeObjectId, string? homeTenantId)
        {
            Result = result;
            HomeObjectId = homeObjectId;
            HomeTenantId = homeTenantId;
        }

        /// <summary>
        /// The token acquisition result returned to callers.
        /// </summary>
        public AcquireTokenResult Result { get; }

        /// <summary>
        /// Home account object identifier ("uid") from the redeemed token, or <see langword="null"/>
        /// when the redeemed result did not carry an account.
        /// </summary>
        public string? HomeObjectId { get; }

        /// <summary>
        /// Home account tenant identifier ("utid") from the redeemed token, or <see langword="null"/>
        /// when the redeemed result did not carry an account.
        /// </summary>
        public string? HomeTenantId { get; }
    }
}

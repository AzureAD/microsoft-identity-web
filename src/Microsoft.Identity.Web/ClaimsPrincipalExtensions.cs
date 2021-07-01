// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Security.Claims;

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// Extensions for <see cref="ClaimsPrincipal"/>.
    /// </summary>
    public static class ClaimsPrincipalExtensions
    {
        /// <summary>
        /// Gets the account identifier for an MSAL.NET account from a <see cref="ClaimsPrincipal"/>.
        /// </summary>
        /// <param name="claimsPrincipal">Claims principal.</param>
        /// <returns>A string corresponding to an account identifier as defined in <see cref="Microsoft.Identity.Client.AccountId.Identifier"/>.</returns>
        public static string? GetMsalAccountId(this ClaimsPrincipal claimsPrincipal)
        {
            if (claimsPrincipal == null)
            {
                throw new ArgumentNullException(nameof(claimsPrincipal));
            }

            string? uniqueObjectIdentifier = claimsPrincipal?.GetHomeObjectId();
            string? uniqueTenantIdentifier = claimsPrincipal?.GetHomeTenantId();

            if (!string.IsNullOrWhiteSpace(uniqueObjectIdentifier) && !string.IsNullOrWhiteSpace(uniqueTenantIdentifier))
            {
                // AAD pattern: {uid}.{utid}
                // B2C pattern: {uid}-{userFlow}.{utid} -> userFlow is included in the uid for B2C
                return $"{uniqueObjectIdentifier}.{uniqueTenantIdentifier}";
            }

            return null;
        }

        /// <summary>
        /// Gets the unique object ID associated with the <see cref="ClaimsPrincipal"/>.
        /// </summary>
        /// <param name="claimsPrincipal">The <see cref="ClaimsPrincipal"/> from which to retrieve the unique object ID.</param>
        /// <remarks>This method returns the object ID both in case the developer has enabled or not claims mapping.</remarks>
        /// <returns>Unique object ID of the identity, or <c>null</c> if it cannot be found.</returns>
        public static string? GetObjectId(this ClaimsPrincipal claimsPrincipal)
        {
            return GetClaimValue(claimsPrincipal, ClaimConstants.Oid, ClaimConstants.ObjectId);
        }

        /// <summary>
        /// Gets the Tenant ID associated with the <see cref="ClaimsPrincipal"/>.
        /// </summary>
        /// <param name="claimsPrincipal">The <see cref="ClaimsPrincipal"/> from which to retrieve the tenant ID.</param>
        /// <returns>Tenant ID of the identity, or <c>null</c> if it cannot be found.</returns>
        /// <remarks>This method returns the tenant ID both in case the developer has enabled or not claims mapping.</remarks>
        public static string? GetTenantId(this ClaimsPrincipal claimsPrincipal)
        {
            return GetClaimValue(claimsPrincipal, ClaimConstants.Tid, ClaimConstants.TenantId);
        }

        /// <summary>
        /// Gets the login-hint associated with a <see cref="ClaimsPrincipal"/>.
        /// </summary>
        /// <param name="claimsPrincipal">Identity for which to complete the login-hint.</param>
        /// <returns>The login hint for the identity, or <c>null</c> if it cannot be found.</returns>
        public static string? GetLoginHint(this ClaimsPrincipal claimsPrincipal)
        {
            return GetDisplayName(claimsPrincipal);
        }

        /// <summary>
        /// Gets the domain-hint associated with an identity.
        /// </summary>
        /// <param name="claimsPrincipal">Identity for which to compute the domain-hint.</param>
        /// <returns> The domain hint for the identity, or <c>null</c> if it cannot be found.</returns>
        public static string? GetDomainHint(this ClaimsPrincipal claimsPrincipal)
        {
            if (claimsPrincipal == null)
            {
                throw new ArgumentNullException(nameof(claimsPrincipal));
            }

            string? tenantId = GetTenantId(claimsPrincipal);
            string? domainHint = string.IsNullOrWhiteSpace(tenantId)
                ? null
                : tenantId!.Equals(Constants.MsaTenantId, StringComparison.OrdinalIgnoreCase) ? Constants.Consumers : Constants.Organizations;

            return domainHint;
        }

        /// <summary>
        /// Get the display name for the signed-in user, from the <see cref="ClaimsPrincipal"/>.
        /// </summary>
        /// <param name="claimsPrincipal">Claims about the user/account.</param>
        /// <returns>A string containing the display name for the user, as determined by Azure AD (v1.0) and Microsoft identity platform (v2.0) tokens,
        /// or <c>null</c> if the claims cannot be found.</returns>
        /// <remarks>See https://docs.microsoft.com/azure/active-directory/develop/id-tokens#payload-claims. </remarks>
        public static string? GetDisplayName(this ClaimsPrincipal claimsPrincipal)
        {
            return GetClaimValue(
                claimsPrincipal,
                ClaimConstants.PreferredUserName,
                ClaimsIdentity.DefaultNameClaimType,
                ClaimConstants.Name);
        }

        /// <summary>
        /// Gets the user flow ID associated with the <see cref="ClaimsPrincipal"/>.
        /// </summary>
        /// <param name="claimsPrincipal">The <see cref="ClaimsPrincipal"/> from which to retrieve the user flow ID.</param>
        /// <returns>User flow ID of the identity, or <c>null</c> if it cannot be found.</returns>
        public static string? GetUserFlowId(this ClaimsPrincipal claimsPrincipal)
        {
            return GetClaimValue(claimsPrincipal, ClaimConstants.Tfp, ClaimConstants.UserFlow);
        }

        /// <summary>
        /// Gets the Home Object ID associated with the <see cref="ClaimsPrincipal"/>.
        /// </summary>
        /// <param name="claimsPrincipal">The <see cref="ClaimsPrincipal"/> from which to retrieve the sub claim.</param>
        /// <returns>Home Object ID (sub) of the identity, or <c>null</c> if it cannot be found.</returns>
        public static string? GetHomeObjectId(this ClaimsPrincipal claimsPrincipal)
        {
            return GetClaimValue(claimsPrincipal, ClaimConstants.UniqueObjectIdentifier);
        }

        /// <summary>
        /// Gets the Home Tenant ID associated with the <see cref="ClaimsPrincipal"/>.
        /// </summary>
        /// <param name="claimsPrincipal">The <see cref="ClaimsPrincipal"/> from which to retrieve the sub claim.</param>
        /// <returns>Home Tenant ID (sub) of the identity, or <c>null</c> if it cannot be found.</returns>
        public static string? GetHomeTenantId(this ClaimsPrincipal claimsPrincipal)
        {
            return GetClaimValue(claimsPrincipal, ClaimConstants.UniqueTenantIdentifier);
        }

        /// <summary>
        /// Gets the NameIdentifierId associated with the <see cref="ClaimsPrincipal"/>.
        /// </summary>
        /// <param name="claimsPrincipal">The <see cref="ClaimsPrincipal"/> from which to retrieve the <c>NameIdentifierId</c> claim.</param>
        /// <returns>Name identifier ID of the identity, or <c>null</c> if it cannot be found.</returns>
        public static string? GetNameIdentifierId(this ClaimsPrincipal claimsPrincipal)
        {
            return GetClaimValue(claimsPrincipal, ClaimConstants.NameIdentifierId);
        }

        internal static string? GetClaimValue(ClaimsPrincipal? claimsPrincipal, params string[] claimNames)
        {
            if (claimsPrincipal == null)
            {
                throw new ArgumentNullException(nameof(claimsPrincipal));
            }

            for (var i = 0; i < claimNames.Length; i++)
            {
                var currentValue = claimsPrincipal.FindFirstValue(claimNames[i]);
                if (!string.IsNullOrEmpty(currentValue))
                {
                    return currentValue;
                }
            }

            return null;
        }

#if NET472 || NET462
        private static string? FindFirstValue(this ClaimsPrincipal claimsPrincipal, string type)
        {
             return claimsPrincipal.FindFirst(type)?.Value;
        }
#endif
    }
}

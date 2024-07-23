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
        /// New Object id claim: "oid".
        /// </summary>
        private const string Oid = "oid";

        /// <summary>
        /// Old Object Id claim: http://schemas.microsoft.com/identity/claims/objectidentifier.
        /// </summary>
        private const string ObjectId = "http://schemas.microsoft.com/identity/claims/objectidentifier";

        /// <summary>
        /// Old TenantId claim: "http://schemas.microsoft.com/identity/claims/tenantid".
        /// </summary>
        private const string TenantId = "http://schemas.microsoft.com/identity/claims/tenantid";

        /// <summary>
        /// New Tenant Id claim: "tid".
        /// </summary>
        private const string Tid = "tid";

        /// <summary>
        /// PreferredUserName: "preferred_username".
        /// </summary>
        private const string PreferredUserName = "preferred_username";

        /// <summary>
        /// Name claim: "name".
        /// </summary>
        private const string Name = "name";

        /// <summary>
        /// UserFlow claim: "http://schemas.microsoft.com/claims/authnclassreference".
        /// </summary>
        private const string UserFlow = "http://schemas.microsoft.com/claims/authnclassreference";

        /// <summary>
        /// Tfp claim: "tfp".
        /// </summary>
        private const string Tfp = "tfp";

        /// <summary>
        /// UniqueObjectIdentifier: "uid".
        /// Home Object Id.
        /// </summary>
        private const string UniqueObjectIdentifier = "uid";

        /// <summary>
        /// UniqueTenantIdentifier: "utid".
        /// Home Tenant Id.
        /// </summary>
        private const string UniqueTenantIdentifier = "utid";

        /// <summary>
        /// Name Identifier ID claim: "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier".
        /// </summary>
        private const string NameIdentifierId = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier";

        /// <summary>
        /// Issuer claim: "iss".
        /// </summary>
        private const string Iss = "iss";

        /// <summary>
        /// Identity Provider claim: "idp".
        /// </summary>
        private const string Idp = "idp";

        /// <summary>
        /// Old Identity Provider claim: "http://schemas.microsoft.com/identity/claims/identityprovider".
        /// </summary>
        private const string IdentityProvider = "http://schemas.microsoft.com/identity/claims/identityprovider";

        private const string MsaTenantId = "9188040d-6c67-4c5b-b112-36a304b66dad";
        private const string Consumers = "consumers";
        private const string Organizations = "organizations";

        /// <summary>
        /// Gets the account identifier for an MSAL.NET account from a <see cref="ClaimsPrincipal"/>.
        /// </summary>
        /// <param name="claimsPrincipal">Claims principal.</param>
        /// <returns>A string corresponding to an account identifier as defined in <see cref="Microsoft.Identity.Client.AccountId.Identifier"/>.</returns>
        public static string? GetMsalAccountId(this ClaimsPrincipal claimsPrincipal)
        {
            _ = Throws.IfNull(claimsPrincipal);

            string? uniqueObjectIdentifier = claimsPrincipal.GetHomeObjectId();
            string? uniqueTenantIdentifier = claimsPrincipal.GetHomeTenantId();

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
            return GetClaimValue(claimsPrincipal, Oid, ObjectId);
        }

        /// <summary>
        /// Gets the Tenant ID associated with the <see cref="ClaimsPrincipal"/>.
        /// </summary>
        /// <param name="claimsPrincipal">The <see cref="ClaimsPrincipal"/> from which to retrieve the tenant ID.</param>
        /// <returns>Tenant ID of the identity, or <c>null</c> if it cannot be found.</returns>
        /// <remarks>This method returns the tenant ID both in case the developer has enabled or not claims mapping.</remarks>
        public static string? GetTenantId(this ClaimsPrincipal claimsPrincipal)
        {
            return GetClaimValue(claimsPrincipal, Tid, TenantId);
        }

        /// <summary>
        /// Gets the Identity Provider associated with the <see cref="ClaimsPrincipal"/>.
        /// </summary>
        /// <param name="claimsPrincipal">The <see cref="ClaimsPrincipal"/> from which to retrieve the tenant ID.</param>
        /// <returns>Tenant Identity Provider used to log in the user, or <c>null</c> if it cannot be found.</returns>
        /// <remarks>This method returns the Identity Provider both in case the developer has enabled or not claims mapping, and if none is present, it returns the Issuer.</remarks>
        public static string? GetIdentityProvider(this ClaimsPrincipal claimsPrincipal)
        {
            return GetClaimValue(claimsPrincipal, Idp, IdentityProvider, Iss);
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
            _ = Throws.IfNull(claimsPrincipal);

            string? tenantId = GetTenantId(claimsPrincipal);
            string? domainHint = string.IsNullOrWhiteSpace(tenantId)
                ? null
                : tenantId!.Equals(MsaTenantId, StringComparison.OrdinalIgnoreCase) ? Consumers : Organizations;

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
                PreferredUserName,
                ClaimsIdentity.DefaultNameClaimType,
                Name);
        }

        /// <summary>
        /// Gets the user flow ID associated with the <see cref="ClaimsPrincipal"/>.
        /// </summary>
        /// <param name="claimsPrincipal">The <see cref="ClaimsPrincipal"/> from which to retrieve the user flow ID.</param>
        /// <returns>User flow ID of the identity, or <c>null</c> if it cannot be found.</returns>
        public static string? GetUserFlowId(this ClaimsPrincipal claimsPrincipal)
        {
            return GetClaimValue(claimsPrincipal, Tfp, UserFlow);
        }

        /// <summary>
        /// Gets the Home Object ID associated with the <see cref="ClaimsPrincipal"/>.
        /// </summary>
        /// <param name="claimsPrincipal">The <see cref="ClaimsPrincipal"/> from which to retrieve the sub claim.</param>
        /// <returns>Home Object ID (sub) of the identity, or <c>null</c> if it cannot be found.</returns>
        public static string? GetHomeObjectId(this ClaimsPrincipal claimsPrincipal)
        {
            return GetClaimValue(claimsPrincipal, UniqueObjectIdentifier);
        }

        /// <summary>
        /// Gets the Home Tenant ID associated with the <see cref="ClaimsPrincipal"/>.
        /// </summary>
        /// <param name="claimsPrincipal">The <see cref="ClaimsPrincipal"/> from which to retrieve the sub claim.</param>
        /// <returns>Home Tenant ID (sub) of the identity, or <c>null</c> if it cannot be found.</returns>
        public static string? GetHomeTenantId(this ClaimsPrincipal claimsPrincipal)
        {
            return GetClaimValue(claimsPrincipal, UniqueTenantIdentifier);
        }

        /// <summary>
        /// Gets the NameIdentifierId associated with the <see cref="ClaimsPrincipal"/>.
        /// </summary>
        /// <param name="claimsPrincipal">The <see cref="ClaimsPrincipal"/> from which to retrieve the <c>NameIdentifierId</c> claim.</param>
        /// <returns>Name identifier ID of the identity, or <c>null</c> if it cannot be found.</returns>
        public static string? GetNameIdentifierId(this ClaimsPrincipal claimsPrincipal)
        {
            return GetClaimValue(claimsPrincipal, NameIdentifierId);
        }

        private static string? GetClaimValue(ClaimsPrincipal? claimsPrincipal, params string[] claimNames)
        {
            _ = Throws.IfNull(claimsPrincipal);

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

        private static string? FindFirstValue(this ClaimsPrincipal claimsPrincipal, string type)
        {
             return claimsPrincipal.FindFirst(type)?.Value;
        }
    }
}

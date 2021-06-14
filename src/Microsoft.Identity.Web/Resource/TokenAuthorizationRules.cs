// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;

namespace Microsoft.Identity.Web.Resource
{
    /// <summary>
    /// Rules to authorize an access token in a web API, depending on the
    /// values of the <see cref="ITokenAuthorizationOptions"/>.
    /// </summary>
    public class TokenAuthorizationRules
    {
        /// <summary>
        /// Validates the authorization of the token.
        /// </summary>
        /// <param name="tokenAuthorizationOptions">Merged options to direct the authorization.</param>
        /// <param name="claimsPrincipal">Claims in the token.</param>
        internal static void ValidateApiAuthorization(ITokenAuthorizationOptions tokenAuthorizationOptions, ClaimsPrincipal claimsPrincipal)
        {
            // Does the web API disallow app only tokens?
            if (!tokenAuthorizationOptions.ApiAllowsAppOnlyTokens)
            {
                VerifyClaimEquals(claimsPrincipal, ClaimConstants.Idtyp, "app", IDWebErrorMessage.ApiAllowsAppOnlyTokensCantBeComputed, IDWebErrorMessage.ApiDoesNotAllowsAppOnlyTokens);
            }

            // Does the web API disallow guest accounts?
            if (!tokenAuthorizationOptions.ApiAllowsGuestAccounts)
            {
                VerifyClaimEquals(claimsPrincipal, ClaimConstants.Acct, "1", IDWebErrorMessage.ApiAllowsGuestAccountsCantBeComputed, IDWebErrorMessage.ApiDoesNotAllowsGuestAccounts);
            }

            // Does the web API demand specific tenants?
            if (tokenAuthorizationOptions.AllowedTenantIds != null)
            {
                VerifyClaimIsInAcceptedValues(
                    claimsPrincipal,
                    new[] { ClaimConstants.Tid, ClaimConstants.TenantId },
                    tokenAuthorizationOptions.AllowedTenantIds,
                    IDWebErrorMessage.ApiAllowedTenantsCantBeComputed,
                    IDWebErrorMessage.ApiTenantNotAllowed);
            }

            // Does the web API demand specific clients (oid)
            if (tokenAuthorizationOptions.AllowedClientApplications != null)
            {
                VerifyClaimIsInAcceptedValues(
                    claimsPrincipal,
                    new[] { ClaimConstants.Oid, ClaimConstants.ObjectId },
                    tokenAuthorizationOptions.AllowedClientApplications,
                    IDWebErrorMessage.ApiAllowedClientApplicationsCantBeComputed,
                    IDWebErrorMessage.ApiClientApplicationNotAllowed);
            }
        }

        /// <summary>
        /// Verify the value of a claim.
        /// </summary>
        /// <param name="claimsPrincipal">Claims.</param>
        /// <param name="claimName">Name (type) of the claim to verify.</param>
        /// <param name="expectedValue">Expected value.</param>
        /// <param name="messageWhenClaimsNotFound">Error message thrown when the claim is not found.</param>
        /// <param name="messageWhenUnexpectedClaimValue">Error message thrown when claim value is not the expected one.</param>
        private static void VerifyClaimEquals(
            ClaimsPrincipal claimsPrincipal,
            string claimName,
            string expectedValue,
            string messageWhenClaimsNotFound,
            string messageWhenUnexpectedClaimValue)
        {
            string? claimValue = ClaimsPrincipalExtensions.GetClaimValue(claimsPrincipal, claimName);
            if (claimValue == null)
            {
                throw new UnauthorizedAccessException(messageWhenClaimsNotFound);
            }

            if (string.Equals(claimValue, expectedValue, StringComparison.OrdinalIgnoreCase))
            {
                throw new UnauthorizedAccessException(messageWhenUnexpectedClaimValue);
            }
        }

        /// <summary>
        /// Verify the value of a claim.
        /// </summary>
        /// <param name="claimsPrincipal">Claims.</param>
        /// <param name="possibleClaimNames">Names (types) of the claims to verify.</param>
        /// <param name="acceptedValues">Expected values.</param>
        /// <param name="messageWhenClaimsNotFound">Error message thrown when the claim is not found.</param>
        /// <param name="messageWhenUnexpectedClaimValue">Error message thrown when claim value is not the expected one.</param>
        private static void VerifyClaimIsInAcceptedValues(
            ClaimsPrincipal claimsPrincipal,
            string[] possibleClaimNames,
            IEnumerable<string> acceptedValues,
            string messageWhenClaimsNotFound,
            string messageWhenUnexpectedClaimValue)
        {
            string? claimValue = ClaimsPrincipalExtensions.GetClaimValue(claimsPrincipal, possibleClaimNames);
            if (claimValue == null)
            {
                throw new UnauthorizedAccessException(messageWhenClaimsNotFound);
            }

            if (acceptedValues.Contains(claimValue, StringComparer.OrdinalIgnoreCase))
            {
                throw new UnauthorizedAccessException(messageWhenUnexpectedClaimValue);
            }
        }
    }
}

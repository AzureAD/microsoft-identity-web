// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Security.Claims;
using Microsoft.Identity.Client;

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// Extension methods for <see cref="IAccount"/>.
    /// </summary>
    public static class AccountExtensions
    {
        /// <summary>
        /// Creates the <see cref="ClaimsPrincipal"/> from the values found
        /// in an <see cref="IAccount"/>.
        /// </summary>
        /// <param name="account">The <see cref="IAccount"/> instance.</param>
        /// <returns>A <see cref="ClaimsPrincipal"/> built from <see cref="IAccount"/>.</returns>
        public static ClaimsPrincipal ToClaimsPrincipal(this IAccount account)
        {
            if (account == null)
            {
                throw new ArgumentNullException(nameof(account));
            }

            ClaimsIdentity identity = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.Upn, account.Username),
                });

            if (!string.IsNullOrEmpty(account.HomeAccountId?.ObjectId))
            {
                identity.AddClaim(new Claim(ClaimConstants.Oid, account.HomeAccountId.ObjectId));
            }

            if (!string.IsNullOrEmpty(account.HomeAccountId?.TenantId))
            {
                identity.AddClaim(new Claim(ClaimConstants.Tid, account.HomeAccountId.TenantId));
            }

            return new ClaimsPrincipal(identity);
        }
    }
}

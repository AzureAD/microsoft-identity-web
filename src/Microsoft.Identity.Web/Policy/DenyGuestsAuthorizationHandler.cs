// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// Handler for the <see cref="DenyGuestsAuthorizationRequirement"/>.
    /// Will prioritize checking the "acct" claim. If it is not present, it will check the "iss" and "idp" claims.
    /// </summary>
    internal class DenyGuestsAuthorizationsHandler : AuthorizationHandler<DenyGuestsAuthorizationRequirement>
    {
        /// <summary>
        /// Account status claim: "acct"
        /// "0" indicates the user is a member of the tenant.
        /// "1" indicates the user is a guest of the tenant.
        /// </summary>
        private const string Acct = "acct";

        /// <summary>
        /// Tenant Member value of the Account Status claim: "0"
        /// </summary>
        private const string TenantMember = "0";

        /// <summary>
        /// Issuer claim: "iss"
        /// </summary>
        private const string Iss = "iss";

        /// <summary>
        /// Makes a decision if authorization is allowed based on a specific requirement.
        /// </summary>
        /// <param name="context">AuthorizationHandlerContext.</param>
        /// <param name="requirement">Deny Guests authorization requirement.</param>
        /// <returns>Task.</returns>
        protected override Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            DenyGuestsAuthorizationRequirement requirement)
        {
            _ = Throws.IfNull(context);

            _ = Throws.IfNull(requirement);

            // acct is an optional claim
            // if it is present, it dictates wether the user is a guest or not
            var acct = context.User.FindFirstValue(Acct);

            if (!string.IsNullOrEmpty(acct))
            {
                if (acct == TenantMember)
                {
                    context.Succeed(requirement);
                }

                return Task.CompletedTask;
            }

            // if acct is not present
            // we can use the iss and idp claim to determine if the user is a guest
            var iss = context.User.FindFirstValue(Iss);
            var idp = context.User.GetIdentityProvider();

            if (!string.IsNullOrEmpty(iss) && iss == idp)
            {
                context.Succeed(requirement);
                return Task.CompletedTask;
            }

            return Task.CompletedTask;
        }
    }
}

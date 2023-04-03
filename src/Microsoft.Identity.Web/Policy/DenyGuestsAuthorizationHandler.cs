// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace Microsoft.Identity.Web
{
    internal class DenyGuestsAuthorizationsHandler : AuthorizationHandler<DenyGuestsAuthorizationRequirement>
    {
        private const string Acct = "acct";
        private const string TenantMember = "0";

        private const string Iss = "iss";

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

            if (iss == idp)
            {
                context.Succeed(requirement);
                return Task.CompletedTask;
            }

            return Task.CompletedTask;
        }
    }
}

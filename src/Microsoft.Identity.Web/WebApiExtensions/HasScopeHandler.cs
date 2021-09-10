// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace Microsoft.Identity.Web
{
    /// <summary>
    ///  Authorizer handler which needs to be called for the ScopeAuthorizationRequirement requirement type.
    /// </summary>
    internal class HasScopeHandler : AuthorizationHandler<ScopeAuthorizationRequirement>
    {
        /// <summary>
        /// Makes a decision if authorization is allowed based on the scope requirements specified.
        /// </summary>
        /// <param name="context">The authorization context.</param>
        /// <param name="requirement">The requirement to evaluate.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, ScopeAuthorizationRequirement requirement)
        {
            if (context.User != null && requirement.AllowedValues != null)
            {
                Claim? scopeClaim = context.User.FindFirst(ClaimConstants.Scp);

                // Fallback to Scope claim name
                if (scopeClaim == null)
                {
                    scopeClaim = context.User.FindFirst(ClaimConstants.Scope);
                }

                if (scopeClaim != null && scopeClaim.Value.Split(' ').Intersect(requirement.AllowedValues).Any())
                {
                    context.Succeed(requirement);
                }
            }

            return Task.CompletedTask;
        }
    }
}

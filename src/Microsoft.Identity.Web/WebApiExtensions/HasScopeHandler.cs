// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Identity.Web.Resource;

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
            // The resource is either the HttpContext or the Endpoint directly when used with the 
            // authorization middleware
            var endpoint = context.Resource switch
            {
                HttpContext httpContext => httpContext.GetEndpoint(),
                Endpoint ep => ep,
                _ => null,
            };
            var data = endpoint?.Metadata.GetMetadata<IAuthRequiredScopeMetadata>();

            var scopes = requirement.AllowedValues ?? data?.AcceptedScopes;

            // Can't determine what to do without scope metadata, so proceed
            if (scopes is null)
            {
                context.Succeed(requirement);
                return Task.CompletedTask;
            }

            Claim? scopeClaim = context.User.FindFirst(ClaimConstants.Scp) ?? context.User.FindFirst(ClaimConstants.Scope);

            if (scopeClaim is null)
            {
                return Task.CompletedTask;
            }

            // REVIEW: Remove the allocations here
            if (scopeClaim != null && scopeClaim.Value.Split(' ').Intersect(scopes).Any())
            {
                context.Succeed(requirement);
                return Task.CompletedTask;
            }

            return Task.CompletedTask;
        }
    }
}

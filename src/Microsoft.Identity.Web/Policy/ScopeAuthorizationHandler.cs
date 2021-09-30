// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Identity.Web
{
    /// <summary>
    ///  Scope authorization handler that needs to be called for a specific requirement type.
    ///  In this case, <see cref="ScopeAuthorizationRequirement"/>.
    /// </summary>
    internal class ScopeAuthorizationHandler : AuthorizationHandler<ScopeAuthorizationRequirement>
    {
        private readonly IConfiguration _configuration;

        /// <summary>
        /// Constructor for the scope authorization handler, which takes a configuration.
        /// </summary>
        /// <param name="configuration">Configuration.</param>
        public ScopeAuthorizationHandler(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        /// <summary>
        ///  Makes a decision if authorization is allowed based on a specific requirement.
        /// </summary>
        /// <param name="context">AuthorizationHandlerContext.</param>
        /// <param name="requirement">Scope authorization requirement.</param>
        /// <returns>Task.</returns>
        protected override Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            ScopeAuthorizationRequirement requirement)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (requirement is null)
            {
                throw new ArgumentNullException(nameof(requirement));
            }

            // The resource is either the HttpContext or the Endpoint directly when used with the
            // authorization middleware
            var endpoint = context.Resource switch
            {
                HttpContext httpContext => httpContext.GetEndpoint(),
                Endpoint ep => ep,
                _ => null,
            };

            var data = endpoint?.Metadata.GetMetadata<IAuthRequiredScopeMetadata>();

            IEnumerable<string>? scopes = null;
            if (requirement.RequiredScopesConfigurationKey != null)
            {
                scopes = _configuration.GetValue<string>(requirement.RequiredScopesConfigurationKey)?.Split(' ');
            }

            if (scopes is null)
            {
                scopes = requirement.AllowedValues ?? data?.AcceptedScope;
            }

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

            if (scopeClaim != null && scopeClaim.Value.Split(' ').Intersect(scopes).Any())
            {
                context.Succeed(requirement);
                return Task.CompletedTask;
            }

            return Task.CompletedTask;
        }
    }
}

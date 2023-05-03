// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

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
            _ = Throws.IfNull(context);

            _ = Throws.IfNull(requirement);

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

            var configurationKey = requirement.RequiredScopesConfigurationKey ?? data?.RequiredScopesConfigurationKey;

            if (configurationKey != null)
            {
                scopes = _configuration[configurationKey]?.Split(' ');
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

            var scopeClaims = context.User.FindAll(ClaimConstants.Scp)
              .Union(context.User.FindAll(ClaimConstants.Scope))
              .ToList();

            if (!scopeClaims.Any())
            {
                return Task.CompletedTask;
            }

            var hasScope = scopeClaims.SelectMany(s => s.Value.Split(' ')).Intersect(scopes).Any();

            if (hasScope)
            {
                context.Succeed(requirement);
                return Task.CompletedTask;
            }

            return Task.CompletedTask;
        }
    }
}

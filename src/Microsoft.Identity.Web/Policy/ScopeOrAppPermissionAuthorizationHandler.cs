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
using Microsoft.Extensions.Options;

namespace Microsoft.Identity.Web
{
    /// <summary>
    ///  Scope or app permission authorization handler that needs to be called for a specific requirement type.
    ///  In this case, <see cref="ScopeOrAppPermissionAuthorizationRequirement"/>.
    /// </summary>
    internal class ScopeOrAppPermissionAuthorizationHandler : AuthorizationHandler<ScopeOrAppPermissionAuthorizationRequirement>
    {
        private readonly IConfiguration _configuration;

        /// <summary>
        /// Constructor for the scope authorization handler, which takes a configuration.
        /// </summary>
        /// <param name="configuration">Configuration.</param>
        public ScopeOrAppPermissionAuthorizationHandler(IConfiguration configuration)
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
            ScopeOrAppPermissionAuthorizationRequirement requirement)
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

            var data = endpoint?.Metadata.GetMetadata<IAuthRequiredScopeOrAppPermissionMetadata>();

            IEnumerable<string>? scopes = null;
            IEnumerable<string>? appPermissions = null;

            var scopeConfigurationKey = requirement.RequiredScopesConfigurationKey ?? data?.RequiredScopesConfigurationKey;

            if (scopeConfigurationKey != null)
            {
                scopes = _configuration[scopeConfigurationKey]?.Split(' ');
            }

            if (scopes is null)
            {
                scopes = requirement.ScopeAllowedValues ?? data?.AcceptedScope;
            }

            var appPermissionConfigurationKey = requirement.RequiredAppPermissionsConfigurationKey ?? data?.RequiredAppPermissionsConfigurationKey;

            if (appPermissionConfigurationKey != null)
            {
                appPermissions = _configuration[appPermissionConfigurationKey]?.Split(' ');
            }

            if (appPermissions is null)
            {
                appPermissions = requirement.AppPermissionAllowedValues ?? data?.AcceptedAppPermission;
            }

            // Can't determine what to do without scope or app permission metadata, so proceed
            if (scopes is null && appPermissions is null)
            {
                context.Succeed(requirement);
                return Task.CompletedTask;
            }

            var scopeClaims = context.User.FindAll(ClaimConstants.Scp)
              .Union(context.User.FindAll(ClaimConstants.Scope))
              .ToList();

           var appPermissionClaims = context.User.FindAll(ClaimConstants.Role)
              .Union(context.User.FindAll(ClaimConstants.Roles))
              .ToList();

            if (!scopeClaims.Any() && !appPermissionClaims.Any())
            {
                return Task.CompletedTask;
            }

            var hasScope = scopes != null && scopeClaims.SelectMany(s => s.Value.Split(' ')).Intersect(scopes).Any();

            var hasAppPermission = appPermissions != null && appPermissionClaims.SelectMany(s => s.Value.Split(' ')).Intersect(appPermissions).Any();

            if (hasScope || hasAppPermission)
            {
                context.Succeed(requirement);
                return Task.CompletedTask;
            }

            return Task.CompletedTask;
        }
    }
}

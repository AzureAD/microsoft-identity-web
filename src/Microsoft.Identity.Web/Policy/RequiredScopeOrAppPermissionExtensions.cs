// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// Extensions for building the required scope or app permission attribute during application startup.
    /// </summary>
    public static class RequiredScopeOrAppPermissionExtensions
    {
        /// <summary>
        /// This method adds support for the required scope or app permission attribute. It adds a default policy that
        /// adds a scope requirement or app permission requirement.
        /// This requirement looks for IAuthRequiredScopeOrAppPermissionMetadata on the current endpoint.
        /// </summary>
        /// <param name="services">The services being configured.</param>
        /// <returns>Services.</returns>
        public static IServiceCollection AddRequiredScopeOrAppPermissionAuthorization(this IServiceCollection services)
        {
            services.AddAuthorization();

            services.TryAddEnumerable(ServiceDescriptor.Singleton<IPostConfigureOptions<AuthorizationOptions>, RequireScopeOrAppPermissionOptions>());
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IAuthorizationHandler, ScopeOrAppPermissionAuthorizationHandler>());
            return services;
        }

        /// <summary>
        /// This method adds metadata to route endpoint to describe required scopes or app permissions. It's the imperative version of
        /// the [RequiredScopeOrAppPermission] attribute.
        /// </summary>
        /// <typeparam name="TBuilder">Class implementing <see cref="IEndpointConventionBuilder"/>.</typeparam>
        /// <param name="endpointConventionBuilder">To customize the endpoints.</param>
        /// <param name="scope">Scope.</param>
        /// <param name="appPermission">App permission.</param>
        /// <returns>Builder.</returns>
        public static TBuilder RequireScopeOrAppPermission<TBuilder>(this TBuilder endpointConventionBuilder, string[] scope, string[] appPermission)
            where TBuilder : IEndpointConventionBuilder
        {
            return endpointConventionBuilder.WithMetadata(new RequiredScopeOrAppPermissionMetadata(scope, appPermission));
        }

        private sealed class RequiredScopeOrAppPermissionMetadata : IAuthRequiredScopeOrAppPermissionMetadata
        {
            public RequiredScopeOrAppPermissionMetadata(string[] scope, string[] appPermission)
            {
                AcceptedScope = scope;
                AcceptedAppPermission = appPermission;
            }

            public string[]? AcceptedScope { get; }
            public string[]? AcceptedAppPermission { get; }

            public string? RequiredScopesConfigurationKey { get; }
            public string? RequiredAppPermissionsConfigurationKey { get; }
        }
    }
}

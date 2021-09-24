// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// Extensions for building the required scope attribute during application startup.
    /// </summary>
    public static class RequiredScopeExtensions
    {
        /// <summary>
        /// This method adds support for the required scope attribute. It adds a default policy that
        /// adds a scope requirement. This requirement looks for IAuthRequiredScopeMetadata on the current endpoint.
        /// </summary>
        /// <param name="services">The services being configured.</param>
        /// <returns>Services.</returns>
        public static IServiceCollection AddRequiredScopeAuthorization(this IServiceCollection services)
        {
            services.AddAuthorization();

            services.TryAddEnumerable(ServiceDescriptor.Singleton<IPostConfigureOptions<AuthorizationOptions>, RequireScopeOptions>());
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IAuthorizationHandler, ScopeAuthorizationHandler>());
            return services;
        }

        /// <summary>
        /// This method adds metadata to route endpoint to describe required scopes. It's the imperative version of
        /// the [RequiredScope] attribute.
        /// </summary>
        /// <typeparam name="TBuilder">Class implementing <see cref="IEndpointConventionBuilder"/>.</typeparam>
        /// <param name="endpointConventionBuilder">To customize the endpoints.</param>
        /// <param name="scope">Scope.</param>
        /// <returns>Builder.</returns>
        public static TBuilder RequireScope<TBuilder>(this TBuilder endpointConventionBuilder, params string[] scope)
            where TBuilder : IEndpointConventionBuilder
        {
            return endpointConventionBuilder.WithMetadata(new RequiredScopeMetadata(scope));
        }

        private sealed class RequiredScopeMetadata : IAuthRequiredScopeMetadata
        {
            public RequiredScopeMetadata(string[] scope)
            {
                AcceptedScope = scope;
            }

            public IEnumerable<string>? AcceptedScope { get; }

            public string? RequiredScopeConfigurationKey { get; }
        }
    }
}

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// Extensions for building the deny guest requirement during application startup.
    /// </summary>
    public static class DenyGuestsExtensions
    {
        /// <summary>
        /// This method adds support for the deny guests requirement.
        /// </summary>
        /// <param name="services">The services being configured.</param>
        /// <returns>Services.</returns>
        public static IServiceCollection AddDenyGuestsAuthorization(this IServiceCollection services)
        {
            services.AddAuthorization();
            
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IAuthorizationHandler, DenyGuestsAuthorizationsHandler>());

            return services;
        }
    }
}

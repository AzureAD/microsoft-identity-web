// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Identity.Web.TokenCacheProviders.InMemory
{
    /// <summary>
    /// Extension class used to add an in-memory token cache serializer to MSAL.
    /// </summary>
    public static class InMemoryTokenCacheProviderExtension
    {
        /// <summary>Adds both the app and per-user in-memory token caches.</summary>
        /// <param name="services">The services collection to add to.</param>
        /// <returns>the services (for chaining).</returns>
        public static IServiceCollection AddInMemoryTokenCaches(
            this IServiceCollection services)
        {
            _ = Throws.IfNull(services);

            services.AddMemoryCache();
            services.TryAddSingleton<IMsalTokenCacheProvider, MsalMemoryTokenCacheProvider>();
            return services;
        }
    }
}

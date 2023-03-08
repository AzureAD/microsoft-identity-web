// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Identity.Web.TokenCacheProviders.Distributed
{
    /// <summary>
    /// Extension class used to add distributed token cache serializer to MSAL.
    /// See https://aka.ms/ms-id-web/token-cache-serialization for details.
    /// </summary>
    public static class DistributedTokenCacheAdapterExtension
    {
        /// <summary>Adds the .NET Core distributed cache based app token cache to the service collection.</summary>
        /// <param name="services">The services collection to add to.</param>
        /// <returns>A <see cref="IServiceCollection"/> to chain.</returns>
        public static IServiceCollection AddDistributedTokenCaches(
            this IServiceCollection services)
        {
            _ = Throws.IfNull(services);

            services.AddDistributedMemoryCache();
            services.TryAddSingleton<IMsalTokenCacheProvider, MsalDistributedTokenCacheAdapter>();
            return services;
        }
    }
}

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Web.TokenCacheProviders;

namespace PerformanceTestService.EventSource
{
    public static class ServiceExtensions
    {
        public static IServiceCollection AddBenchmarkInMemoryTokenCaches(this IServiceCollection services)
        {
            RemoveExistingMemoryCache(services);
            services.AddSingleton<IMsalTokenCacheProvider, BenchmarkMsalMemoryTokenCacheProvider>();

            return services;
        }

        public static IServiceCollection AddBenchmarkDistributedTokenCaches(this IServiceCollection services)
        {
            RemoveExistingMemoryCache(services);
            services.AddSingleton<IMsalTokenCacheProvider, BenchmarkMsalDistributedTokenCacheAdapter>();

            return services;
        }

        private static void RemoveExistingMemoryCache(IServiceCollection services)
        {
            ServiceDescriptor msalMemoryCacheService = services.First(s => s.ServiceType == typeof(IMsalTokenCacheProvider));

            if (msalMemoryCacheService != null)
            {
                services.Remove(msalMemoryCacheService);
            }
        }
    }
}

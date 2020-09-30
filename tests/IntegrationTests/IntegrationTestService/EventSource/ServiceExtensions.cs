using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Web.TokenCacheProviders;

namespace IntegrationTestService.EventSource
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
            ServiceDescriptor msalMemoryCacheService = services.FirstOrDefault(s => s.ServiceType == typeof(IMsalTokenCacheProvider));

            if (msalMemoryCacheService != null)
            {
                services.Remove(msalMemoryCacheService);
            }
        }
    }
}

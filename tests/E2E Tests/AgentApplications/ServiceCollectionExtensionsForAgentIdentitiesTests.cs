// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.TokenCacheProviders.InMemory;

namespace AgentApplicationsTests
{
    public static class ServiceCollectionExtensionsForAgentIdentitiesTests
    {
        public static IServiceProvider ConfigureServicesForAgentIdentitiesTests(this IServiceCollection services)
        {
            services.AddSingleton(new ConfigurationBuilder().Build());
            services.AddTokenAcquisition(true);
            services.AddInMemoryTokenCaches();
            services.AddHttpClient();
            services.AddMicrosoftGraph();        // If you want to call Microsoft Graph
            services.AddAgentIdentities();
            return services.BuildServiceProvider();
        }
    }
}

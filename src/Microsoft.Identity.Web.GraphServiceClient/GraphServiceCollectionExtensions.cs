// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Linq;
using System.Net.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Graph;
using Microsoft.Identity.Abstractions;

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// Extensions methods on a MicrosoftIdentityAppCallingWebApiAuthenticationBuilder builder
    /// to add support to call Microsoft Graph.
    /// </summary>
    public static class GraphServiceCollectionExtensions
    {
        /// <summary>
        /// Add support to call Microsoft Graph. From a named option and a configuration section.
        /// </summary>
        /// <param name="services">Builder.</param>
        /// <returns>The service collection to chain.</returns>
        public static IServiceCollection AddMicrosoftGraph(this IServiceCollection services)
        {
            // Add token acquisition (as a convenience) if not already added.
            if (services.FirstOrDefault(s => s.ServiceType == typeof(ITokenAcquisition)) == null)
            {
                services.AddTokenAcquisition();
                services.AddHttpClient();
            }
            return services.AddMicrosoftGraph(options => { });
        }

        /// <summary>
        /// Add support to call Microsoft Graph. From a base Graph URL and a default scope.
        /// </summary>
        /// <param name="services">Builder.</param>
        /// <param name="configurationSection">Configuration section containing the Microsoft graph config.</param>
        /// <returns>The service collection to chain.</returns>
        public static IServiceCollection AddMicrosoftGraph(this IServiceCollection services, IConfiguration configurationSection)
        {
            return services.AddMicrosoftGraph(o => configurationSection.Bind(o));
        }

        /// <summary>
        /// Add support to call Microsoft Graph. From a base Graph URL and a default scope.
        /// </summary>
        /// <param name="services">Builder.</param>
        /// <param name="configureMicrosoftGraphOptions">Delegate to configure the graph service options</param>
        /// <returns>The service collection to chain.</returns>
        public static IServiceCollection AddMicrosoftGraph(this IServiceCollection services, Action<GraphServiceClientOptions> configureMicrosoftGraphOptions)
        {
            // https://docs.microsoft.com/en-us/dotnet/standard/microservices-architecture/implement-resilient-applications/use-httpclientfactory-to-implement-resilient-http-requests
            services.AddOptions<GraphServiceClientOptions>().Configure(configureMicrosoftGraphOptions);

            // Add the Graph Service client depending on the lifetime of ITokenAcquisition
            AddGraphServiceClient(services);
            return services;
        }

        internal /* for unit tests*/ static void AddGraphServiceClient(IServiceCollection services)
        {
            ServiceDescriptor? tokenAcquisitionService = services.FirstOrDefault(s => s.ServiceType == typeof(ITokenAcquisition));
            ServiceDescriptor? graphServiceClient = services.FirstOrDefault(s => s.ServiceType == typeof(GraphServiceClient));

            if (tokenAcquisitionService != null)
            {
                if (graphServiceClient != null)
                {
                    if (graphServiceClient.Lifetime != tokenAcquisitionService.Lifetime)
                    {
                        services.Remove(graphServiceClient);
                        AddGraphServiceClientWithLifetime(services, tokenAcquisitionService.Lifetime);
                    }
                }
                else
                {
                    AddGraphServiceClientWithLifetime(services, tokenAcquisitionService.Lifetime);
                }
            }
            else
            {
                services.AddScoped<GraphServiceClient, GraphServiceClient>(CreateGraphServiceClient);
            }
        }

        internal static /* for unit tests*/ void AddGraphServiceClientWithLifetime(IServiceCollection services, ServiceLifetime lifetime)
        {
            if (lifetime == ServiceLifetime.Singleton)
            {
                services.AddSingleton<GraphServiceClient, GraphServiceClient>(CreateGraphServiceClient);
            }
            else
            {
                services.AddScoped<GraphServiceClient, GraphServiceClient>(CreateGraphServiceClient);
            }
        }

        private static GraphServiceClient CreateGraphServiceClient(IServiceProvider serviceProvider)
        {
            var authorizationHeaderProvider = serviceProvider.GetRequiredService<IAuthorizationHeaderProvider>();
            var options = serviceProvider.GetRequiredService<IOptions<GraphServiceClientOptions>>();
            var microsoftGraphOptions = options.Value;
            if (microsoftGraphOptions.Scopes == null)
            {
                Throws.ArgumentNullException("scopes", IDWebErrorMessage.CalledApiScopesAreNull);
            }

            var httpClient = GraphClientFactory.Create();

            GraphServiceClient graphServiceClient = new(httpClient,
                new GraphAuthenticationProvider(authorizationHeaderProvider, microsoftGraphOptions), microsoftGraphOptions.BaseUrl);
            return graphServiceClient;
        }
    }
}

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Graph.Beta;
using Microsoft.Identity.Abstractions;

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// Extensions methods on a MicrosoftIdentityAppCallingWebApiAuthenticationBuilder builder
    /// to add support to call Microsoft Graph Beta.
    /// </summary>
    public static class GraphBetaServiceCollectionExtensions
    {
        /// <summary>
        /// Add support to call Microsoft Graph Beta. From a named option and a configuration section.
        /// </summary>
        /// <param name="services">Builder.</param>
        /// <returns>The service collection to chain.</returns>
        public static IServiceCollection AddMicrosoftGraphBeta(this IServiceCollection services)
        {
            // Add token acquisition (as a convenience) if not already added.
            if (services.FirstOrDefault(s => s.ServiceType == typeof(ITokenAcquisition)) == null)
            {
                services.AddTokenAcquisition();
                services.AddHttpClient();
            }
            return services.AddMicrosoftGraphBeta(options => { });
        }

        /// <summary>
        /// Add support to call Microsoft Graph Beta. From a base Graph URL and a default scope.
        /// </summary>
        /// <param name="services">Builder.</param>
        /// <param name="configurationSection">Configuration section containing the Microsoft graph config.</param>
        /// <returns>The service collection to chain.</returns>
#if NET8_0_OR_GREATER
        [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "Configuration binding with AddOptions<T>().Bind() uses source generators on .NET 8+")]
        [UnconditionalSuppressMessage("AOT", "IL3050:Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.", Justification = "Configuration binding with AddOptions<T>().Bind() uses source generators on .NET 8+")]
#endif
        public static IServiceCollection AddMicrosoftGraphBeta(this IServiceCollection services, IConfiguration configurationSection)
        {
#if NET8_0_OR_GREATER
            // For .NET 8+, use source generator-based binding for AOT compatibility
            services.AddOptions<GraphServiceClientOptions>()
                .Bind(configurationSection);
            return services.AddMicrosoftGraphBeta(_ => { }); // No-op, binding already done
#else
            return services.AddMicrosoftGraphBeta(o => configurationSection.Bind(o));
#endif
        }

        /// <summary>
        /// Add support to call Microsoft Graph Beta. From a base Graph URL and a default scope.
        /// </summary>
        /// <param name="services">Builder.</param>
        /// <param name="configureMicrosoftGraphOptions">Delegate to configure the graph service options</param>
        /// <returns>The service collection to chain.</returns>
        public static IServiceCollection AddMicrosoftGraphBeta(this IServiceCollection services, Action<GraphServiceClientOptions> configureMicrosoftGraphOptions)
        {
            // https://learn.microsoft.com/dotnet/standard/microservices-architecture/implement-resilient-applications/use-httpclientfactory-to-implement-resilient-http-requests
            services.AddOptions<GraphServiceClientOptions>().Configure(configureMicrosoftGraphOptions);

            // Add the Graph Service client depending on the lifetime of ITokenAcquisition
            AddGraphBetaServiceClient(services);
            return services;
        }

        internal /* for unit tests*/ static void AddGraphBetaServiceClient(IServiceCollection services)
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
                        AddGraphBetaServiceClientWithLifetime(services, tokenAcquisitionService.Lifetime);
                    }
                }
                else
                {
                    AddGraphBetaServiceClientWithLifetime(services, tokenAcquisitionService.Lifetime);
                }
            }
            else
            {
                services.AddScoped<GraphServiceClient, GraphServiceClient>(CreateGraphBetaServiceClient);
            }
        }

        internal static /* for unit tests*/ void AddGraphBetaServiceClientWithLifetime(IServiceCollection services, ServiceLifetime lifetime)
        {
            if (lifetime == ServiceLifetime.Singleton)
            {
                services.AddSingleton<GraphServiceClient, GraphServiceClient>(CreateGraphBetaServiceClient);
            }
            else
            {
                services.AddScoped<GraphServiceClient, GraphServiceClient>(CreateGraphBetaServiceClient);
            }
        }

        private static GraphServiceClient CreateGraphBetaServiceClient(IServiceProvider serviceProvider)
        {
                var authorizationHeaderProvider = serviceProvider.GetRequiredService<IAuthorizationHeaderProvider>();
                var options = serviceProvider.GetRequiredService<IOptions<GraphServiceClientOptions>>();
                var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
                var microsoftGraphOptions = options.Value;
                if (microsoftGraphOptions.Scopes == null)
                {
                    Throws.ArgumentNullException("scopes", IDWebErrorMessage.CalledApiScopesAreNull);
                }

                var httpClient = httpClientFactory.CreateClient("GraphServiceClientBeta");

                GraphServiceClient betaGraphServiceClient = new(httpClient,
                    new GraphAuthenticationProvider(authorizationHeaderProvider, microsoftGraphOptions), microsoftGraphOptions.BaseUrl);
                return betaGraphServiceClient;
        }
    }
}

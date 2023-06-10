// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Net.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Graph;
using Microsoft.Graph.Drives.Item.Items.Item.Workbook.Functions.Beta_Dist;
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
            services.AddTokenAcquisition();
            services.AddHttpClient();
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

            services.AddScoped<GraphServiceClient, GraphServiceClient>(serviceProvider =>
            {
                var authorizationHeaderProvider = serviceProvider.GetRequiredService<IAuthorizationHeaderProvider>();
                var options = serviceProvider.GetRequiredService<IOptions<GraphServiceClientOptions>>();
                var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
                var microsoftGraphOptions = options.Value;
                if (microsoftGraphOptions.Scopes == null)
                {
                    Throws.ArgumentNullException("scopes", IDWebErrorMessage.CalledApiScopesAreNull);
                }

                var httpClient = httpClientFactory.CreateClient("GraphServiceClient");

                GraphServiceClient graphServiceClient = new(httpClient,
                    new GraphAuthenticationProvider(authorizationHeaderProvider, microsoftGraphOptions), microsoftGraphOptions.BaseUrl);
                return graphServiceClient;
            });
            return services;
        }
    }

}

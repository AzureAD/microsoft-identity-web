// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Linq;
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
            services.AddTokenAcquisition();
            services.AddHttpClient();
            return services.AddMicrosoftGraph(options => { });
        }

        /// <summary>
        /// Add support to call Microsoft Graph. From a base Graph URL and a default scope.
        /// </summary>
        /// <param name="services">Builder.</param>
        /// <param name="configureMicrosoftGraphOptions">Delegate to configure the graph service options</param>
        /// <returns>The service collection to chain.</returns>
        public static IServiceCollection AddMicrosoftGraph(this IServiceCollection services, Action<MicrosoftGraphOptions> configureMicrosoftGraphOptions)
        {
            // https://learn.microsoft.com/dotnet/standard/microservices-architecture/implement-resilient-applications/use-httpclientfactory-to-implement-resilient-http-requests
            services.AddOptions<MicrosoftGraphOptions>().Configure(configureMicrosoftGraphOptions);

            services.AddScoped<GraphServiceClient, GraphServiceClient>(serviceProvider =>
            {
                var authorizationHeaderProvider = serviceProvider.GetRequiredService<IAuthorizationHeaderProvider>();
                var options = serviceProvider.GetRequiredService<IOptions<MicrosoftGraphOptions>>();

                var microsoftGraphOptions = options.Value;
                if (microsoftGraphOptions.Scopes == null)
                {
                    throw new ArgumentException(IDWebErrorMessage.CalledApiScopesAreNull);
                }

                string graphBaseUrl = microsoftGraphOptions.BaseUrl;
                string[] initialScopes = microsoftGraphOptions.Scopes.Split(' ');

                GraphServiceClient client = string.IsNullOrWhiteSpace(graphBaseUrl) ?
                            new GraphServiceClient(new TokenAcquisitionAuthenticationProvider(
                                authorizationHeaderProvider,
                                new TokenAcquisitionAuthenticationProviderOption() { Scopes = initialScopes.ToArray() })) :
                            new GraphServiceClient(graphBaseUrl,
                                new TokenAcquisitionAuthenticationProvider(
                                    authorizationHeaderProvider,
                                    new TokenAcquisitionAuthenticationProviderOption() { Scopes = initialScopes.ToArray() }));
                return client;
            });
            return services;
        }
    }

}

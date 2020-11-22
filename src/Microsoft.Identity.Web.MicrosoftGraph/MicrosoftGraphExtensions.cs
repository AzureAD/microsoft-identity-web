// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Graph;

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// Extensions methods on a MicrosoftIdentityAppCallingWebApiAuthenticationBuilder builder
    /// to add support to call Microsoft Graph.
    /// </summary>
    public static class MicrosoftGraphExtensions
    {
        /// <summary>
        /// Add support to call Microsoft Graph. From a named option and a configuration section.
        /// </summary>
        /// <param name="builder">Builder.</param>
        /// <param name="configurationSection">Configuration section.</param>
        /// <returns>The builder to chain.</returns>
        public static MicrosoftIdentityAppCallsWebApiAuthenticationBuilder AddMicrosoftGraph(
            this MicrosoftIdentityAppCallsWebApiAuthenticationBuilder builder,
            IConfigurationSection configurationSection)
        {
            return builder.AddMicrosoftGraph(
                options => configurationSection.Bind(options));
        }

        /// <summary>
        /// Add support to call Microsoft Graph. From a base Graph URL and a default scope.
        /// </summary>
        /// <param name="builder">Builder.</param>
        /// <param name="graphBaseUrl">Named instance of option.</param>
        /// <param name="defaultScopes">Configuration section.</param>
        /// <returns>The builder to chain.</returns>
        public static MicrosoftIdentityAppCallsWebApiAuthenticationBuilder AddMicrosoftGraph(
            this MicrosoftIdentityAppCallsWebApiAuthenticationBuilder builder,
            string graphBaseUrl = Constants.GraphBaseUrlV1,
            string defaultScopes = Constants.UserReadScope)
        {
            return builder.AddMicrosoftGraph(
                options =>
                {
                    options.BaseUrl = graphBaseUrl;
                    options.Scopes = defaultScopes;
                });
        }

        /// <summary>
        /// Add support to call Microsoft Graph. From a named options and a configuration method.
        /// </summary>
        /// <param name="builder">Builder.</param>
        /// <param name="configureMicrosoftGraphOptions">Method to configure the options.</param>
        /// <returns>The builder to chain.</returns>
        public static MicrosoftIdentityAppCallsWebApiAuthenticationBuilder AddMicrosoftGraph(
            this MicrosoftIdentityAppCallsWebApiAuthenticationBuilder builder,
            Action<MicrosoftGraphOptions> configureMicrosoftGraphOptions)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            // https://docs.microsoft.com/en-us/dotnet/standard/microservices-architecture/implement-resilient-applications/use-httpclientfactory-to-implement-resilient-http-requests
            builder.Services.AddOptions<MicrosoftGraphOptions>().Configure(configureMicrosoftGraphOptions);

            builder.Services.AddHttpClient();
            builder.Services.AddScoped<GraphServiceClient, GraphServiceClient>(serviceProvider =>
            {
                var tokenAquisitionService = serviceProvider.GetRequiredService<ITokenAcquisition>();
                var options = serviceProvider.GetRequiredService<IOptions<MicrosoftGraphOptions>>();

                var microsoftGraphOptions = options.Value;
                if (microsoftGraphOptions.Scopes == null)
                {
                    throw new ArgumentException(IDWebErrorMessage.CalledApiScopesAreNull);
                }

                string graphBaseUrl = microsoftGraphOptions.BaseUrl;
                string[] initialScopes = microsoftGraphOptions.Scopes.Split(' ');

                var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
                var httpClient = httpClientFactory.CreateClient();
                httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                GraphServiceClient client = new GraphServiceClient(httpClient)
                {
                    AuthenticationProvider = new TokenAcquisitionAuthenticationProvider(
                    tokenAquisitionService,
                        new TokenAcquisitionAuthenticationProviderOption() { Scopes = initialScopes.ToArray() })
                };

                if (!string.IsNullOrWhiteSpace(graphBaseUrl))
                {
                    httpClient.BaseAddress = new Uri(graphBaseUrl);
                    client.BaseUrl = graphBaseUrl;
                }

                return client;
            });
            return builder;
        }

        /// <summary>
        /// Add support to call Microsoft Graph.  
        /// </summary>
        /// <param name="builder">Builder.</param>
        /// <param name="graphServiceClientFactory">Function to create a GraphServiceClient.</param>
        /// <param name="initialScopes">Initial scopes.</param>
        /// <returns>The builder to chain.</returns>
        public static MicrosoftIdentityAppCallsWebApiAuthenticationBuilder AddMicrosoftGraph(
            this MicrosoftIdentityAppCallsWebApiAuthenticationBuilder builder,
            Func<IAuthenticationProvider, GraphServiceClient> graphServiceClientFactory, IEnumerable<string> initialScopes)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            builder.Services.AddScoped<GraphServiceClient, GraphServiceClient>(serviceProvider =>
            {
                ITokenAcquisition? tokenAquisitionService = serviceProvider.GetRequiredService<ITokenAcquisition>();

                return graphServiceClientFactory(new TokenAcquisitionAuthenticationProvider(
                    tokenAquisitionService,
                    new TokenAcquisitionAuthenticationProviderOption() { Scopes = initialScopes.ToArray() }));
            });
            return builder;
        }


        /// <summary>
        /// Add support to call Microsoft Graph.  
        /// </summary>
        /// <param name="builder">Builder.</param>
        /// <param name="graphServiceClientFactory">Function to create a GraphServiceClient.</param>
        /// <returns>The builder to chain.</returns>
        public static MicrosoftIdentityAppCallsWebApiAuthenticationBuilder AddMicrosoftGraphAppOnly(
            this MicrosoftIdentityAppCallsWebApiAuthenticationBuilder builder,
            Func<IAuthenticationProvider, GraphServiceClient> graphServiceClientFactory)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            builder.Services.AddScoped<GraphServiceClient, GraphServiceClient>(serviceProvider =>
            {
                ITokenAcquisition? tokenAquisitionService = serviceProvider.GetRequiredService<ITokenAcquisition>();

                return graphServiceClientFactory(new TokenAcquisitionAuthenticationProvider(
                    tokenAquisitionService,
                    new TokenAcquisitionAuthenticationProviderOption() { AppOnly = true }));
            });
            return builder;
        }
    }
}

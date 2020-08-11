// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Graph;

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// Extensions methods on a MicrososoftAppCallingWebApiAuthenticationBuilder builder
    /// to add support to call Microsoft Graph.
    /// </summary>
    public static class MicrosoftGraphServiceExtensions
    {
        /// <summary>
        /// Add support to calls Microsoft graph. From a named option and a configuration section.
        /// </summary>
        /// <param name="builder">Builder.</param>
        /// <param name="configurationSection">Configuraiton section.</param>
        /// <returns>The builder to chain.</returns>
        public static MicrosoftIdentityAppCallsWebApisAuthenticationBuilder AddMicrosoftGraphServiceClient(
            this MicrosoftIdentityAppCallsWebApisAuthenticationBuilder builder,
            IConfigurationSection configurationSection)
        {
            return builder.AddMicrosoftGraphServiceClient(
                options => configurationSection.Bind(options));
        }

        /// <summary>
        /// Add support to calls Microsoft graph. From a base graph Url and a default scope.
        /// </summary>
        /// <param name="builder">Builder.</param>
        /// <param name="graphBaseUrl">Named instance of option.</param>
        /// <param name="defaultScopes">Configuraiton section.</param>
        /// <returns>The builder to chain.</returns>
        public static MicrosoftIdentityAppCallsWebApisAuthenticationBuilder AddMicrosoftGraphServiceClient(
            this MicrosoftIdentityAppCallsWebApisAuthenticationBuilder builder,
            string graphBaseUrl = "https://graph.microsoft.com/v1.0",
            string defaultScopes = "user.read")
        {
            return builder.AddMicrosoftGraphServiceClient(
                options =>
                {
                    options.BaseUrl = graphBaseUrl;
                    options.InitialScopes = defaultScopes;
                });
        }

        /// <summary>
        /// Add support to calls Microsoft graph. From a named options and a configuraiton method.
        /// </summary>
        /// <param name="builder">Builder.</param>
        /// <param name="configureMicrosoftGraphOptions">Method to configure the options.</param>
        /// <returns>The builder to chain.</returns>
        public static MicrosoftIdentityAppCallsWebApisAuthenticationBuilder AddMicrosoftGraphServiceClient(
            this MicrosoftIdentityAppCallsWebApisAuthenticationBuilder builder,
            Action<MicrosoftGraphOptions> configureMicrosoftGraphOptions)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            // https://docs.microsoft.com/en-us/dotnet/standard/microservices-architecture/implement-resilient-applications/use-httpclientfactory-to-implement-resilient-http-requests
            builder.Services.AddOptions<MicrosoftGraphOptions>().Configure(configureMicrosoftGraphOptions);
            builder.Services.AddTokenAcquisition(true);

            builder.Services.AddSingleton<GraphServiceClient, GraphServiceClient>(serviceProvider =>
            {
                var tokenAquisitionService = serviceProvider.GetRequiredService<ITokenAcquisition>();
                var options = serviceProvider.GetRequiredService<IOptions<MicrosoftGraphOptions>>();

                var microsoftGraphOptions = options.Value;
                if (microsoftGraphOptions.InitialScopes == null)
                {
                    throw new ArgumentException("CalledApiScopes should not be null.");
                }

                string graphBaseUrl = microsoftGraphOptions.BaseUrl;
                string[] initialScopes = microsoftGraphOptions.InitialScopes.Split(' ');

                GraphServiceClient client = string.IsNullOrWhiteSpace(graphBaseUrl) ?
                            new GraphServiceClient(new TokenAcquisitionCredentialProvider(tokenAquisitionService, initialScopes)) :
                            new GraphServiceClient(graphBaseUrl, new TokenAcquisitionCredentialProvider(tokenAquisitionService, initialScopes));
                return client;
            });
            return builder;
        }
    }
}

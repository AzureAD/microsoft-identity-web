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
    /// Extensions methods on a MicrosoftIdentityAppCallingWebApiAuthenticationBuilder builder
    /// to add support to call Microsoft Graph.
    /// </summary>
    public static class MicrosoftGraphServiceExtensions
    {
        /// <summary>
        /// Add support to call Microsoft Graph. From a named option and a configuration section.
        /// </summary>
        /// <param name="builder">Builder.</param>
        /// <param name="configurationSection">Configuration section.</param>
        /// <returns>The builder to chain.</returns>
        public static MicrosoftIdentityAppCallsWebApiAuthenticationBuilder AddMicrosoftGraphServiceClient(
            this MicrosoftIdentityAppCallsWebApiAuthenticationBuilder builder,
            IConfigurationSection configurationSection)
        {
            return builder.AddMicrosoftGraphServiceClient(
                options => configurationSection.Bind(options));
        }

        /// <summary>
        /// Add support to call Microsoft Graph. From a base Graph URL and a default scope.
        /// </summary>
        /// <param name="builder">Builder.</param>
        /// <param name="graphBaseUrl">Named instance of option.</param>
        /// <param name="defaultScopes">Configuration section.</param>
        /// <returns>The builder to chain.</returns>
        public static MicrosoftIdentityAppCallsWebApiAuthenticationBuilder AddMicrosoftGraphServiceClient(
            this MicrosoftIdentityAppCallsWebApiAuthenticationBuilder builder,
            string graphBaseUrl = Constants.GraphBaseUrlV1,
            string defaultScopes = Constants.UserReadScope)
        {
            return builder.AddMicrosoftGraphServiceClient(
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
        public static MicrosoftIdentityAppCallsWebApiAuthenticationBuilder AddMicrosoftGraphServiceClient(
            this MicrosoftIdentityAppCallsWebApiAuthenticationBuilder builder,
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
                if (microsoftGraphOptions.Scopes == null)
                {
                    throw new ArgumentException(IDWebErrorMessage.CalledApiScopesAreNull);
                }

                string graphBaseUrl = microsoftGraphOptions.BaseUrl;
                string[] initialScopes = microsoftGraphOptions.Scopes.Split(' ');

                GraphServiceClient client = string.IsNullOrWhiteSpace(graphBaseUrl) ?
                            new GraphServiceClient(new TokenAcquisitionCredentialProvider(tokenAquisitionService, initialScopes)) :
                            new GraphServiceClient(graphBaseUrl, new TokenAcquisitionCredentialProvider(tokenAquisitionService, initialScopes));
                return client;
            });
            return builder;
        }
    }
}

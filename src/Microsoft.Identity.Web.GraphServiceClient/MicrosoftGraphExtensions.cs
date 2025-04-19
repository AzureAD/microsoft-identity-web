// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Graph;
using Microsoft.Identity.Abstractions;
using Microsoft.Kiota.Abstractions.Authentication;

namespace Microsoft.Identity.Web
{
#if !NET472 && !NET462 && !NETSTANDARD2_0
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
            IEnumerable<string>? defaultScopes = null)
        {
            return builder.AddMicrosoftGraph(
                options =>
                {
                    options.BaseUrl = graphBaseUrl;
                    options.Scopes = defaultScopes ?? new List<string> { Constants.UserReadScope };
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
            Action<GraphServiceClientOptions> configureMicrosoftGraphOptions)
        {
            _ = Throws.IfNull(builder);

            builder.Services.AddMicrosoftGraph(configureMicrosoftGraphOptions);
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
            _ = Throws.IfNull(builder);

            builder.Services.AddScoped<GraphServiceClient, GraphServiceClient>(serviceProvider =>
            {
                IAuthorizationHeaderProvider authorizationHeaderProvider = serviceProvider.GetRequiredService<IAuthorizationHeaderProvider>();

                return graphServiceClientFactory(new GraphAuthenticationProvider(
                    authorizationHeaderProvider,
                    new GraphAuthenticationOptions() { Scopes = initialScopes.ToArray() }));
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
            _ = Throws.IfNull(builder);

            builder.Services.AddScoped<GraphServiceClient, GraphServiceClient>(serviceProvider =>
            {
                IAuthorizationHeaderProvider authorizationHeaderProvider = serviceProvider.GetRequiredService<IAuthorizationHeaderProvider>();

                return graphServiceClientFactory(new GraphAuthenticationProvider(
                    authorizationHeaderProvider,
                    new GraphAuthenticationOptions() { RequestAppToken = true }));
            });
            return builder;
        }
    }
#endif
}

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
#if NET6_0_OR_GREATER && !NET8_0_OR_GREATER
        [RequiresUnreferencedCode("Microsoft.Extensions.Configuration.ConfigurationBinder.Bind(IConfiguration, Object).")]
#endif
#if NET8_0_OR_GREATER
        [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "Configuration binding with AddOptions<T>().Bind() uses source generators on .NET 8+")]
        [UnconditionalSuppressMessage("AOT", "IL3050:Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.", Justification = "Configuration binding with AddOptions<T>().Bind() uses source generators on .NET 8+")]
#endif
        public static MicrosoftIdentityAppCallsWebApiAuthenticationBuilder AddMicrosoftGraph(
            this MicrosoftIdentityAppCallsWebApiAuthenticationBuilder builder,
            IConfigurationSection configurationSection)
        {
#if NET8_0_OR_GREATER
            // For .NET 8+, use source generator-based binding for AOT compatibility
            builder.Services.AddOptions<MicrosoftGraphOptions>()
                .Bind(configurationSection);
            return builder.AddMicrosoftGraph(_ => { }); // No-op, binding already done
#else
            return builder.AddMicrosoftGraph(
                options => configurationSection.Bind(options));
#endif
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

                return graphServiceClientFactory(new TokenAcquisitionAuthenticationProvider(
                    authorizationHeaderProvider,
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
            _ = Throws.IfNull(builder);

            builder.Services.AddScoped<GraphServiceClient, GraphServiceClient>(serviceProvider =>
            {
                IAuthorizationHeaderProvider authorizationHeaderProvider = serviceProvider.GetRequiredService<IAuthorizationHeaderProvider>();

                return graphServiceClientFactory(new TokenAcquisitionAuthenticationProvider(
                    authorizationHeaderProvider,
                    new TokenAcquisitionAuthenticationProviderOption() { AppOnly = true }));
            });
            return builder;
        }
    }
#endif
}

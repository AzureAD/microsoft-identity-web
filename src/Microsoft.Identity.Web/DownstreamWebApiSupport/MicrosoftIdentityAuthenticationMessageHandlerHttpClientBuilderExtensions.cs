// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// Extension for IHttpClientBuilder for startup initialization of Microsoft Identity authentication handlers.
    /// </summary>
    public static class MicrosoftIdentityAuthenticationMessageHandlerHttpClientBuilderExtensions
    {
        /// <summary>
        /// Adds a named Microsoft Identity user authentication message handler related to a specific configuration section.
        /// </summary>
        /// <param name="builder">Builder.</param>
        /// <param name="serviceName">Name of the configuration for the service.</param>
        /// <param name="configuration">Configuration.</param>
        /// <returns>The builder for chaining.</returns>
#if NET8_0_OR_GREATER
        [RequiresUnreferencedCode("Calls Microsoft.Extensions.Configuration.ConfigurationBinder.Bind(IConfiguration, Object)")]
        [RequiresDynamicCode("Calls Microsoft.Extensions.Configuration.ConfigurationBinder.Bind(IConfiguration, Object)")]
#endif
        public static IHttpClientBuilder AddMicrosoftIdentityUserAuthenticationHandler(
            this IHttpClientBuilder builder,
            string serviceName,
            IConfiguration configuration)
        {
            _ = Throws.IfNull(builder);

            builder.Services.Configure<MicrosoftIdentityAuthenticationMessageHandlerOptions>(serviceName, configuration);
            builder.AddMicrosoftIdentityAuthenticationHandlerCore(factory => factory.CreateUserHandler(serviceName));

            return builder;
        }

        /// <summary>
        /// Adds a named Microsoft Identity user authentication message handler initialized with delegates.
        /// </summary>
        /// <param name="builder">Builder.</param>
        /// <param name="serviceName">Name of the configuration for the service.</param>
        /// <param name="configureOptions">Action to configure the options.</param>
        /// <returns>The builder for chaining.</returns>
        public static IHttpClientBuilder AddMicrosoftIdentityUserAuthenticationHandler(
            this IHttpClientBuilder builder,
            string serviceName,
            Action<MicrosoftIdentityAuthenticationMessageHandlerOptions> configureOptions)
        {
            _ = Throws.IfNull(builder);

            builder.Services.Configure(serviceName, configureOptions);
            builder.AddMicrosoftIdentityAuthenticationHandlerCore(factory => factory.CreateUserHandler(serviceName));

            return builder;
        }

        /// <summary>
        /// Adds a named Microsoft Identity application authentication message handler related to a specific configuration section.
        /// </summary>
        /// <param name="builder">Builder.</param>
        /// <param name="serviceName">Name of the configuration for the service.</param>
        /// <param name="configuration">Configuration.</param>
        /// <returns>The builder for chaining.</returns>
#if NET8_0_OR_GREATER
        [RequiresUnreferencedCode("Calls Microsoft.Extensions.Configuration.ConfigurationBinder.Bind(IConfiguration, Object)")]
        [RequiresDynamicCode("Calls Microsoft.Extensions.Configuration.ConfigurationBinder.Bind(IConfiguration, Object)")]
#endif
        public static IHttpClientBuilder AddMicrosoftIdentityAppAuthenticationHandler(
            this IHttpClientBuilder builder,
            string serviceName,
            IConfiguration configuration)
        {
            _ = Throws.IfNull(builder);

            builder.Services.Configure<MicrosoftIdentityAuthenticationMessageHandlerOptions>(serviceName, configuration);
            builder.AddMicrosoftIdentityAuthenticationHandlerCore(factory => factory.CreateAppHandler(serviceName));

            return builder;
        }

        /// <summary>
        /// Adds a named Microsoft Identity application authentication message handler initialized with delegates.
        /// </summary>
        /// <param name="builder">Builder.</param>
        /// <param name="serviceName">Name of the configuration for the service.</param>
        /// <param name="configureOptions">Action to configure the options.</param>
        /// <returns>The builder for chaining.</returns>
        public static IHttpClientBuilder AddMicrosoftIdentityAppAuthenticationHandler(
            this IHttpClientBuilder builder,
            string serviceName,
            Action<MicrosoftIdentityAuthenticationMessageHandlerOptions> configureOptions)
        {
            _ = Throws.IfNull(builder);

            builder.Services.Configure(serviceName, configureOptions);
            builder.AddMicrosoftIdentityAuthenticationHandlerCore(factory => factory.CreateAppHandler(serviceName));

            return builder;
        }

        /// <summary>
        /// Adds the common configuration for message handlers.
        /// </summary>
        /// <param name="builder">
        /// The <see cref="IHttpClientBuilder"/> to configure.
        /// </param>
        /// <param name="configureHandler">
        /// A Func that takes the <see cref="IMicrosoftIdentityAuthenticationDelegatingHandlerFactory"/> and returns
        /// the <see cref="DelegatingHandler"/>. This func allows us to reuse the logic to add message handlers,
        /// while allowing the caller to decide if it needs an app handler or a user handler.
        /// </param>
        private static void AddMicrosoftIdentityAuthenticationHandlerCore(
            this IHttpClientBuilder builder,
            Func<IMicrosoftIdentityAuthenticationDelegatingHandlerFactory, DelegatingHandler> configureHandler)
        {
            builder.Services.TryAddScoped<IMicrosoftIdentityAuthenticationDelegatingHandlerFactory, DefaultMicrosoftIdentityAuthenticationDelegatingHandlerFactory>();
            builder.AddHttpMessageHandler(services =>
            {
                var factory = services.GetRequiredService<IMicrosoftIdentityAuthenticationDelegatingHandlerFactory>();
                return configureHandler(factory);
            });
        }
    }
}

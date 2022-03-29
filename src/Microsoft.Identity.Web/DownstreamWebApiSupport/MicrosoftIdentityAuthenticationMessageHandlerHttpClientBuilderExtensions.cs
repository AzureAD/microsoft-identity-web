// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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
        public static IHttpClientBuilder AddMicrosoftIdentityUserAuthenticationHandler(
            this IHttpClientBuilder builder,
            string serviceName,
            IConfiguration configuration)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            builder.Services.Configure<MicrosoftIdentityAuthenticationMessageHandlerOptions>(serviceName, configuration);
            builder.Services.AddSingleton<IMicrosoftIdentityAuthenticationDelegatingHandlerFactory, MicrosoftIdentityAuthenticationDelegatingHandlerFactory>();
            builder.AddHttpMessageHandler(services =>
            {
                return services
                    .GetRequiredService<IMicrosoftIdentityAuthenticationDelegatingHandlerFactory>()
                    .CreateUserHandler(serviceName);
            });

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
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            builder.Services.Configure(serviceName, configureOptions);
            builder.Services.AddSingleton<IMicrosoftIdentityAuthenticationDelegatingHandlerFactory, MicrosoftIdentityAuthenticationDelegatingHandlerFactory>();
            builder.AddHttpMessageHandler(services =>
            {
                return services
                    .GetRequiredService<IMicrosoftIdentityAuthenticationDelegatingHandlerFactory>()
                    .CreateUserHandler(serviceName);
            });

            return builder;
        }

        /// <summary>
        /// Adds a named Microsoft Identity application authentication message handler related to a specific configuration section.
        /// </summary>
        /// <param name="builder">Builder.</param>
        /// <param name="serviceName">Name of the configuration for the service.</param>
        /// <param name="configuration">Configuration.</param>
        /// <returns>The builder for chaining.</returns>
        public static IHttpClientBuilder AddMicrosoftIdentityAppAuthenticationHandler(
            this IHttpClientBuilder builder,
            string serviceName,
            IConfiguration configuration)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            builder.Services.Configure<MicrosoftIdentityAuthenticationMessageHandlerOptions>(serviceName, configuration);
            builder.Services.AddSingleton<IMicrosoftIdentityAuthenticationDelegatingHandlerFactory, MicrosoftIdentityAuthenticationDelegatingHandlerFactory>();
            builder.AddHttpMessageHandler(services =>
            {
                return services
                    .GetRequiredService<IMicrosoftIdentityAuthenticationDelegatingHandlerFactory>()
                    .CreateAppHandler(serviceName);
            });

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
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            builder.Services.Configure(serviceName, configureOptions);
            builder.Services.AddSingleton<IMicrosoftIdentityAuthenticationDelegatingHandlerFactory, MicrosoftIdentityAuthenticationDelegatingHandlerFactory>();
            builder.AddHttpMessageHandler(services =>
            {
                return services
                    .GetRequiredService<IMicrosoftIdentityAuthenticationDelegatingHandlerFactory>()
                    .CreateAppHandler(serviceName);
            });

            return builder;
        }
    }
}

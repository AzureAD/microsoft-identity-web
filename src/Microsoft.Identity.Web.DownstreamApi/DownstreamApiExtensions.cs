// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.AspNetCore.DataProtection.XmlEncryption;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Abstractions;

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// Extension methods to support downstream API services.
    /// </summary>
    public static class DownstreamApiExtensions
    {
        /// <summary>
        /// Adds a named downstream API service related to a specific configuration section.
        /// </summary>
        /// <param name="services">services.</param>
        /// <param name="serviceName">Name of the configuration for the service.
        /// This is the name used when calling the service from controller/pages.</param>
        /// <param name="configuration">Configuration.</param>
        /// <returns>The builder for chaining.</returns>
#if NET6_0_OR_GREATER
        [RequiresUnreferencedCode("Microsoft.Extensions.DependencyInjection.OptionsConfigurationServiceCollectionExtensions.Configure<TOptions>(IServiceCollection, String, IConfiguration).")]
#endif
        public static IServiceCollection AddDownstreamApi(
            this IServiceCollection services,
            string serviceName,
            IConfiguration configuration)
        {
            _ = Throws.IfNull(services);

            services.Configure<DownstreamApiOptions>(serviceName, configuration);
            RegisterDownstreamApi(services);
            return services;
        }

        /// <summary>
        /// Adds a named downstream API service initialized with delegates.
        /// </summary>
        /// <param name="services">services.</param>
        /// <param name="serviceName">Name of the configuration for the service.
        /// This is the name which will be used when calling the service from controller/pages.</param>
        /// <param name="configureOptions">Action to configure the options.</param>
        /// <returns>The builder for chaining.</returns>
        public static IServiceCollection AddDownstreamApi(
            this IServiceCollection services,
            string serviceName,
            Action<DownstreamApiOptions> configureOptions)
        {
            _ = Throws.IfNull(services);

            services.Configure(serviceName, configureOptions);
            RegisterDownstreamApi(services);

            return services;
        }

        private static void RegisterDownstreamApi(IServiceCollection services)
        {
            ServiceDescriptor? tokenAcquisitionService = services.FirstOrDefault(s => s.ServiceType == typeof(ITokenAcquisition));
            ServiceDescriptor? downstreamApi = services.FirstOrDefault(s => s.ServiceType == typeof(IDownstreamApi));

            if (tokenAcquisitionService != null)
            {
                if (downstreamApi != null)
                {
                    if (downstreamApi.Lifetime != tokenAcquisitionService.Lifetime)
                    {
                        services.Remove(downstreamApi);
                        AddDownstreamApiWithLifetime(services, tokenAcquisitionService.Lifetime);
                    }
                }
                else
                {
                    AddDownstreamApiWithLifetime(services, tokenAcquisitionService.Lifetime);
                }
            }
            else
            {
                services.AddScoped<IDownstreamApi, DownstreamApi>();
            }
        }

        private static void AddDownstreamApiWithLifetime(IServiceCollection services, ServiceLifetime lifetime)
        {
            if (lifetime == ServiceLifetime.Singleton)
            {
                services.AddSingleton<IDownstreamApi, DownstreamApi>();
            }
            else
            {
                services.AddScoped<IDownstreamApi, DownstreamApi>();
            }
        }

#if NETCOREAPP
        /// <summary>
        /// Adds a named downstream web service related to a specific configuration section.
        /// </summary>
        /// <param name="builder">Builder.</param>
        /// <param name="serviceName">Name of the configuration for the service.
        /// This is the name used when calling the service from controller/pages.</param>
        /// <param name="configuration">Configuration.</param>
        /// <returns>The builder for chaining.</returns>
#if NET6_0_OR_GREATER
        [RequiresUnreferencedCode("Microsoft.Identity.Web.DownstreamApiExtensions.AddDownstreamApi(IServiceCollection, String, IConfiguration).")]
#endif
        public static MicrosoftIdentityAppCallsWebApiAuthenticationBuilder AddDownstreamApi(
            this MicrosoftIdentityAppCallsWebApiAuthenticationBuilder builder,
            string serviceName,
            IConfiguration configuration)
        {
            _ = Throws.IfNull(builder);

            builder.Services.AddDownstreamApi(serviceName, configuration);
            return builder;
        }

        /// <summary>
        /// Adds a named downstream API service initialized with delegates.
        /// </summary>
        /// <param name="builder">Builder.</param>
        /// <param name="serviceName">Name of the configuration for the service.
        /// This is the name which will be used when calling the service from controller/pages.</param>
        /// <param name="configureOptions">Action to configure the options.</param>
        /// <returns>The builder for chaining.</returns>
        public static MicrosoftIdentityAppCallsWebApiAuthenticationBuilder AddDownstreamApi(
            this MicrosoftIdentityAppCallsWebApiAuthenticationBuilder builder,
            string serviceName,
            Action<DownstreamApiOptions> configureOptions)
        {
            _ = Throws.IfNull(builder);

            builder.Services.AddDownstreamApi(serviceName, configureOptions);
            return builder;
        }
#endif
    }
}

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// Extension methods to support downstream REST API services.
    /// </summary>
    public static class DownstreamRestApiExtensions
    {
        /// <summary>
        /// Adds a named downstream REST API service related to a specific configuration section.
        /// </summary>
        /// <param name="services">services.</param>
        /// <param name="serviceName">Name of the configuration for the service.
        /// This is the name used when calling the service from controller/pages.</param>
        /// <param name="configuration">Configuration.</param>
        /// <returns>The builder for chaining.</returns>
        public static IServiceCollection AddDownstreamRestApi(
            this IServiceCollection services,
            string serviceName,
            IConfiguration configuration)
        {
            if (services is null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            services.Configure<DownstreamRestApiOptions>(serviceName, configuration);
            services.AddScoped<IDownstreamRestApi, DownstreamRestApi>();
            return services;
        }

        /// <summary>
        /// Adds a named downstream REST API service initialized with delegates.
        /// </summary>
        /// <param name="services">services.</param>
        /// <param name="serviceName">Name of the configuration for the service.
        /// This is the name which will be used when calling the service from controller/pages.</param>
        /// <param name="configureOptions">Action to configure the options.</param>
        /// <returns>The builder for chaining.</returns>
        public static IServiceCollection AddDownstreamRestApi(
            this IServiceCollection services,
            string serviceName,
            Action<DownstreamRestApiOptions> configureOptions)
        {
            if (services is null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            services.Configure<DownstreamRestApiOptions>(serviceName, configureOptions);

            // https://docs.microsoft.com/en-us/dotnet/standard/microservices-architecture/implement-resilient-applications/use-httpclientfactory-to-implement-resilient-http-requests
            services.AddScoped<IDownstreamRestApi, DownstreamRestApi>();
            services.Configure(serviceName, configureOptions);
            return services;
        }
    }
}

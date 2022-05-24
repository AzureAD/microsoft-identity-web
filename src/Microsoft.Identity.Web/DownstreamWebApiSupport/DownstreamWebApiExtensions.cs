// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// Extension methods to support downstream web API services.
    /// </summary>
    public static class DownstreamWebApiExtensions
    {
        /// <summary>
        /// Adds a named downstream web API service related to a specific configuration section.
        /// </summary>
        /// <param name="builder">Builder.</param>
        /// <param name="serviceName">Name of the configuration for the service.
        /// This is the name used when calling the service from controller/pages.</param>
        /// <param name="configuration">Configuration.</param>
        /// <param name="configureHttpClient">Configure the HttpClient for the Downstream Web Api.</param>
        /// <returns>The builder for chaining.</returns>
        public static MicrosoftIdentityAppCallsWebApiAuthenticationBuilder AddDownstreamWebApi(
            this MicrosoftIdentityAppCallsWebApiAuthenticationBuilder builder,
            string serviceName,
            IConfiguration configuration,
            Action<IHttpClientBuilder>? configureHttpClient = null)
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            builder.Services.Configure<DownstreamWebApiOptions>(serviceName, configuration);

            builder.Services.TryAddTransient<IDownstreamWebApi, DownstreamWebApi>();

            IHttpClientBuilder httpClientBuilder = builder.Services.AddHttpClient(serviceName);
            configureHttpClient?.Invoke(httpClientBuilder);

            return builder;
        }

        /// <summary>
        /// Adds a named downstream web API service initialized with delegates.
        /// </summary>
        /// <param name="builder">Builder.</param>
        /// <param name="serviceName">Name of the configuration for the service.
        /// This is the name which will be used when calling the service from controller/pages.</param>
        /// <param name="configureOptions">Action to configure the options.</param>
        /// <param name="configureHttpClient">Configure the HttpClient for the Downstream Web Api.</param>
        /// <returns>The builder for chaining.</returns>
        public static MicrosoftIdentityAppCallsWebApiAuthenticationBuilder AddDownstreamWebApi(
            this MicrosoftIdentityAppCallsWebApiAuthenticationBuilder builder,
            string serviceName,
            Action<DownstreamWebApiOptions> configureOptions,
            Action<IHttpClientBuilder>? configureHttpClient = null)
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            builder.Services.Configure<DownstreamWebApiOptions>(serviceName, configureOptions);

            builder.Services.TryAddTransient<IDownstreamWebApi, DownstreamWebApi>();

            // https://docs.microsoft.com/en-us/dotnet/standard/microservices-architecture/implement-resilient-applications/use-httpclientfactory-to-implement-resilient-http-requests
            IHttpClientBuilder httpClientBuilder = builder.Services.AddHttpClient(serviceName);
            configureHttpClient?.Invoke(httpClientBuilder);

            builder.Services.Configure(serviceName, configureOptions);
            return builder;
        }
    }
}

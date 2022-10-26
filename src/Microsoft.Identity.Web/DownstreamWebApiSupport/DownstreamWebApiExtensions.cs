// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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
        /// <returns>The builder for chaining.</returns>
        public static MicrosoftIdentityAppCallsWebApiAuthenticationBuilder AddDownstreamWebApi(
            this MicrosoftIdentityAppCallsWebApiAuthenticationBuilder builder,
            string serviceName,
            IConfiguration configuration)
        {
            _ = Throws.IfNull(builder);

            builder.Services.Configure<DownstreamWebApiOptions>(serviceName, configuration);
            builder.Services.AddHttpClient<IDownstreamWebApi, DownstreamWebApi>();
            return builder;
        }

        /// <summary>
        /// Adds a named downstream web API service initialized with delegates.
        /// </summary>
        /// <param name="builder">Builder.</param>
        /// <param name="serviceName">Name of the configuration for the service.
        /// This is the name which will be used when calling the service from controller/pages.</param>
        /// <param name="configureOptions">Action to configure the options.</param>
        /// <returns>The builder for chaining.</returns>
        public static MicrosoftIdentityAppCallsWebApiAuthenticationBuilder AddDownstreamWebApi(
            this MicrosoftIdentityAppCallsWebApiAuthenticationBuilder builder,
            string serviceName,
            Action<DownstreamWebApiOptions> configureOptions)
        {
            _ = Throws.IfNull(builder);

            builder.Services.Configure<DownstreamWebApiOptions>(serviceName, configureOptions);

            // https://docs.microsoft.com/en-us/dotnet/standard/microservices-architecture/implement-resilient-applications/use-httpclientfactory-to-implement-resilient-http-requests
            builder.Services.AddHttpClient<IDownstreamWebApi, DownstreamWebApi>();
            builder.Services.Configure(serviceName, configureOptions);
            return builder;
        }
    }
}

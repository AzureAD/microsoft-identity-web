// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// Extension methods to support downstream api services.
    /// </summary>
    public static class DownstreamApiServiceExtensions
    {
        /// <summary>
        /// Adds a named downstream API service related to a specific configuration section.
        /// </summary>
        /// <param name="builder">Builder.</param>
        /// <param name="optionsInstanceName">Name of the configuration for the service.
        /// This is the name which will be used when calling the service from controller/pages.</param>
        /// <param name="configuration">Configuration.</param>
        /// <returns>The builder for chaining.</returns>
        public static MicrosoftIdentityAppCallsWebApiAuthenticationBuilder AddDownstreamApiService(
            this MicrosoftIdentityAppCallsWebApiAuthenticationBuilder builder,
            string optionsInstanceName,
            IConfiguration configuration)
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            builder.Services.Configure<CalledApiOptions>(optionsInstanceName, configuration);
            builder.Services.AddHttpClient<IDownstreamWebApi, DownstreamWebApi>();
            return builder;
        }

        /// <summary>
        /// Adds a named downstream API service initialized with delegates.
        /// </summary>
        /// <param name="builder">Builder.</param>
        /// <param name="optionsInstanceName">Name of the configuration for the service.
        /// This is the name which will be used when calling the service from controller/pages.</param>
        /// <param name="configureOptions">Action to configure the options.</param>
        /// <returns>The builder for chaining.</returns>
        public static MicrosoftIdentityAppCallsWebApiAuthenticationBuilder AddDownstreamApiService(
            this MicrosoftIdentityAppCallsWebApiAuthenticationBuilder builder,
            string optionsInstanceName,
            Action<CalledApiOptions> configureOptions)
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            builder.Services.Configure<CalledApiOptions>(optionsInstanceName, configureOptions);

            // https://docs.microsoft.com/en-us/dotnet/standard/microservices-architecture/implement-resilient-applications/use-httpclientfactory-to-implement-resilient-http-requests
            builder.Services.AddHttpClient<IDownstreamWebApi, DownstreamWebApi>();
            builder.Services.Configure(optionsInstanceName, configureOptions);
            return builder;
        }
    }
}

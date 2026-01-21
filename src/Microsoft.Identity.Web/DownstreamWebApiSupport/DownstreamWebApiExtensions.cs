// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
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
        [Obsolete("Use AddDownstreamApi in Microsoft.Identity.Abstractions, implemented in Microsoft.Identity.Web.DownstreamApi." +
        "See aka.ms/id-web-downstream-api-v2 for migration details.", false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
#if NET6_0_OR_GREATER && !NET8_0_OR_GREATER
        [RequiresUnreferencedCode("Calls Microsoft.Extensions.DependencyInjection.OptionsConfigurationServiceCollectionExtensions.Configure<TOutput>(IServiceCollection, String, IConfiguration).")]
#endif
#if NET8_0_OR_GREATER
        [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "Configuration binding with AddOptions<T>().Bind() uses source generators on .NET 8+")]
        [UnconditionalSuppressMessage("AOT", "IL3050:Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.", Justification = "Configuration binding with AddOptions<T>().Bind() uses source generators on .NET 8+")]
#endif
        public static MicrosoftIdentityAppCallsWebApiAuthenticationBuilder AddDownstreamWebApi(
            this MicrosoftIdentityAppCallsWebApiAuthenticationBuilder builder,
            string serviceName,
            IConfiguration configuration)
        {
            _ = Throws.IfNull(builder);

#if NET8_0_OR_GREATER
            // For .NET 8+, use source generator-based binding for AOT compatibility
            builder.Services.AddOptions<DownstreamWebApiOptions>(serviceName)
                .Bind(configuration);
#else
            builder.Services.Configure<DownstreamWebApiOptions>(serviceName, configuration);
#endif
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
        [Obsolete("Use AddDownstreamApi in Microsoft.Identity.Abstractions, implemented in Microsoft.Identity.Web.DownstreamApi." +
        "See aka.ms/id-web-downstream-api-v2 for migration details.", false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static MicrosoftIdentityAppCallsWebApiAuthenticationBuilder AddDownstreamWebApi(
            this MicrosoftIdentityAppCallsWebApiAuthenticationBuilder builder,
            string serviceName,
            Action<DownstreamWebApiOptions> configureOptions)
        {
            _ = Throws.IfNull(builder);

            builder.Services.Configure<DownstreamWebApiOptions>(serviceName, configureOptions);

            // https://learn.microsoft.com/dotnet/standard/microservices-architecture/implement-resilient-applications/use-httpclientfactory-to-implement-resilient-http-requests
            builder.Services.AddHttpClient<IDownstreamWebApi, DownstreamWebApi>();
            builder.Services.Configure(serviceName, configureOptions);
            return builder;
        }
    }
}

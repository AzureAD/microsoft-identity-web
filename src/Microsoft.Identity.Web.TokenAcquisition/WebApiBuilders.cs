// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Client;

namespace Microsoft.Identity.Web.Internal
{
    /// <summary>
    /// Web API authentication builder.
    /// </summary>
    public static class WebApiBuilders
    {
        /// <summary>
        /// Allows a higher level abstraction of security token (i.e. System.IdentityModel.Tokens.Jwt and more modern, Microsoft.IdentityModel.JsonWebTokens)
        /// to be used with Microsoft Identity Web.
        /// Developers should continue to use `EnableTokenAcquisitionToCallDownstreamApi`.
        /// This API is not considered part of the public API and may change.
        /// </summary>
        /// <param name="configureConfidentialClientApplicationOptions">The action to configure <see cref="ConfidentialClientApplicationOptions"/>.</param>
        /// <param name="authenticationScheme">Authentication scheme.</param>
        /// <param name="services">The services being configured.</param>
        /// <param name="configuration">IConfigurationSection.</param>
        /// <returns>The authentication builder to chain.</returns>
#if NET6_0_OR_GREATER && !NET8_0_OR_GREATER
        [RequiresUnreferencedCode("Calls Bind, Configure with Unspecified Configuration and ServiceCollection.")]
#endif
        public static MicrosoftIdentityAppCallsWebApiAuthenticationBuilder EnableTokenAcquisition(
            Action<ConfidentialClientApplicationOptions> configureConfidentialClientApplicationOptions,
            string authenticationScheme,
            IServiceCollection services,
            IConfigurationSection? configuration)
        {
            if (configuration != null)
            {
#if NET8_0_OR_GREATER
                // For .NET 8+, use source generator-based binding for AOT compatibility
                services.AddOptions<MicrosoftIdentityApplicationOptions>(authenticationScheme)
                    .Bind(configuration);
                services.AddOptions<MicrosoftIdentityOptions>(authenticationScheme)
                    .Bind(configuration);
#else
                // TODO: This never was right. And the configureConfidentialClientApplicationOptions delegate is not used
                // services.Configure<ConfidentialClientApplicationOptions>(authenticationScheme, configuration);
                services.Configure<MicrosoftIdentityApplicationOptions>(authenticationScheme, options
                    =>
                { configuration.Bind(options); });
                services.Configure<MicrosoftIdentityOptions>(authenticationScheme, options
                    =>
                { configuration.Bind(options); });
#endif
            }
            services.AddTokenAcquisition();

            return new MicrosoftIdentityAppCallsWebApiAuthenticationBuilder(
                services,
                configuration);
        }
    }
}

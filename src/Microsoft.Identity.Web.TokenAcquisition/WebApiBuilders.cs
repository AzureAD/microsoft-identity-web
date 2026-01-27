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
        [RequiresUnreferencedCode("Calls Microsoft.Extensions.Configuration.ConfigurationBinder.Bind(IConfiguration, Object).")]
        [RequiresDynamicCode("Calls Microsoft.Extensions.Configuration.ConfigurationBinder.Bind(IConfiguration, Object).")]
        public static MicrosoftIdentityAppCallsWebApiAuthenticationBuilder EnableTokenAcquisition(
            Action<ConfidentialClientApplicationOptions> configureConfidentialClientApplicationOptions,
            string authenticationScheme,
            IServiceCollection services,
            IConfigurationSection? configuration)
        {
            if (configuration != null)
            {
                // TODO: This never was right. And the configureConfidentialClientApplicationOptions delegate is not used
                // services.Configure<ConfidentialClientApplicationOptions>(authenticationScheme, configuration);
                services.Configure<MicrosoftIdentityApplicationOptions>(authenticationScheme, options
                    =>
                { configuration.Bind(options); });
                services.Configure<MicrosoftIdentityOptions>(authenticationScheme, options
                    =>
                { configuration.Bind(options); });
            }
            services.AddTokenAcquisition();

            return new MicrosoftIdentityAppCallsWebApiAuthenticationBuilder(
                services,
                configuration);
        }

        /// <summary>
        /// Allows a higher level abstraction of security token (i.e. System.IdentityModel.Tokens.Jwt and more modern, Microsoft.IdentityModel.JsonWebTokens)
        /// to be used with Microsoft Identity Web.
        /// Developers should continue to use `EnableTokenAcquisitionToCallDownstreamApi`.
        /// This API is not considered part of the public API and may change.
        /// </summary>
        /// <param name="authenticationScheme">Authentication scheme.</param>
        /// <param name="services">The services being configured.</param>
        /// <param name="configuration">IConfigurationSection.</param>
        /// <returns>The authentication builder to chain.</returns>
        public static MicrosoftIdentityAppCallsWebApiAuthenticationBuilder EnableTokenAcquisition(
            string authenticationScheme,
            IServiceCollection services,
            IConfigurationSection? configuration)
        {
            if (configuration != null)
            {
                services.Configure<MicrosoftIdentityApplicationOptions>(authenticationScheme, options =>
                    MicrosoftIdentityApplicationOptionsBinder.Bind(options, configuration));
                services.Configure<MicrosoftIdentityOptions>(authenticationScheme, options =>
                    MicrosoftIdentityOptionsBinder.Bind(options, configuration));
            }
            services.AddTokenAcquisition();

            return new MicrosoftIdentityAppCallsWebApiAuthenticationBuilder(
                services,
                configuration);
        }
    }
}

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// Builder for a Microsoft identity web app authentication where configuration is
    /// available for EnableTokenAcquisitionToCallDownstreamApi.
    /// </summary>
#if NET8_0_OR_GREATER
    [RequiresUnreferencedCode("Calls Microsoft.Extensions.Configuration.ConfigurationBinder.Bind(IConfiguration, Object)")]
    [RequiresDynamicCode("Calls Microsoft.Extensions.Configuration.ConfigurationBinder.Bind(IConfiguration, Object)")]
#endif
    public class MicrosoftIdentityWebAppAuthenticationBuilderWithConfiguration : MicrosoftIdentityWebAppAuthenticationBuilder
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="services"> The services being configured.</param>
        /// <param name="openIdConnectScheme">Default scheme used for OpenIdConnect.</param>
        /// <param name="configureMicrosoftIdentityOptions">Action called to configure
        /// the <see cref="MicrosoftIdentityOptions"/>Microsoft identity options.</param>
        /// <param name="configurationSection">Optional configuration section.</param>

        internal MicrosoftIdentityWebAppAuthenticationBuilderWithConfiguration(
            IServiceCollection services,
            string openIdConnectScheme,
            Action<MicrosoftIdentityOptions> configureMicrosoftIdentityOptions,
            IConfigurationSection configurationSection)
            : base(services, openIdConnectScheme, configureMicrosoftIdentityOptions, configurationSection)
        {
            _ = Throws.IfNull(configurationSection);
        }

        /// <summary>
        /// Add support for the web app to acquire tokens to call an API.
        /// </summary>
        /// <param name="initialScopes">Optional initial scopes to request.</param>
        /// <returns>The authentication builder for chaining.</returns>
        public new MicrosoftIdentityAppCallsWebApiAuthenticationBuilder EnableTokenAcquisitionToCallDownstreamApi(
            IEnumerable<string>? initialScopes = null)
        {
            Services.AddHttpClient();

            return EnableTokenAcquisitionToCallDownstreamApi(
                options =>
                {
                    ConfigurationSection?.Bind(options);
                    if (AppServicesAuthenticationInformation.IsAppServicesAadAuthenticationEnabled)
                    {
                        options.ClientId = AppServicesAuthenticationInformation.ClientId;
                        options.ClientSecret = AppServicesAuthenticationInformation.ClientSecret;
                        options.Instance = AppServicesAuthenticationInformation.Issuer;
                    }
                },
                initialScopes);
        }
    }
}

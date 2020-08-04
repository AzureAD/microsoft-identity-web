// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// Builder for Web API authentication with configuration.
    /// </summary>
    public class MicrosoftWebApiAuthenticationBuilderWithConfiguration : MicrosoftWebApiAuthenticationBuilder
    {
        internal MicrosoftWebApiAuthenticationBuilderWithConfiguration(
            IServiceCollection services,
            string jwtBearerAuthenticationScheme,
            Action<JwtBearerOptions> configureJwtBearerOptions,
            Action<MicrosoftIdentityOptions> configureMicrosoftIdentityOptions,
            IConfigurationSection? configurationSection)
            : base(services, jwtBearerAuthenticationScheme, configureJwtBearerOptions, configureMicrosoftIdentityOptions, configurationSection)
        {
            if (configurationSection == null)
            {
                throw new ArgumentNullException(nameof(configurationSection));
            }
        }

        /// <summary>
        /// Protects the web API with Microsoft identity platform (formerly Azure AD v2.0).
        /// This method expects the configuration file will have a section, named "AzureAd" as default, with the necessary settings to initialize authentication options.
        /// </summary>
        /// <returns>The authentication builder to chain.</returns>
        public MicrososoftAppCallingWebApiAuthenticationBuilder CallsWebApi()
        {
            return CallsWebApi(options => ConfigurationSection.Bind(options));
        }
    }
}

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// Builder for web API authentication with configuration.
    /// </summary>
    [RequiresUnreferencedCode("Microsoft.Identity.Web.MicrosoftIdentityWebApiAuthenticationBuilder.MicrosoftIdentityWebApiAuthenticationBuilder(IServiceCollection, String, Action<JwtBearerOptions>, Action<MicrosoftIdentityOptions>, IConfigurationSection).")]
    [RequiresDynamicCode("Microsoft.Identity.Web.MicrosoftIdentityWebApiAuthenticationBuilder.MicrosoftIdentityWebApiAuthenticationBuilder(IServiceCollection, String, Action<JwtBearerOptions>, Action<MicrosoftIdentityOptions>, IConfigurationSection).")]
    public class MicrosoftIdentityWebApiAuthenticationBuilderWithConfiguration : MicrosoftIdentityWebApiAuthenticationBuilder
    {
        internal MicrosoftIdentityWebApiAuthenticationBuilderWithConfiguration(
            IServiceCollection services,
            string jwtBearerAuthenticationScheme,
            Action<JwtBearerOptions> configureJwtBearerOptions,
            Action<MicrosoftIdentityOptions> configureMicrosoftIdentityOptions,
            IConfigurationSection? configurationSection)
            : base(services, jwtBearerAuthenticationScheme, configureJwtBearerOptions, configureMicrosoftIdentityOptions, configurationSection)
        {
            _ = Throws.IfNull(configurationSection);
        }

        /// <summary>
        /// Protects the web API with Microsoft identity platform (formerly Azure AD v2.0).
        /// This method expects the configuration file will have a section, named "AzureAd" as default, with the necessary settings to initialize authentication options.
        /// </summary>
        /// <returns>The authentication builder to chain.</returns>
        [RequiresUnreferencedCode("Calls Microsoft.Extensions.Configuration.ConfigurationBinder.Bind(IConfiguration, Object).")]
        [RequiresDynamicCode("Calls Microsoft.Extensions.Configuration.ConfigurationBinder.Bind(IConfiguration, Object).")]
        public MicrosoftIdentityAppCallsWebApiAuthenticationBuilder EnableTokenAcquisitionToCallDownstreamApi()
        {
            return EnableTokenAcquisitionToCallDownstreamApi(options => ConfigurationSection?.Bind(options));
        }
    }
}

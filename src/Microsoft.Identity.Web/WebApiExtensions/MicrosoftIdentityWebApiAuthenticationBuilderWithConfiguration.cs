// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Client;

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// Builder for web API authentication with configuration.
    /// </summary>
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

        /// <summary>
        /// Protects the web API with Microsoft identity platform (formerly Azure AD v2.0).
        /// This method expects the configuration file will have a section, named "AzureAd" as default, with the necessary settings to initialize authentication options.
        /// This overload is AOT-safe when binder delegates are provided.
        /// </summary>
        /// <param name="bindConfidentialClientApplicationOptions">
        /// An action to bind <see cref="ConfidentialClientApplicationOptions"/> from configuration.
        /// Pass <c>null</c> to use the default internal AOT-safe binder.
        /// </param>
        /// <returns>The authentication builder to chain.</returns>
        public new MicrosoftIdentityAppCallsWebApiAuthenticationBuilder EnableTokenAcquisitionToCallDownstreamApi(
            Action<ConfidentialClientApplicationOptions, IConfigurationSection?>? bindConfidentialClientApplicationOptions)
        {
            return base.EnableTokenAcquisitionToCallDownstreamApi(bindConfidentialClientApplicationOptions);
        }
    }
}

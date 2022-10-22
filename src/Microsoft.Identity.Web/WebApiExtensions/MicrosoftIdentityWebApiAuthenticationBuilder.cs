// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Client;
using Microsoft.Identity.Web.Internal;

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// Authentication builder for a web API.
    /// </summary>
    public class MicrosoftIdentityWebApiAuthenticationBuilder : MicrosoftIdentityBaseAuthenticationBuilder
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="services">The services being configured.</param>
        /// <param name="jwtBearerAuthenticationScheme">Default scheme used for OpenIdConnect.</param>
        /// <param name="configureJwtBearerOptions">ACtion called to configure the JwtBearer options.</param>
        /// <param name="configureMicrosoftIdentityOptions">Action called to configure
        /// the <see cref="MicrosoftIdentityOptions"/>Microsoft identity options.</param>
        /// <param name="configurationSection">Configuration section from which to
        /// get parameters.</param>
        internal MicrosoftIdentityWebApiAuthenticationBuilder(
            IServiceCollection services,
            string jwtBearerAuthenticationScheme,
            Action<JwtBearerOptions> configureJwtBearerOptions,
            Action<MicrosoftIdentityOptions> configureMicrosoftIdentityOptions,
            IConfigurationSection? configurationSection)
            : base(services, configurationSection)
        {
            JwtBearerAuthenticationScheme = jwtBearerAuthenticationScheme;
            ConfigureJwtBearerOptions = Throws.IfNull(configureJwtBearerOptions);
            ConfigureMicrosoftIdentityOptions = Throws.IfNull(configureMicrosoftIdentityOptions);

            Services.Configure(jwtBearerAuthenticationScheme, configureMicrosoftIdentityOptions);
        }

        private Action<MicrosoftIdentityOptions> ConfigureMicrosoftIdentityOptions { get; set; }

        private string JwtBearerAuthenticationScheme { get; set; }

        private Action<JwtBearerOptions> ConfigureJwtBearerOptions { get; set; }

        /// <summary>
        /// Protects the web API with Microsoft identity platform (formerly Azure AD v2.0).
        /// </summary>
        /// <param name="configureConfidentialClientApplicationOptions">The action to configure <see cref="ConfidentialClientApplicationOptions"/>.</param>
        /// <returns>The authentication builder to chain.</returns>
        public MicrosoftIdentityAppCallsWebApiAuthenticationBuilder EnableTokenAcquisitionToCallDownstreamApi(
            Action<ConfidentialClientApplicationOptions> configureConfidentialClientApplicationOptions)
        {
            _ = Throws.IfNull(configureConfidentialClientApplicationOptions);

            CallsWebApiImplementation(
                Services,
                JwtBearerAuthenticationScheme,
                configureConfidentialClientApplicationOptions,
                ConfigurationSection);

            return new MicrosoftIdentityAppCallsWebApiAuthenticationBuilder(
                Services,
                ConfigurationSection);
        }

        internal static void CallsWebApiImplementation(
            IServiceCollection services,
            string jwtBearerAuthenticationScheme,
            Action<ConfidentialClientApplicationOptions> configureConfidentialClientApplicationOptions,
            IConfigurationSection? configurationSection = null)
        {
            services.Configure(jwtBearerAuthenticationScheme, configureConfidentialClientApplicationOptions);

            WebApiBuilders.EnableTokenAcquisition(
                configureConfidentialClientApplicationOptions,
                jwtBearerAuthenticationScheme,
                services,
                configurationSection);

            services.AddHttpContextAccessor();

            services.AddOptions<JwtBearerOptions>(jwtBearerAuthenticationScheme)
                .Configure((options) =>
                {
                    options.Events ??= new JwtBearerEvents();

                    var onTokenValidatedHandler = options.Events.OnTokenValidated;

                    options.Events.OnTokenValidated = async context =>
                    {
                        context.HttpContext.StoreTokenUsedToCallWebAPI(context.SecurityToken as JwtSecurityToken);
                        await onTokenValidatedHandler(context).ConfigureAwait(false);
                    };
                });
        }
    }
}

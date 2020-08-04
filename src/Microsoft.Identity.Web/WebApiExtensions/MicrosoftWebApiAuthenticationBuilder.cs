// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Client;

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// Authentication builder for a web API.
    /// </summary>
    public class MicrosoftWebApiAuthenticationBuilder
    {
        /// <summary>
        ///  Constructor.
        /// </summary>
        /// <param name="services"> The services being configured.</param>
        /// <param name="jwtBearerAuthenticationScheme">Defaut scheme used for OpenIdConnect.</param>
        /// <param name="configureJwtBearerOptions">ACtion called to configure the JwtBearer options.</param>
        /// <param name="configureMicrosoftIdentityOptions">Action called to configure
        /// the <see cref="MicrosoftIdentityOptions"/>Microsoft identity options.</param>
        internal MicrosoftWebApiAuthenticationBuilder(
            IServiceCollection services,
            string jwtBearerAuthenticationScheme,
            Action<JwtBearerOptions> configureJwtBearerOptions,
            Action<MicrosoftIdentityOptions> configureMicrosoftIdentityOptions)
        {
            Services = services;
            JwtBearerAuthenticationScheme = jwtBearerAuthenticationScheme;
            ConfigureJwtBearerOptions = configureJwtBearerOptions;
            ConfigureMicrosoftIdentityOptions = configureMicrosoftIdentityOptions;

            if (ConfigureMicrosoftIdentityOptions == null)
            {
                throw new ArgumentNullException(nameof(configureMicrosoftIdentityOptions));
            }

            if (ConfigureJwtBearerOptions == null)
            {
                throw new ArgumentNullException(nameof(configureMicrosoftIdentityOptions));
            }

            Services.Configure(configureMicrosoftIdentityOptions);
        }

        /// <summary>
        /// The services being configured.
        /// </summary>
        public virtual IServiceCollection Services { get; private set; }

        private Action<MicrosoftIdentityOptions> ConfigureMicrosoftIdentityOptions { get; set; }

        private string JwtBearerAuthenticationScheme { get; set; }

        private Action<JwtBearerOptions> ConfigureJwtBearerOptions { get; set; }

        internal IConfigurationSection? ConfigurationSection { get; set; }

        /// <summary>
        /// Protects the web API with Microsoft identity platform (formerly Azure AD v2.0).
        /// This method expects the configuration file will have a section, named "AzureAd" as default, with the necessary settings to initialize authentication options.
        /// </summary>
        /// <returns>The authentication builder to chain.</returns>
        public MicrosoftWebApiAuthenticationBuilder CallsWebApi()
        {
            return CallsWebApi(options => ConfigurationSection.Bind(options));
        }

        /// <summary>
        /// Protects the web API with Microsoft identity platform (formerly Azure AD v2.0).
        /// </summary>
        /// <param name="configureConfidentialClientApplicationOptions">The action to configure <see cref="ConfidentialClientApplicationOptions"/>.</param>
        /// <returns>The authentication builder to chain.</returns>
        public MicrosoftWebApiAuthenticationBuilder CallsWebApi(
            Action<ConfidentialClientApplicationOptions> configureConfidentialClientApplicationOptions)
        {
            if (configureConfidentialClientApplicationOptions == null)
            {
                throw new ArgumentNullException(nameof(configureConfidentialClientApplicationOptions));
            }

            CallsWebApiImplementation(
                Services,
                JwtBearerAuthenticationScheme,
                configureConfidentialClientApplicationOptions);

            return this;
        }

        internal static void CallsWebApiImplementation(
            IServiceCollection services,
            string jwtBearerAuthenticationScheme,
            Action<ConfidentialClientApplicationOptions> configureConfidentialClientApplicationOptions)
        {
            services.Configure(configureConfidentialClientApplicationOptions);

            services.AddTokenAcquisition();
            services.AddHttpContextAccessor();

            services.AddOptions<JwtBearerOptions>(jwtBearerAuthenticationScheme)
                .Configure<IServiceProvider>((options, serviceProvider) =>
                {
                    options.Events ??= new JwtBearerEvents();

                    var onTokenValidatedHandler = options.Events.OnTokenValidated;

                    options.Events.OnTokenValidated = async context =>
                    {
                        await onTokenValidatedHandler(context).ConfigureAwait(false);
                        context.HttpContext.StoreTokenUsedToCallWebAPI(context.SecurityToken as JwtSecurityToken);
                        context.Success();
                    };
                });
        }
    }
}

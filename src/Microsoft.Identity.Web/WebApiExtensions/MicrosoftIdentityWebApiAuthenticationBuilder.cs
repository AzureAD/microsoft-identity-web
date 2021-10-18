// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Client;

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
            if (configureConfidentialClientApplicationOptions == null)
            {
                throw new ArgumentNullException(nameof(configureConfidentialClientApplicationOptions));
            }

            CallsWebApiImplementation(
                Services,
                JwtBearerAuthenticationScheme,
                configureConfidentialClientApplicationOptions);

            return new MicrosoftIdentityAppCallsWebApiAuthenticationBuilder(
                Services,
                ConfigurationSection);
        }

        internal static void CallsWebApiImplementation(
            IServiceCollection services,
            string jwtBearerAuthenticationScheme,
            Action<ConfidentialClientApplicationOptions> configureConfidentialClientApplicationOptions)
        {
            services.Configure(jwtBearerAuthenticationScheme, configureConfidentialClientApplicationOptions);

            services.AddTokenAcquisition();
            services.AddHttpContextAccessor();

            services.AddOptions<JwtBearerOptions>(jwtBearerAuthenticationScheme)
                .Configure<IServiceProvider, IOptionsMonitor<MergedOptions>, IOptionsMonitor<ConfidentialClientApplicationOptions>, IOptions<ConfidentialClientApplicationOptions>>((
                       options,
                       serviceProvider,
                       mergedOptionsMonitor,
                       ccaOptionsMonitor,
                       ccaOptions) =>
                {
                    MergedOptions mergedOptions = mergedOptionsMonitor.Get(jwtBearerAuthenticationScheme);

                    MergedOptions.UpdateMergedOptionsFromConfidentialClientApplicationOptions(ccaOptions.Value, mergedOptions); // legacy scenario w/out auth scheme
                    MergedOptions.UpdateMergedOptionsFromConfidentialClientApplicationOptions(ccaOptionsMonitor.Get(jwtBearerAuthenticationScheme), mergedOptions); // w/auth scheme

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

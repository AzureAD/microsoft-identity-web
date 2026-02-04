// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Diagnostics.CodeAnalysis;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Client;
using Microsoft.Identity.Web.Internal;
using Microsoft.IdentityModel.JsonWebTokens;

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
        [RequiresUnreferencedCode("Calls Microsoft.Identity.Web.MicrosoftIdentityBaseAuthenticationBuilder.MicrosoftIdentityBaseAuthenticationBuilder(IServiceCollection, IConfigurationSection).")]
        [RequiresDynamicCode("Calls Microsoft.Identity.Web.MicrosoftIdentityBaseAuthenticationBuilder.MicrosoftIdentityBaseAuthenticationBuilder(IServiceCollection, IConfigurationSection).")]
        internal MicrosoftIdentityWebApiAuthenticationBuilder(
            IServiceCollection services,
            string jwtBearerAuthenticationScheme,
            Action<JwtBearerOptions> configureJwtBearerOptions,
            Action<MicrosoftIdentityOptions> configureMicrosoftIdentityOptions,
            IConfigurationSection? configurationSection)
            : base(services, configurationSection)
        {
            JwtBearerAuthenticationScheme = jwtBearerAuthenticationScheme;
            _ = Throws.IfNull(configureJwtBearerOptions);
            _ = Throws.IfNull(configureMicrosoftIdentityOptions);

            Services.Configure(jwtBearerAuthenticationScheme, configureMicrosoftIdentityOptions);
        }

        private string JwtBearerAuthenticationScheme { get; set; }

        /// <summary>
        /// Protects the web API with Microsoft identity platform (formerly Azure AD v2.0).
        /// </summary>
        /// <param name="configureConfidentialClientApplicationOptions">The action to configure <see cref="ConfidentialClientApplicationOptions"/>.</param>
        /// <returns>The authentication builder to chain.</returns>
        [RequiresUnreferencedCode("Calls Microsoft.Identity.Web.Internal.WebApiBuilders.EnableTokenAcquisition(IServiceCollection, string, Action<ConfidentialClientApplicationOptions>, IConfigurationSection).")]
        [RequiresDynamicCode("Calls Microsoft.Identity.Web.Internal.WebApiBuilders.EnableTokenAcquisition(IServiceCollection, string, Action<ConfidentialClientApplicationOptions>, IConfigurationSection).")]
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

        [RequiresUnreferencedCode("Calls Microsoft.Identity.Web.Internal.WebApiBuilders.EnableTokenAcquisition(Action<ConfidentialClientApplicationOptions>, String, IServiceCollection, IConfigurationSection).")]
        [RequiresDynamicCode("Calls Microsoft.Identity.Web.Internal.WebApiBuilders.EnableTokenAcquisition(Action<ConfidentialClientApplicationOptions>, String, IServiceCollection, IConfigurationSection).")]
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
                        // Only pass through a token if it is of an expected type
                        context.HttpContext.StoreTokenUsedToCallWebAPI(context.SecurityToken is JwtSecurityToken or JsonWebToken ? context.SecurityToken : null);
                        await onTokenValidatedHandler(context).ConfigureAwait(false);
                    };
                });
        }
    }
}

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Client;

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// Extensions for AuthenticationBuilder for startup initialization of Web APIs.
    /// </summary>
    public static partial class WebApiCallsWebApiAuthenticationBuilderExtensions
    {
        /// <summary>
        /// Protects the Web API with Microsoft identity platform (formerly Azure AD v2.0)
        /// This supposes that the configuration files have a section named configSectionName (typically "AzureAD").
        /// </summary>
        /// <param name="builder">AuthenticationBuilder to which to add this configuration.</param>
        /// <param name="configuration">Configuration.</param>
        /// <param name="configSectionName">Section name in the config file (by default "AzureAD").</param>
        /// <param name="jwtBearerScheme">Scheme for the JwtBearer token.</param>
        /// <returns>The authentication builder to chain.</returns>
        public static AuthenticationBuilder AddMicrosoftWebApiCallsWebApi(
            this AuthenticationBuilder builder,
            IConfiguration configuration,
            string configSectionName = "AzureAd",
            string jwtBearerScheme = JwtBearerDefaults.AuthenticationScheme)
        {
            return builder.AddMicrosoftWebApiCallsWebApi(
                options => configuration.Bind(configSectionName, options),
                options => configuration.Bind(configSectionName, options),
                jwtBearerScheme);
        }

        /// <summary>
        /// Protects the Web API with Microsoft identity platform (formerly Azure AD v2.0)
        /// This supposes that the configuration files have a section named configSectionName (typically "AzureAD").
        /// </summary>
        /// <param name="builder">AuthenticationBuilder to which to add this configuration.</param>
        /// <param name="configureConfidentialClientApplicationOptions">The action to configure <see cref="ConfidentialClientApplicationOptions"/>.</param>
        /// <param name="configureMicrosoftIdentityOptions">The action to configure <see cref="MicrosoftIdentityOptions"/>.</param>
        /// <param name="jwtBearerScheme">Scheme for the JwtBearer token.</param>
        /// <returns>The authentication builder to chain.</returns>
        public static AuthenticationBuilder AddMicrosoftWebApiCallsWebApi(
            this AuthenticationBuilder builder,
            Action<ConfidentialClientApplicationOptions> configureConfidentialClientApplicationOptions,
            Action<MicrosoftIdentityOptions> configureMicrosoftIdentityOptions,
            string jwtBearerScheme = JwtBearerDefaults.AuthenticationScheme)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            builder.Services.Configure<ConfidentialClientApplicationOptions>(configureConfidentialClientApplicationOptions);
            builder.Services.Configure<MicrosoftIdentityOptions>(configureMicrosoftIdentityOptions);

            var microsoftIdentityOptions = new MicrosoftIdentityOptions();
            configureMicrosoftIdentityOptions(microsoftIdentityOptions);

            builder.Services.AddTokenAcquisition(microsoftIdentityOptions.SingletonTokenAcquisition);
            builder.Services.AddHttpContextAccessor();

            builder.Services.Configure<JwtBearerOptions>(jwtBearerScheme, options =>
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

            return builder;
        }
    }
}

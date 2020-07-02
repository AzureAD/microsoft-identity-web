// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Client;

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// Extensions for <see cref="AuthenticationBuilder"/> for startup initialization of web APIs.
    /// </summary>
    public static partial class WebApiCallsWebApiAuthenticationBuilderExtensions
    {
        /// <summary>
        /// Protects the web API with Microsoft identity platform (formerly Azure AD v2.0).
        /// This method expects the configuration file will have a section, named "AzureAd" as default, with the necessary settings to initialize authentication options.
        /// </summary>
        /// <param name="builder">The <see cref="AuthenticationBuilder"/> to which to add this configuration.</param>
        /// <param name="configuration">The configuration instance.</param>
        /// <param name="configSectionName">The section name in the config file (by default "AzureAd").</param>
        /// <param name="jwtBearerScheme">The scheme for the JWT bearer token.</param>
        /// <returns>The authentication builder to chain.</returns>
        public static AuthenticationBuilder AddMicrosoftWebApiCallsWebApi(
            this AuthenticationBuilder builder,
            IConfiguration configuration,
            string configSectionName = Constants.AzureAd,
            string jwtBearerScheme = JwtBearerDefaults.AuthenticationScheme)
        {
            return builder.AddMicrosoftWebApiCallsWebApi(
                options => configuration.Bind(configSectionName, options),
                options => configuration.Bind(configSectionName, options),
                jwtBearerScheme);
        }

        /// <summary>
        /// Protects the web API with Microsoft identity platform (formerly Azure AD v2.0).
        /// </summary>
        /// <param name="builder">The <see cref="AuthenticationBuilder"/> to which to add this configuration.</param>
        /// <param name="configureConfidentialClientApplicationOptions">The action to configure <see cref="ConfidentialClientApplicationOptions"/>.</param>
        /// <param name="configureMicrosoftIdentityOptions">The action to configure <see cref="MicrosoftIdentityOptions"/>.</param>
        /// <param name="jwtBearerScheme">The scheme for the JWT bearer token.</param>
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

            if (configureConfidentialClientApplicationOptions == null)
            {
                throw new ArgumentNullException(nameof(configureConfidentialClientApplicationOptions));
            }

            if (configureMicrosoftIdentityOptions == null)
            {
                throw new ArgumentNullException(nameof(configureMicrosoftIdentityOptions));
            }

            builder.Services.Configure(configureConfidentialClientApplicationOptions);
            builder.Services.Configure(configureMicrosoftIdentityOptions);

            builder.Services.AddTokenAcquisition();
            builder.Services.AddHttpContextAccessor();

            builder.Services.AddOptions<JwtBearerOptions>(jwtBearerScheme)
                .Configure<IServiceProvider>((options, serviceProvider) =>
            {
                MicrosoftIdentityOptions microsoftIdentityOptions = serviceProvider.GetRequiredService<IOptions<MicrosoftIdentityOptions>>().Value;

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

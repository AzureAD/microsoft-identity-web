// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.ComponentModel;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Client;

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// Extensions for <see cref="AuthenticationBuilder"/> for startup initialization of web APIs.
    /// </summary>
    public static partial class MicrosoftIdentityWebApiCallsWebApiAuthenticationBuilderExtensions
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
        [Obsolete("Rather use AddMicrosoftIdentityWebApi().EnableTokenAcquisitionToCallDownstreamApi. See https://aka.ms/ms-id-web/0.3.0-preview")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static AuthenticationBuilder AddMicrosoftWebApiCallsWebApi(
            this AuthenticationBuilder builder,
            IConfiguration configuration,
            string configSectionName = IDWebConstants.AzureAd,
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
        [Obsolete("Rather use AddMicrosoftIdentityWebApi().EnableTokenAcquisitionToCallDownstreamApi. See https://aka.ms/ms-id-web/0.3.0-preview")]
        [EditorBrowsable(EditorBrowsableState.Never)]
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

            builder.Services.Configure(configureMicrosoftIdentityOptions);

            MicrosoftIdentityWebApiAuthenticationBuilder.CallsWebApiImplementation(
                builder.Services,
                jwtBearerScheme,
                configureConfidentialClientApplicationOptions);

            return builder;
        }
    }
}

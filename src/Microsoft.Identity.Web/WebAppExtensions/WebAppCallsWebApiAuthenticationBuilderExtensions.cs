// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Client;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// Extensions for <see cref="AuthenticationBuilder"/> for startup initialization.
    /// </summary>
    public static class WebAppCallsWebApiAuthenticationBuilderExtensions
    {
        /// <summary>
        /// Add MSAL support to the web app or web API.
        /// This method expects the configuration file will have a section, named "AzureAd" as default, with the necessary settings to initialize authentication options.
        /// </summary>
        /// <param name="builder">The <see cref="AuthenticationBuilder"/> to which to add this configuration.</param>
        /// <param name="configuration">The configuration instance.</param>
        /// <param name="configSectionName">The name of the configuration section with the necessary
        /// settings to initialize authentication options.</param>
        /// <param name="openIdConnectScheme">Optional name for the OpenID Connect authentication scheme
        /// (by default, <c>OpenIdConnectDefaults.AuthenticationScheme</c>). This can be specified when you want to support
        /// several OpenID Connect identity providers.</param>
        /// <returns>The authentication builder for chaining.</returns>
        /// <remarks>This method cannot be used with Azure AD B2C, as with B2C an initial scope needs
        /// to be provided.
        /// </remarks>
        [Obsolete("Rather use MicrosoftAuthenticationBuilder.CallsWebApi")]
        public static MicrosoftWebAppAuthenticationBuilder AddMicrosoftWebAppCallsWebApi(
            this MicrosoftWebAppAuthenticationBuilder builder,
            IConfiguration configuration,
            string configSectionName = Constants.AzureAd,
            string openIdConnectScheme = OpenIdConnectDefaults.AuthenticationScheme)
        {
            return builder.AddMicrosoftWebAppCallsWebApi(
                null,
                options => configuration.Bind(configSectionName, options),
                options => configuration.Bind(configSectionName, options),
                openIdConnectScheme);
        }

        /// <summary>
        /// Add MSAL support to the web app or web API.
        /// </summary>
        /// <param name="builder">The <see cref="AuthenticationBuilder"/> to which to add this configuration.</param>
        /// <param name="configuration">The configuration instance.</param>
        /// <param name="initialScopes">Initial scopes to request at sign-in.</param>
        /// <param name="configSectionName">The name of the configuration section with the necessary
        /// settings to initialize authentication options.</param>
        /// <param name="openIdConnectScheme">Optional name for the OpenID Connect authentication scheme
        /// (by default, <c>OpenIdConnectDefaults.AuthenticationScheme</c>). This can be specified when you want to support
        /// several OpenID Connect identity providers.</param>
        /// <returns>The authentication builder for chaining.</returns>
        [Obsolete("Rather use MicrosoftAuthenticationBuilder.CallsWebApi")]
        public static MicrosoftWebAppAuthenticationBuilder AddMicrosoftWebAppCallsWebApi(
            this MicrosoftWebAppAuthenticationBuilder builder,
            IConfiguration configuration,
            IEnumerable<string> initialScopes,
            string configSectionName = Constants.AzureAd,
            string openIdConnectScheme = OpenIdConnectDefaults.AuthenticationScheme)
        {
            return builder.AddMicrosoftWebAppCallsWebApi(
                initialScopes,
                options => configuration.Bind(configSectionName, options),
                options => configuration.Bind(configSectionName, options),
                openIdConnectScheme);
        }

        /// <summary>
        /// Add MSAL support to the web app or web API.
        /// </summary>
        /// <param name="builder">The <see cref="AuthenticationBuilder"/> to which to add this configuration.</param>
        /// <param name="initialScopes">Initial scopes to request at sign-in.</param>
        /// <param name="configureMicrosoftIdentityOptions">The action to set the <see cref="MicrosoftIdentityOptions"/>.</param>
        /// <param name="configureConfidentialClientApplicationOptions">The action to set the <see cref="ConfidentialClientApplicationOptions"/>.</param>
        /// <param name="openIdConnectScheme">Optional name for the Open ID Connect authentication scheme
        /// (by default, <c>OpenIdConnectDefaults.AuthenticationScheme</c>). This can be specified when you want to support
        /// several OpenID Connect identity providers.</param>
        /// <returns>The authentication builder for chaining.</returns>
        [Obsolete("Rather use MicrosoftAuthenticationBuilder.CallsWebApi")]
        public static MicrosoftWebAppAuthenticationBuilder AddMicrosoftWebAppCallsWebApi(
            this MicrosoftWebAppAuthenticationBuilder builder,
            IEnumerable<string>? initialScopes,
            Action<MicrosoftIdentityOptions> configureMicrosoftIdentityOptions,
            Action<ConfidentialClientApplicationOptions> configureConfidentialClientApplicationOptions,
            string openIdConnectScheme = OpenIdConnectDefaults.AuthenticationScheme)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (configureMicrosoftIdentityOptions == null)
            {
                throw new ArgumentNullException(nameof(configureMicrosoftIdentityOptions));
            }

            if (configureConfidentialClientApplicationOptions == null)
            {
                throw new ArgumentNullException(nameof(configureConfidentialClientApplicationOptions));
            }

            MicrosoftWebAppAuthenticationBuilder.CallsWebApiImplementation(
                builder.Services,
                initialScopes,
                configureMicrosoftIdentityOptions,
                openIdConnectScheme,
                configureConfidentialClientApplicationOptions);
            return builder;
        }
    }
}

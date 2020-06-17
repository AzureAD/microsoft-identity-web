// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// Extensions for IServiceCollection for startup initialization of Web APIs.
    /// </summary>
    public static partial class WebAppAuthenticationBuilderExtensions
    {
        /// <summary>
        /// Add authentication with Microsoft identity platform.
        /// This method expects the configuration file will have a section, named "AzureAd" as default, with the necessary settings to initialize authentication options.
        /// </summary>
        /// <param name="builder">AuthenticationBuilder to which to add this configuration.</param>
        /// <param name="configuration">The IConfiguration object.</param>
        /// <param name="configSectionName">The configuration section with the necessary settings to initialize authentication options.</param>
        /// <param name="openIdConnectScheme">The OpenIdConnect scheme name to be used. By default it uses "OpenIdConnect".</param>
        /// <param name="cookieScheme">The Cookies scheme name to be used. By default it uses "Cookies".</param>
        /// <param name="subscribeToOpenIdConnectMiddlewareDiagnosticsEvents">
        /// Set to true if you want to debug, or just understand the OpenIdConnect events.
        /// </param>
        /// <returns>The authentication builder for chaining.</returns>
        [Obsolete("Use AddMicrosoftWebApp instead. See https://aka.ms/ms-id-web/net5")]
        public static AuthenticationBuilder AddSignIn(
            this AuthenticationBuilder builder,
            IConfiguration configuration,
            string configSectionName = "AzureAd",
            string openIdConnectScheme = OpenIdConnectDefaults.AuthenticationScheme,
            string cookieScheme = CookieAuthenticationDefaults.AuthenticationScheme,
            bool subscribeToOpenIdConnectMiddlewareDiagnosticsEvents = false) =>
        builder.AddMicrosoftWebApp(
            options => configuration.Bind(configSectionName, options),
            options => configuration.Bind(configSectionName, options),
            openIdConnectScheme,
            cookieScheme,
            subscribeToOpenIdConnectMiddlewareDiagnosticsEvents);

        /// <summary>
        /// Add authentication with Microsoft identity platform.
        /// This method expects the configuration file will have a section, named "AzureAd" as default, with the necessary settings to initialize authentication options.
        /// </summary>
        /// <param name="builder">AuthenticationBuilder to which to add this configuration.</param>
        /// <param name="configureOpenIdConnectOptions">The IConfiguration object.</param>
        /// <param name="configureMicrosoftIdentityOptions">The configuration section with the necessary settings to initialize authentication options.</param>
        /// <param name="openIdConnectScheme">The OpenIdConnect scheme name to be used. By default it uses "OpenIdConnect".</param>
        /// <param name="cookieScheme">The Cookies scheme name to be used. By default it uses "Cookies".</param>
        /// <param name="subscribeToOpenIdConnectMiddlewareDiagnosticsEvents">
        /// Set to true if you want to debug, or just understand the OpenIdConnect events.
        /// </param>
        /// <returns>The authentication builder for chaining.</returns>
        [Obsolete("Use AddMicrosoftWebApp instead. See https://aka.ms/ms-id-web/net5")]
        public static AuthenticationBuilder AddSignIn(
            this AuthenticationBuilder builder,
            Action<OpenIdConnectOptions> configureOpenIdConnectOptions,
            Action<MicrosoftIdentityOptions> configureMicrosoftIdentityOptions,
            string openIdConnectScheme = OpenIdConnectDefaults.AuthenticationScheme,
            string cookieScheme = CookieAuthenticationDefaults.AuthenticationScheme,
            bool subscribeToOpenIdConnectMiddlewareDiagnosticsEvents = false) =>
            builder.AddMicrosoftWebApp(
                configureOpenIdConnectOptions,
                configureMicrosoftIdentityOptions,
                openIdConnectScheme,
                cookieScheme,
                subscribeToOpenIdConnectMiddlewareDiagnosticsEvents);
    }
}

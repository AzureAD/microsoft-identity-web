// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Client;

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// Extensions for IServiceCollection for startup initialization of Web APIs.
    /// </summary>
    public static partial class WebApiServiceCollectionExtensions
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
        [Obsolete("Use AddMicrosoftWebApp instead.  See https://aka.ms/ms-id-web/net5")]
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
        [Obsolete("Use AddMicrosoftWebApp instead.  See https://aka.ms/ms-id-web/net5")]
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

        /// <summary>
        /// Add MSAL support to the Web App or Web API.
        /// </summary>
        /// <param name="services">Service collection to which to add authentication.</param>
        /// <param name="configuration">Configuration.</param>
        /// <param name="configSectionName">The name of the configuration section with the necessary
        /// settings to initialize authentication options.</param>
        /// <param name="openIdConnectScheme">Optional name for the open id connect authentication scheme
        /// (by default OpenIdConnectDefaults.AuthenticationScheme). This can be specified when you want to support
        /// several OpenIdConnect identity providers.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <remarks>This method cannot be used with Azure AD B2C as, with B2C an initial scope needs
        /// to be provided.
        /// </remarks>
        [Obsolete("Use AddMicrosoftWebAppCallsWebApi instead.  See https://aka.ms/ms-id-web/net5")]
        public static IServiceCollection AddWebAppCallsProtectedWebApi(
            this IServiceCollection services,
            IConfiguration configuration,
            string configSectionName = "AzureAd",
            string openIdConnectScheme = OpenIdConnectDefaults.AuthenticationScheme)
        {
            return services.AddAuthentication(openIdConnectScheme).AddMicrosoftWebAppCallsWebApi(
                configuration,
                configSectionName,
                openIdConnectScheme).Services;
        }

        /// <summary>
        /// Add MSAL support to the Web App or Web API.
        /// </summary>
        /// <param name="services">Service collection to which to add authentication.</param>
        /// <param name="configuration">Configuration.</param>
        /// <param name="initialScopes">Initial scopes to request at sign-in.</param>
        /// <param name="configSectionName">The name of the configuration section with the necessary
        /// settings to initialize authentication options.</param>
        /// <param name="openIdConnectScheme">Optional name for the open id connect authentication scheme
        /// (by default OpenIdConnectDefaults.AuthenticationScheme). This can be specified when you want to support
        /// several OpenIdConnect identity providers.</param>
        /// <returns>The service collection for chaining.</returns>
        [Obsolete("Use AddMicrosoftWebAppCallsWebApi instead.  See https://aka.ms/ms-id-web/net5")]
        public static IServiceCollection AddWebAppCallsProtectedWebApi(
            this IServiceCollection services,
            IConfiguration configuration,
            IEnumerable<string> initialScopes,
            string configSectionName = "AzureAd",
            string openIdConnectScheme = OpenIdConnectDefaults.AuthenticationScheme)
        {
            return services.AddAuthentication(openIdConnectScheme).AddMicrosoftWebAppCallsWebApi(
                configuration,
                initialScopes,
                configSectionName,
                openIdConnectScheme).Services;
        }

        /// <summary>
        /// Add MSAL support to the Web App or Web API.
        /// </summary>
        /// <param name="services">Service collection to which to add authentication.</param>
        /// <param name="initialScopes">Initial scopes to request at sign-in.</param>
        /// <param name="configureMicrosoftIdentityOptions">The action to set the <see cref="MicrosoftIdentityOptions"/>.</param>
        /// <param name="configureConfidentialClientApplicationOptions">The action to set the <see cref="ConfidentialClientApplicationOptions"/>.</param>
        /// <param name="openIdConnectScheme">Optional name for the open id connect authentication scheme
        /// (by default OpenIdConnectDefaults.AuthenticationScheme). This can be specified when you want to support
        /// several OpenIdConnect identity providers.</param>
        /// <returns>The service collection for chaining.</returns>
        [Obsolete("Use AddMicrosoftWebAppCallsWebApi instead.  See https://aka.ms/ms-id-web/net5")]
        public static IServiceCollection AddWebAppCallsProtectedWebApi(
            this IServiceCollection services,
            IEnumerable<string> initialScopes,
            Action<MicrosoftIdentityOptions> configureMicrosoftIdentityOptions,
            Action<ConfidentialClientApplicationOptions> configureConfidentialClientApplicationOptions,
            string openIdConnectScheme = OpenIdConnectDefaults.AuthenticationScheme)
        {
            return services.AddAuthentication(openIdConnectScheme).AddMicrosoftWebAppCallsWebApi(
               initialScopes,
               configureMicrosoftIdentityOptions,
               configureConfidentialClientApplicationOptions,
               openIdConnectScheme).Services;
        }
    }
}

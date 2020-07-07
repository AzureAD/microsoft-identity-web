// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Client;

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// Extensions for IServiceCollection for startup initialization.
    /// </summary>
    public static partial class WebAppServiceCollectionExtensions
    {
        /// <summary>
        /// Add authentication with Microsoft identity platform.
        /// This method expects the configuration file will have a section, (by default named "AzureAd"), with the necessary settings to
        /// initialize the authentication options.
        /// </summary>
        /// <param name="services">Service collection to which to add authentication.</param>
        /// <param name="configuration">The IConfiguration object.</param>
        /// <param name="configSectionName">The name of the configuration section with the necessary
        /// settings to initialize authentication options.</param>
        /// <param name="openIdConnectScheme">Optional name for the open id connect authentication scheme
        /// (by default OpenIdConnectDefaults.AuthenticationScheme). This can be specified when you want to support
        /// several OpenIdConnect identity providers.</param>
        /// <param name="cookieScheme">Optional name for the cookie authentication scheme
        /// (by default OpenIdConnectDefaults.AuthenticationScheme). </param>
        /// <param name="subscribeToOpenIdConnectMiddlewareDiagnosticsEvents">
        /// Set to true if you want to debug, or just understand the OpenIdConnect events.
        /// </param>
        /// <returns>The service collection for chaining.</returns>
        [Obsolete("Use AddMicrosoftWebAppAuth. See https://aka.ms/ms-id-web/net5")]
        public static IServiceCollection AddSignIn(
            this IServiceCollection services,
            IConfiguration configuration,
            string configSectionName = Constants.AzureAd,
            string openIdConnectScheme = OpenIdConnectDefaults.AuthenticationScheme,
            string cookieScheme = CookieAuthenticationDefaults.AuthenticationScheme,
            bool subscribeToOpenIdConnectMiddlewareDiagnosticsEvents = false)
        {
            AuthenticationBuilder builder = services.AddAuthentication(openIdConnectScheme);
            builder.AddMicrosoftWebApp(
                options => configuration.Bind(configSectionName, options),
                options => { },
                openIdConnectScheme,
                cookieScheme,
                subscribeToOpenIdConnectMiddlewareDiagnosticsEvents);
            return services;
        }

        /// <summary>
        /// Add authentication with Microsoft identity platform.
        /// This method expects the configuration file will have a section, named "AzureAd" as default, with the necessary settings to initialize authentication options.
        /// </summary>
        /// <param name="services">Service collection to which to add authentication.</param>
        /// <param name="configureOpenIdConnectOptions">the action to configure the <see cref="OpenIdConnectOptions"/>.</param>
        /// <param name="configureMicrosoftIdentityOptions">the action to configure the <see cref="MicrosoftIdentityOptions"/>.</param>
        /// <param name="openIdConnectScheme">Optional name for the open id connect authentication scheme
        /// (by default OpenIdConnectDefaults.AuthenticationScheme). This can be specified when you want to support
        /// several OpenIdConnect identity providers.</param>
        /// <param name="cookieScheme">Optional name for the cookie authentication scheme
        /// (by default OpenIdConnectDefaults.AuthenticationScheme). </param>
        /// <param name="subscribeToOpenIdConnectMiddlewareDiagnosticsEvents">
        /// Set to true if you want to debug, or just understand the OpenIdConnect events.
        /// </param>
        /// <returns>The service collection for chaining.</returns>
        [Obsolete("Use AuthenticationBuilder.AddMicrosoftWebApp. See https://aka.ms/ms-id-web/net5")]
        public static IServiceCollection AddSignIn(
            this IServiceCollection services,
            Action<OpenIdConnectOptions> configureOpenIdConnectOptions,
            Action<MicrosoftIdentityOptions> configureMicrosoftIdentityOptions,
            string openIdConnectScheme = OpenIdConnectDefaults.AuthenticationScheme,
            string cookieScheme = CookieAuthenticationDefaults.AuthenticationScheme,
            bool subscribeToOpenIdConnectMiddlewareDiagnosticsEvents = false)
        {
            AuthenticationBuilder builder = services.AddAuthentication(openIdConnectScheme);
            builder.AddMicrosoftWebApp(
                configureMicrosoftIdentityOptions,
                options => { },
                openIdConnectScheme,
                cookieScheme,
                subscribeToOpenIdConnectMiddlewareDiagnosticsEvents);
            return services;
        }

        /// <summary>
        /// Enable Web Apps to call APIs (acquiring tokens with MSAL.NET).
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
            string configSectionName = Constants.AzureAd,
            string openIdConnectScheme = OpenIdConnectDefaults.AuthenticationScheme)
        {
            return services.AddAuthentication(openIdConnectScheme).AddMicrosoftWebAppCallsWebApi(
                configuration,
                configSectionName,
                openIdConnectScheme).Services;
        }

        /// <summary>
        /// Enable Web Apps to call APIs (acquiring tokens with MSAL.NET).
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
            string configSectionName = Constants.AzureAd,
            string openIdConnectScheme = OpenIdConnectDefaults.AuthenticationScheme)
        {
            return services.AddAuthentication(openIdConnectScheme).AddMicrosoftWebAppCallsWebApi(
                configuration,
                initialScopes,
                configSectionName,
                openIdConnectScheme).Services;
        }

        /// <summary>
        /// Enable Web Apps to call APIs (acquiring tokens with MSAL.NET).
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

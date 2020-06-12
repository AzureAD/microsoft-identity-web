﻿using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// Extensions for IServiceCollection for startup initialization of Web APIs.
    /// </summary>
    public static partial class WebApiAuthenticationBuilderExtensions
    {
        /// <summary>
        /// Protects the Web API with Microsoft identity platform (formerly Azure AD v2.0)
        /// This method expects the configuration file will have a section, named "AzureAd" as default, with the necessary settings to initialize authentication options.
        /// </summary>
        /// <param name="builder">AuthenticationBuilder to which to add this configuration.</param>
        /// <param name="configuration">The Configuration object.</param>
        /// <param name="configSectionName">The configuration section with the necessary settings to initialize authentication options.</param>
        /// <param name="jwtBearerScheme">The JwtBearer scheme name to be used. By default it uses "Bearer".</param>
        /// <param name="tokenDecryptionCertificate">Token decryption certificate (null by default).</param>
        /// <param name="subscribeToJwtBearerMiddlewareDiagnosticsEvents">
        /// Set to true if you want to debug, or just understand the JwtBearer events.
        /// </param>
        /// <returns>The authentication builder to chain.</returns>
        [Obsolete("Rather use AddMicrosoftWebApi. See https://aka.ms/ms-id-web/net5")]
        public static AuthenticationBuilder AddProtectedWebApi2(
            this AuthenticationBuilder builder,
            IConfiguration configuration,
            string configSectionName = "AzureAd",
            string jwtBearerScheme = JwtBearerDefaults.AuthenticationScheme,
            X509Certificate2 tokenDecryptionCertificate = null,
            bool subscribeToJwtBearerMiddlewareDiagnosticsEvents = false)
        {
            return builder.AddMicrosoftWebApi(
                configuration,
                configSectionName,
                jwtBearerScheme,
                tokenDecryptionCertificate,
                subscribeToJwtBearerMiddlewareDiagnosticsEvents);
        }

        /// <summary>
        /// Protects the Web API with Microsoft identity platform (formerly Azure AD v2.0)
        /// This method expects the configuration file will have a section, named "AzureAd" as default, with the necessary settings to initialize authentication options.
        /// </summary>
        /// <param name="builder">AuthenticationBuilder to which to add this configuration.</param>
        /// <param name="configureJwtBearerOptions">The action to configure <see cref="JwtBearerOptions"/>.</param>
        /// <param name="configureMicrosoftIdentityOptions">The action to configure the <see cref="MicrosoftIdentityOptions"/>
        /// configuration options.</param>
        /// <param name="tokenDecryptionCertificate">Token decryption certificate.</param>
        /// <param name="jwtBearerScheme">The JwtBearer scheme name to be used. By default it uses "Bearer".</param>
        /// <param name="subscribeToJwtBearerMiddlewareDiagnosticsEvents">
        /// Set to true if you want to debug, or just understand the JwtBearer events.
        /// </param>
        /// <returns>The authentication builder to chain.</returns>
        [Obsolete("Rather use AddMicrosoftWebApi. See https://aka.ms/ms-id-web/net5")]
        public static AuthenticationBuilder AddProtectedWebApi2(
            this AuthenticationBuilder builder,
            Action<JwtBearerOptions> configureJwtBearerOptions,
            Action<MicrosoftIdentityOptions> configureMicrosoftIdentityOptions,
            X509Certificate2 tokenDecryptionCertificate = null,
            string jwtBearerScheme = JwtBearerDefaults.AuthenticationScheme,
            bool subscribeToJwtBearerMiddlewareDiagnosticsEvents = false)
            => builder.AddMicrosoftWebApi(
                configureJwtBearerOptions,
                configureMicrosoftIdentityOptions,
                tokenDecryptionCertificate,
                jwtBearerScheme,
                subscribeToJwtBearerMiddlewareDiagnosticsEvents);
    }
}

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Client;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// Extensions for IServiceCollection for startup initialization of Web APIs.
    /// </summary>
    public static class WebApiServiceCollectionExtensions
    {
        /// <summary>
        /// Protects the Web API with Microsoft identity platform (formerly Azure AD v2.0)
        /// This method expects the configuration file will have a section, named "AzureAd" as default, with the necessary settings to initialize authentication options.
        /// </summary>
        /// <param name="services">Service collection to which to add authentication.</param>
        /// <param name="configuration">The Configuration object.</param>
        /// <param name="configSectionName">The configuration section with the necessary settings to initialize authentication options.</param>
        /// <param name="jwtBearerScheme">The JwtBearer scheme name to be used. By default it uses "Bearer".</param>
        /// <param name="tokenDecryptionCertificate">Token decryption certificate (null by default).</param>
        /// <param name="subscribeToJwtBearerMiddlewareDiagnosticsEvents">
        /// Set to true if you want to debug, or just understand the JwtBearer events.
        /// </param>
        /// <returns>The service collection to chain.</returns>
        public static IServiceCollection AddProtectedWebApi(
            this IServiceCollection services,
            IConfiguration configuration,
            string configSectionName = "AzureAd",
            string jwtBearerScheme = JwtBearerDefaults.AuthenticationScheme,
            X509Certificate2 tokenDecryptionCertificate = null,
            bool subscribeToJwtBearerMiddlewareDiagnosticsEvents = false)
        {
            AuthenticationBuilder builder = services.AddAuthentication(jwtBearerScheme);
            builder.AddProtectedWebApi(options => configuration.Bind(configSectionName, options),
                options => configuration.Bind(configSectionName, options),
                tokenDecryptionCertificate,
                jwtBearerScheme,
                subscribeToJwtBearerMiddlewareDiagnosticsEvents);
            return services;
        }

        /// <summary>
        /// Protects the Web API with Microsoft identity platform (formerly Azure AD v2.0)
        /// This method expects the configuration file will have a section, named "AzureAd" as default, with the necessary settings to initialize authentication options.
        /// </summary>
        /// <param name="services">Service collection to which to add authentication.</param>
        /// <param name="configureJwtBearerOptions">The action to configure <see cref="JwtBearerOptions"/>.</param>
        /// <param name="configureMicrosoftIdentityOptions">The action to configure <see cref="configureMicrosoftIdentityOptions"/>.</param>
        /// <param name="jwtBearerScheme">The JwtBearer scheme name to be used. By default it uses "Bearer".</param>
        /// <param name="tokenDecryptionCertificate">Token decryption certificate (null by default).</param>
        /// <param name="subscribeToJwtBearerMiddlewareDiagnosticsEvents">
        /// Set to true if you want to debug, or just understand the JwtBearer events.
        /// </param>
        /// <returns>The service collection to chain.</returns>
        public static IServiceCollection AddProtectedWebApi(
            this IServiceCollection services,
            Action<JwtBearerOptions> configureJwtBearerOptions,
            Action<MicrosoftIdentityOptions> configureMicrosoftIdentityOptions,
            X509Certificate2 tokenDecryptionCertificate = null,
            string jwtBearerScheme = JwtBearerDefaults.AuthenticationScheme,
            bool subscribeToJwtBearerMiddlewareDiagnosticsEvents = false)
        {
            AuthenticationBuilder builder = services.AddAuthentication(jwtBearerScheme);
            builder.AddProtectedWebApi(
                configureJwtBearerOptions,
                configureMicrosoftIdentityOptions,
                tokenDecryptionCertificate,
                jwtBearerScheme,
                subscribeToJwtBearerMiddlewareDiagnosticsEvents);
            return services;
        }

        /// <summary>
        /// Protects the Web API with Microsoft identity platform (formerly Azure AD v2.0)
        /// This supposes that the configuration files have a section named configSectionName (typically "AzureAD").
        /// </summary>
        /// <param name="services">Service collection to which to add authentication.</param>
        /// <param name="configuration">Configuration.</param>
        /// <param name="configSectionName">Section name in the config file (by default "AzureAD").</param>
        /// <param name="jwtBearerScheme">Scheme for the JwtBearer token.</param>
        /// <returns>The service collection to chain.</returns>
        public static IServiceCollection AddProtectedWebApiCallsProtectedWebApi(
            this IServiceCollection services,
            IConfiguration configuration,
            string configSectionName = "AzureAd",
            string jwtBearerScheme = JwtBearerDefaults.AuthenticationScheme)
        {
            return services.AddProtectedWebApiCallsProtectedWebApi(
                options => configuration.Bind(configSectionName, options),
                options => configuration.Bind(configSectionName, options),
                jwtBearerScheme);
        }

        /// <summary>
        /// Protects the Web API with Microsoft identity platform (formerly Azure AD v2.0)
        /// This supposes that the configuration files have a section named configSectionName (typically "AzureAD").
        /// </summary>
        /// <param name="services">Service collection to which to add authentication.</param>
        /// <param name="configureConfidentialClientApplicationOptions">The action to configure <see cref="ConfidentialClientApplicationOptions"/>.</param>
        /// <param name="configureMicrosoftIdentityOptions">The action to configure <see cref="MicrosoftIdentityOptions"/>.</param>
        /// <param name="jwtBearerScheme">Scheme for the JwtBearer token.</param>
        /// <returns>The service collection to chain.</returns>
        public static IServiceCollection AddProtectedWebApiCallsProtectedWebApi(
            this IServiceCollection services,
            Action<ConfidentialClientApplicationOptions> configureConfidentialClientApplicationOptions,
            Action<MicrosoftIdentityOptions> configureMicrosoftIdentityOptions,
            string jwtBearerScheme = JwtBearerDefaults.AuthenticationScheme)
        {
            services.AddTokenAcquisition();
            services.AddHttpContextAccessor();
            services.Configure<ConfidentialClientApplicationOptions>(configureConfidentialClientApplicationOptions);
            services.Configure<MicrosoftIdentityOptions>(configureMicrosoftIdentityOptions);

            services.Configure<JwtBearerOptions>(jwtBearerScheme, options =>
            {
                options.Events ??= new JwtBearerEvents();

                options.Events.OnTokenValidated = async context =>
                {
                    context.HttpContext.StoreTokenUsedToCallWebAPI(context.SecurityToken as JwtSecurityToken);
                    context.Success();
                    await Task.FromResult(0).ConfigureAwait(false);
                };
            });

            return services;
        }
    }
}
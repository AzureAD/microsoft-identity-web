﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Linq;
using System.Security.Claims;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Web.Hosts;
using Microsoft.Identity.Web.OWIN;
using Microsoft.IdentityModel.Tokens;
using Microsoft.IdentityModel.Validators;
using Microsoft.Owin.Security.Jwt;
using Microsoft.Owin.Security.OAuth;
using Microsoft.Owin.Security.OpenIdConnect;
using Owin;

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// Extension methods on an ASP.NET application to add a web app or web API
    /// </summary>
    public static class AppBuilderExtension
    {
        static internal TokenAcquirerFactory? TokenAcquirerFactory { get; set; }
        /// <summary>
        /// Configuration
        /// </summary>
        static internal IConfiguration? Configuration { get { return TokenAcquirerFactory?.Configuration; } }

        /// <summary>
        /// Service Provider
        /// </summary>
        static internal IServiceProvider? ServiceProvider { get { return TokenAcquirerFactory?.ServiceProvider; } }

        /// <summary>
        /// Adds a protected web API
        /// </summary>
        /// <param name="app">Application builder</param>
        /// <param name="configureServices">Configure the services (if you want to call downstream web APIs).</param>
        /// <param name="configureMicrosoftIdentityOptions">Configure Microsoft identity options.</param>
        /// <param name="updateOptions">Update the OWIN options if you want to finesse token validation.</param>
        /// <param name="configurationSection">Configuration section in which to read the options.</param>
        /// <returns>the app builder to chain</returns>
        public static IAppBuilder AddMicrosoftIdentityWebApi(
            this IAppBuilder app,
            Action<IServiceCollection>? configureServices = null,
            Action<MicrosoftIdentityOptions>? configureMicrosoftIdentityOptions = null,
            Action<OAuthBearerAuthenticationOptions>? updateOptions = null,
            string configurationSection = "AzureAd")
        {
            TokenAcquirerFactory = TokenAcquirerFactory.GetDefaultInstance<OwinTokenAcquirerFactory>();
            var services = TokenAcquirerFactory.Services;

            // Configure the Microsoft identity options
            services.Configure(
                string.Empty, 
                configureMicrosoftIdentityOptions ?? (option =>
            {
                Configuration?.GetSection(configurationSection).Bind(option);
            }));
            
            if (configureServices != null)
            {
                configureServices(services);
            }

            // Replace the genenric host by an OWIN host
            ServiceDescriptor? tokenAcquisitionhost = services.FirstOrDefault(s => s.ServiceType == typeof(ITokenAcquisitionHost));
            if (tokenAcquisitionhost != null)
            {
                services.Remove(tokenAcquisitionhost);

                if (tokenAcquisitionhost.Lifetime == ServiceLifetime.Singleton)
                {
                    // The service was already added, but not with the right lifetime
                    services.AddSingleton<ITokenAcquisitionHost, OwinTokenAcquisitionHost>();     
                }
                else
                {
                    // The service is already added with the right lifetime
                    services.AddScoped<ITokenAcquisitionHost, OwinTokenAcquisitionHost>();
                }
            }

            TokenAcquirerFactory.Build();
            string instance = Configuration.GetValue<string>($"{configurationSection}:Instance");
            string tenantId = Configuration.GetValue<string>($"{configurationSection}:TenantId");
            string clientId = Configuration.GetValue<string>($"{configurationSection}:ClientId");
            string audience = Configuration.GetValue<string>($"{configurationSection}:Audience");
            string authority = instance + tenantId + "/v2.0";
            TokenValidationParameters tokenValidationParameters = new TokenValidationParameters
            {
                ValidAudience = audience ?? clientId,
                IssuerValidator = AadIssuerValidator.GetAadIssuerValidator(authority).Validate,
                SaveSigninToken = true,
            };
            OAuthBearerAuthenticationOptions options = new OAuthBearerAuthenticationOptions
            {
                AccessTokenFormat = new JwtFormat(tokenValidationParameters, new OpenIdConnectCachingSecurityTokenProvider(authority+ "/.well-known/openid-configuration"))
            };

            if (updateOptions != null)
            {
                updateOptions(options);
            }

            return app.UseOAuthBearerAuthentication(options);
        }

        /// <summary>
        /// Adds a protected web app
        /// </summary>
        /// <param name="app">Application builder</param>
        /// <param name="configureServices">Configure the services (if you want to call downstream web APIs).</param>
        /// <param name="configureMicrosoftIdentityOptions">Configure Microsoft identity options.</param>
        /// <param name="updateOptions">Update the OWIN options if you want to finesse the OpenIdConnect options.</param>
        /// <param name="configurationSection">Configuration section in which to read the options.</param>
        /// <returns>the app builder to chain</returns>
        public static IAppBuilder AddMicrosoftIdentityWebApp(
            this IAppBuilder app,
            Action<IServiceCollection>? configureServices = null,
            Action<MicrosoftIdentityOptions>? configureMicrosoftIdentityOptions = null,
            Action<OpenIdConnectAuthenticationOptions>? updateOptions = null,
            string configurationSection = "AzureAd")
        {
            TokenAcquirerFactory = TokenAcquirerFactory.GetDefaultInstance<OwinTokenAcquirerFactory>();
            var services = TokenAcquirerFactory.Services;

            // Configure the Microsoft identity options
            services.Configure(
                string.Empty,
                configureMicrosoftIdentityOptions ?? (option =>
                {
                    Configuration?.GetSection(configurationSection).Bind(option);
                }));

            if (configureServices != null)
            {
                configureServices(services);
            }

            // Replace the genenric host by an OWIN host
            ServiceDescriptor? tokenAcquisitionhost = services.FirstOrDefault(s => s.ServiceType == typeof(ITokenAcquisitionHost));
            if (tokenAcquisitionhost != null)
            {
                services.Remove(tokenAcquisitionhost);

                if (tokenAcquisitionhost.Lifetime == ServiceLifetime.Singleton)
                {
                    // The service was already added, but not with the right lifetime
                    services.AddSingleton<ITokenAcquisitionHost, OwinTokenAcquisitionHost>();
                }
                else
                {
                    // The service is already added with the right lifetime
                    services.AddScoped<ITokenAcquisitionHost, OwinTokenAcquisitionHost>();
                }
            }

            TokenAcquirerFactory.Build();
            string instance = Configuration.GetValue<string>($"{configurationSection}:Instance");
            string tenantId = Configuration.GetValue<string>($"{configurationSection}:TenantId");
            string clientId = Configuration.GetValue<string>($"{configurationSection}:ClientId");
            string postLogoutRedirectUri = Configuration.GetValue<string>($"{configurationSection}:SignedOutCallbackPath");
            string authority = instance + tenantId + "/v2.0";

            OpenIdConnectAuthenticationOptions options = new OpenIdConnectAuthenticationOptions
            {
                ClientId = clientId,
                Authority = authority,
                PostLogoutRedirectUri = postLogoutRedirectUri,

                Notifications = new OpenIdConnectAuthenticationNotifications()
                {
                    SecurityTokenValidated = (context) =>
                    {
                        string name = context.AuthenticationTicket.Identity.FindFirst("preferred_username").Value;
                        context.AuthenticationTicket.Identity.AddClaim(new Claim(ClaimTypes.Name, name, string.Empty));
                        return System.Threading.Tasks.Task.FromResult(0);
                    }
                }
            };

            if (updateOptions != null)
            {
                updateOptions(options);
            }

            return app.UseOpenIdConnectAuthentication(options);
        }
    }
}

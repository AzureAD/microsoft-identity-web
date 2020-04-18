// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Client;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// Extensions for IServiceCollection for startup initialization.
    /// </summary>
    public static class WebAppServiceCollectionExtensions
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
        /// <param name="cookieScheme">Optional name for the acookie uthentication scheme
        /// (by default OpenIdConnectDefaults.AuthenticationScheme). </param>
        /// <param name="subscribeToOpenIdConnectMiddlewareDiagnosticsEvents">
        /// Set to true if you want to debug, or just understand the OpenIdConnect events.
        /// </param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddSignIn(
            this IServiceCollection services,
            IConfiguration configuration,
            string configSectionName = "AzureAd",
            string openIdConnectScheme = OpenIdConnectDefaults.AuthenticationScheme,
            string cookieScheme = CookieAuthenticationDefaults.AuthenticationScheme,
            bool subscribeToOpenIdConnectMiddlewareDiagnosticsEvents = false)
        {
            AuthenticationBuilder builder = services.AddAuthentication(openIdConnectScheme);
            builder.AddSignIn(
                options => configuration.Bind(configSectionName, options),
                options => configuration.Bind(configSectionName, options),
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
        /// <param name="configureMicrosoftIdentityOptions">the action to set the <see cref="MicrosoftIdentityOptions"/>.</param>
        /// <param name="configureMicrosoftIdentityOptions">the action to set the <see cref="MicrosoftIdentityOptions"/>.</param>
        /// <param name="openIdConnectScheme">Optional name for the open id connect authentication scheme
        /// (by default OpenIdConnectDefaults.AuthenticationScheme). This can be specified when you want to support
        /// several OpenIdConnect identity providers.</param>
        /// <param name="cookieScheme">Optional name for the acookie uthentication scheme
        /// (by default OpenIdConnectDefaults.AuthenticationScheme). </param>
        /// <param name="subscribeToOpenIdConnectMiddlewareDiagnosticsEvents">
        /// Set to true if you want to debug, or just understand the OpenIdConnect events.
        /// </param>
        /// <returns>Yhe service collection for chaining.</returns>
        public static IServiceCollection AddSignIn(
            this IServiceCollection services,
            Action<OpenIdConnectOptions> configureOpenIdConnectOptions,
            Action<MicrosoftIdentityOptions> configureMicrosoftIdentityOptions,
            string openIdConnectScheme = OpenIdConnectDefaults.AuthenticationScheme,
            string cookieScheme = CookieAuthenticationDefaults.AuthenticationScheme,
            bool subscribeToOpenIdConnectMiddlewareDiagnosticsEvents = false)
        {
            AuthenticationBuilder builder = services.AddAuthentication(openIdConnectScheme);
            builder.AddSignIn(
                 configureOpenIdConnectOptions,
                 configureMicrosoftIdentityOptions,
                 openIdConnectScheme,
                 cookieScheme,
                 subscribeToOpenIdConnectMiddlewareDiagnosticsEvents);
            return services;
        }

        /// <summary>
        /// Add MSAL support to the Web App or Web API.
        /// </summary>
        /// <param name="services">Service collection to which to add authentication.</param>
        /// <param name="initialScopes">Initial scopes to request at sign-in.</param>
        /// <param name="configSectionName">The name of the configuration section with the necessary
        /// settings to initialize authentication options.</param>
        /// <param name="openIdConnectScheme">Optional name for the open id connect authentication scheme
        /// (by default OpenIdConnectDefaults.AuthenticationScheme). This can be specified when you want to support
        /// several OpenIdConnect identity providers.</param>
        /// <returns>Yhe service collection for chaining.</returns>
        public static IServiceCollection AddWebAppCallsProtectedWebApi(
            this IServiceCollection services,
            IConfiguration configuration,
            IEnumerable<string> initialScopes,
            string configSectionName = "AzureAd",
            string openIdConnectScheme = OpenIdConnectDefaults.AuthenticationScheme)
        {
            return AddWebAppCallsProtectedWebApi(
                services,
                initialScopes,
                options => configuration.Bind(configSectionName, options),
                options => configuration.Bind(configSectionName, options),
                openIdConnectScheme);
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
        public static IServiceCollection AddWebAppCallsProtectedWebApi(
            this IServiceCollection services,
            IEnumerable<string> initialScopes,
            Action<MicrosoftIdentityOptions> configureMicrosoftIdentityOptions,
            Action<ConfidentialClientApplicationOptions> configureConfidentialClientApplicationOptions,
            string openIdConnectScheme = OpenIdConnectDefaults.AuthenticationScheme)
        {
            // Ensure that configuration options for MSAL.NET, HttpContext accessor and the Token acquisition service
            // (encapsulating MSAL.NET) are available through dependency injection
            services.Configure<MicrosoftIdentityOptions>(configureMicrosoftIdentityOptions);
            services.Configure<ConfidentialClientApplicationOptions>(configureConfidentialClientApplicationOptions);

            services.AddHttpContextAccessor();
            services.AddTokenAcquisition();

            services.Configure<OpenIdConnectOptions>(openIdConnectScheme, options =>
            {
                options.ResponseType = OpenIdConnectResponseType.CodeIdToken;

                // This scope is needed to get a refresh token when users sign-in with their Microsoft personal accounts
                // It's required by MSAL.NET and automatically provided when users sign-in with work or school accounts
                options.Scope.Add(OidcConstants.ScopeOfflineAccess);
                if (initialScopes != null)
                {
                    foreach (string scope in initialScopes)
                    {
                        if (!options.Scope.Contains(scope))
                        {
                            options.Scope.Add(scope);
                        }
                    }
                }

                // Handling the auth redemption by MSAL.NET so that a token is available in the token cache
                // where it will be usable from Controllers later (through the TokenAcquisition service)
                var codeReceivedHandler = options.Events.OnAuthorizationCodeReceived;
                options.Events.OnAuthorizationCodeReceived = async context =>
                {
                    var tokenAcquisition = context.HttpContext.RequestServices.GetRequiredService<ITokenAcquisition>() as ITokenAcquisitionInternal;
                    await tokenAcquisition.AddAccountToCacheFromAuthorizationCodeAsync(context, options.Scope).ConfigureAwait(false);
                    await codeReceivedHandler(context).ConfigureAwait(false);
                };

                // Handling the token validated to get the client_info for cases where tenantId is not present (example: B2C)
                var onTokenValidatedHandler = options.Events.OnTokenValidated;
                options.Events.OnTokenValidated = async context =>
                {
                    if (!context.Principal.HasClaim(c => c.Type == ClaimConstants.Tid || c.Type == ClaimConstants.TenantId))
                    {
                        ClientInfo clientInfoFromServer;
                        if (context.Request.Form.ContainsKey(ClaimConstants.ClientInfo))
                        {
                            context.Request.Form.TryGetValue(ClaimConstants.ClientInfo, out Microsoft.Extensions.Primitives.StringValues value);

                            if (!string.IsNullOrEmpty(value))
                            {
                                clientInfoFromServer = ClientInfo.CreateFromJson(value);

                                if (clientInfoFromServer != null)
                                {
                                    context.Principal.Identities.FirstOrDefault().AddClaim(new Claim(ClaimConstants.Tid, clientInfoFromServer.UniqueTenantIdentifier));
                                    context.Principal.Identities.FirstOrDefault().AddClaim(new Claim(ClaimConstants.UniqueObjectIdentifier, clientInfoFromServer.UniqueObjectIdentifier));
                                }
                            }
                        }
                    }

                    await onTokenValidatedHandler(context).ConfigureAwait(false);
                };

                // Handling the sign-out: removing the account from MSAL.NET cache
                var signOutHandler = options.Events.OnRedirectToIdentityProviderForSignOut;
                options.Events.OnRedirectToIdentityProviderForSignOut = async context =>
                {
                    // Remove the account from MSAL.NET token cache
                    var tokenAcquisition = context.HttpContext.RequestServices.GetRequiredService<ITokenAcquisition>() as ITokenAcquisitionInternal;
                    await tokenAcquisition.RemoveAccountAsync(context).ConfigureAwait(false);
                    await signOutHandler(context).ConfigureAwait(false);
                };
            });
            return services;
        }
    }
}
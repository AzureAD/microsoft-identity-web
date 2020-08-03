// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Client;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// Authentication builder specific for Microsoft identity platform.
    /// </summary>
    public class MicrosoftWebAppAuthenticationBuilder
    {
        /// <summary>
        ///  Constructor.
        /// </summary>
        /// <param name="services"> The services being configured.</param>
        /// <param name="openIdConnectScheme">Defaut scheme used for OpenIdConnect.</param>
        /// <param name="configureMicrosoftIdentityOptions">Action called to configure
        /// the <see cref="MicrosoftIdentityOptions"/>Microsoft identity options.</param>
        internal MicrosoftWebAppAuthenticationBuilder(
            IServiceCollection services,
            string openIdConnectScheme,
            Action<MicrosoftIdentityOptions> configureMicrosoftIdentityOptions)
        {
            Services = services;
            _openIdConnectScheme = openIdConnectScheme;
            _configureMicrosoftIdentityOptions = configureMicrosoftIdentityOptions;

            if (_configureMicrosoftIdentityOptions == null)
            {
                throw new ArgumentNullException(nameof(configureMicrosoftIdentityOptions));
            }
        }

        /// <summary>
        /// The services being configured.
        /// </summary>
        public virtual IServiceCollection Services { get; private set; }

        private Action<MicrosoftIdentityOptions> _configureMicrosoftIdentityOptions { get; set; }

        private string _openIdConnectScheme { get; set; }

        internal IConfigurationSection? ConfigurationSection { get; set; }

        /// <summary>
        /// Add MSAL support to the web app or web API.
        /// This method expects the configuration file will have a section, named "AzureAd" as default, with the necessary settings to initialize authentication options.
        /// </summary>
        /// <param name="configuration">The configuration instance.</param>
        /// <param name="configSectionName">The name of the configuration section with the necessary
        /// settings to initialize authentication options.</param>
        /// <returns>The authentication builder for chaining.</returns>
        /// <remarks>This method cannot be used with Azure AD B2C, as with B2C an initial scope needs
        /// to be provided.
        /// </remarks>
        public MicrosoftWebAppAuthenticationBuilder CallsWebApi()
        {
            return CallsWebApi(null);
        }

        /// <summary>
        /// Add MSAL support to the web app or web API.
        /// </summary>
        /// <param name="configuration">The configuration instance.</param>
        /// <param name="initialScopes">Initial scopes to request at sign-in.</param>
        /// <param name="configSectionName">The name of the configuration section with the necessary
        /// settings to initialize authentication options.</param>
        /// <returns>The authentication builder for chaining.</returns>
        [Obsolete("Rather use MicrosoftAuthenticationBuilder.CallsWebApi")]
        public MicrosoftWebAppAuthenticationBuilder CallsWebApi(
            IEnumerable<string> initialScopes)
        {
            return CallsWebApi(
                initialScopes,
                options => ConfigurationSection.Bind(options));
        }

        /// <summary>
        /// Add MSAL support to the web app or web API.
        /// </summary>
        /// <param name="configurationSection">The configuration section instance from which to extract the values
        /// </param>
        /// <param name="initialScopes">Initial scopes to request at sign-in.</param>
        /// <param name="configSectionName">The name of the configuration section with the necessary
        /// settings to initialize authentication options.</param>
        /// <returns>The authentication builder for chaining.</returns>
        [Obsolete("Rather use MicrosoftAuthenticationBuilder.CallsWebApi")]
        public MicrosoftWebAppAuthenticationBuilder CallsWebApi(
            IConfigurationSection configurationSection,
            IEnumerable<string> initialScopes)
        {
            return CallsWebApi(
                initialScopes,
                options => configurationSection.Bind(options));
        }

        /// <summary>
        /// The Web app calls a web api.
        /// </summary>
        /// <param name="initialScopes">Initial scopes.</param>
        /// <param name="configureConfidentialClientApplicationOptions">Action to configure the
        /// MSAL.NET confidential client application options.</param>
        /// <returns>the builder itself for chaining.</returns>
        public MicrosoftWebAppAuthenticationBuilder CallsWebApi(
                   IEnumerable<string>? initialScopes,
                   Action<ConfidentialClientApplicationOptions> configureConfidentialClientApplicationOptions)
        {
            if (configureConfidentialClientApplicationOptions == null)
            {
                throw new ArgumentNullException(nameof(configureConfidentialClientApplicationOptions));
            }

            CallsWebApiImplementation(
                Services,
                initialScopes,
                _configureMicrosoftIdentityOptions,
                _openIdConnectScheme,
                configureConfidentialClientApplicationOptions);
            return this;
        }

        internal static void CallsWebApiImplementation(
            IServiceCollection services,
            IEnumerable<string>? initialScopes,
            Action<MicrosoftIdentityOptions> configureMicrosoftIdentityOptions,
            string openIdConnectScheme,
            Action<ConfidentialClientApplicationOptions> configureConfidentialClientApplicationOptions)
        {
            // Ensure that configuration options for MSAL.NET, HttpContext accessor and the Token acquisition service
            // (encapsulating MSAL.NET) are available through dependency injection
            services.Configure(configureMicrosoftIdentityOptions);
            services.Configure(configureConfidentialClientApplicationOptions);

            services.AddHttpContextAccessor();

            services.AddTokenAcquisition();

            services.AddOptions<OpenIdConnectOptions>(openIdConnectScheme)
                 .Configure<IServiceProvider>((options, serviceProvider) =>
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
                         var tokenAcquisition = context.HttpContext.RequestServices.GetRequiredService<ITokenAcquisitionInternal>();
                         await tokenAcquisition.AddAccountToCacheFromAuthorizationCodeAsync(context, options.Scope).ConfigureAwait(false);
                         await codeReceivedHandler(context).ConfigureAwait(false);
                     };

                     // Handling the token validated to get the client_info for cases where tenantId is not present (example: B2C)
                     var onTokenValidatedHandler = options.Events.OnTokenValidated;
                     options.Events.OnTokenValidated = async context =>
                     {
                         string? clientInfo = context.ProtocolMessage?.GetParameter(ClaimConstants.ClientInfo);

                         if (!string.IsNullOrEmpty(clientInfo))
                         {
                             ClientInfo? clientInfoFromServer = ClientInfo.CreateFromJson(clientInfo);

                             if (clientInfoFromServer != null)
                             {
                                 context.Principal.Identities.FirstOrDefault()?.AddClaim(new Claim(ClaimConstants.UniqueTenantIdentifier, clientInfoFromServer.UniqueTenantIdentifier));
                                 context.Principal.Identities.FirstOrDefault()?.AddClaim(new Claim(ClaimConstants.UniqueObjectIdentifier, clientInfoFromServer.UniqueObjectIdentifier));
                             }
                         }

                         await onTokenValidatedHandler(context).ConfigureAwait(false);
                     };

                     // Handling the sign-out: removing the account from MSAL.NET cache
                     var signOutHandler = options.Events.OnRedirectToIdentityProviderForSignOut;
                     options.Events.OnRedirectToIdentityProviderForSignOut = async context =>
                     {
                         // Remove the account from MSAL.NET token cache
                         var tokenAcquisition = context.HttpContext.RequestServices.GetRequiredService<ITokenAcquisitionInternal>();
                         await tokenAcquisition.RemoveAccountAsync(context).ConfigureAwait(false);
                         await signOutHandler(context).ConfigureAwait(false);
                     };
                 });
        }
    }
}

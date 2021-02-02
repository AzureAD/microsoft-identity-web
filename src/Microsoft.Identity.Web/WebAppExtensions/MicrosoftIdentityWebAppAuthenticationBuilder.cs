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
    public class MicrosoftIdentityWebAppAuthenticationBuilder : MicrosoftIdentityBaseAuthenticationBuilder
    {
        /// <summary>
        ///  Constructor.
        /// </summary>
        /// <param name="services"> The services being configured.</param>
        /// <param name="openIdConnectScheme">Default scheme used for OpenIdConnect.</param>
        /// <param name="configureMicrosoftIdentityOptions">Action called to configure
        /// the <see cref="MicrosoftIdentityOptions"/>Microsoft identity options.</param>
        /// <param name="configurationSection">Optional configuration section.</param>
        internal MicrosoftIdentityWebAppAuthenticationBuilder(
            IServiceCollection services,
            string openIdConnectScheme,
            Action<MicrosoftIdentityOptions> configureMicrosoftIdentityOptions,
            IConfigurationSection? configurationSection)
            : base(services, configurationSection)
        {
            OpenIdConnectScheme = openIdConnectScheme;
            ConfigureMicrosoftIdentityOptions = configureMicrosoftIdentityOptions;

            if (ConfigureMicrosoftIdentityOptions == null)
            {
                throw new ArgumentNullException(nameof(configureMicrosoftIdentityOptions));
            }
        }

        private Action<MicrosoftIdentityOptions> ConfigureMicrosoftIdentityOptions { get; set; }

        private string OpenIdConnectScheme { get; set; }

        /// <summary>
        /// The web app calls a web API.
        /// </summary>
        /// <param name="initialScopes">Initial scopes.</param>
        /// <returns>The builder itself for chaining.</returns>
        public MicrosoftIdentityAppCallsWebApiAuthenticationBuilder EnableTokenAcquisitionToCallDownstreamApi(
            IEnumerable<string>? initialScopes = null)
        {
            return EnableTokenAcquisitionToCallDownstreamApi(null, initialScopes);
        }

        /// <summary>
        /// The web app calls a web API. This override enables you to specify the
        /// ConfidentialClientApplicationOptions (from MSAL.NET) programmatically.
        /// </summary>
        /// <param name="configureConfidentialClientApplicationOptions">Action to configure the
        /// MSAL.NET confidential client application options.</param>
        /// <param name="initialScopes">Initial scopes.</param>
        /// <returns>The builder itself for chaining.</returns>
        public MicrosoftIdentityAppCallsWebApiAuthenticationBuilder EnableTokenAcquisitionToCallDownstreamApi(
            Action<ConfidentialClientApplicationOptions>? configureConfidentialClientApplicationOptions,
            IEnumerable<string>? initialScopes = null)
        {
            WebAppCallsWebApiImplementation(
                Services,
                initialScopes,
                ConfigureMicrosoftIdentityOptions,
                OpenIdConnectScheme,
                configureConfidentialClientApplicationOptions);
            return new MicrosoftIdentityAppCallsWebApiAuthenticationBuilder(
                Services,
                ConfigurationSection);
        }

        internal static void WebAppCallsWebApiImplementation(
            IServiceCollection services,
            IEnumerable<string>? initialScopes,
            Action<MicrosoftIdentityOptions> configureMicrosoftIdentityOptions,
            string openIdConnectScheme,
            Action<ConfidentialClientApplicationOptions>? configureConfidentialClientApplicationOptions)
        {
            // Ensure that configuration options for MSAL.NET, HttpContext accessor and the Token acquisition service
            // (encapsulating MSAL.NET) are available through dependency injection
            services.Configure(configureMicrosoftIdentityOptions);

            if (configureConfidentialClientApplicationOptions != null)
            {
                services.Configure(configureConfidentialClientApplicationOptions);
            }

            services.AddHttpContextAccessor();

            if (AppServicesAuthenticationInformation.IsAppServicesAadAuthenticationEnabled)
            {
                services.AddScoped<ITokenAcquisition, AppServicesAuthenticationTokenAcquisition>();
            }
            else
            {
                services.AddTokenAcquisition();

                services.AddOptions<OpenIdConnectOptions>(openIdConnectScheme)
                   .Configure<IServiceProvider>((options, serviceProvider) =>
                   {
                       options.ResponseType = OpenIdConnectResponseType.Code;
                       options.UsePkce = false;

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
                           var tokenAcquisition = context!.HttpContext.RequestServices.GetRequiredService<ITokenAcquisitionInternal>();
                           await tokenAcquisition.AddAccountToCacheFromAuthorizationCodeAsync(context, options.Scope).ConfigureAwait(false);
                           await codeReceivedHandler(context).ConfigureAwait(false);
                       };

                       // Handling the token validated to get the client_info for cases where tenantId is not present (example: B2C)
                       var onTokenValidatedHandler = options.Events.OnTokenValidated;
                       options.Events.OnTokenValidated = async context =>
                       {
                           string? clientInfo = context!.ProtocolMessage?.GetParameter(ClaimConstants.ClientInfo);

                           if (!string.IsNullOrEmpty(clientInfo))
                           {
                               ClientInfo? clientInfoFromServer = ClientInfo.CreateFromJson(clientInfo);

                               if (clientInfoFromServer != null)
                               {
                                   context!.Principal!.Identities.FirstOrDefault()?.AddClaim(new Claim(ClaimConstants.UniqueTenantIdentifier, clientInfoFromServer.UniqueTenantIdentifier));
                                   context!.Principal!.Identities.FirstOrDefault()?.AddClaim(new Claim(ClaimConstants.UniqueObjectIdentifier, clientInfoFromServer.UniqueObjectIdentifier));
                               }
                           }

                           await onTokenValidatedHandler(context).ConfigureAwait(false);
                       };

                       // Handling the sign-out: removing the account from MSAL.NET cache
                       var signOutHandler = options.Events.OnRedirectToIdentityProviderForSignOut;
                       options.Events.OnRedirectToIdentityProviderForSignOut = async context =>
                       {
                           // Remove the account from MSAL.NET token cache
                           var tokenAcquisition = context!.HttpContext.RequestServices.GetRequiredService<ITokenAcquisitionInternal>();
                           await tokenAcquisition.RemoveAccountAsync(context).ConfigureAwait(false);
                           await signOutHandler(context).ConfigureAwait(false);
                       };
                   });
            }
        }
    }
}

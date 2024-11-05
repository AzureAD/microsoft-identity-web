// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Abstractions;
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
#if NET6_0_OR_GREATER && !NET8_0_OR_GREATER
        [RequiresUnreferencedCode("Calls Microsoft.Identity.Web.MicrosoftIdentityBaseAuthenticationBuilder.MicrosoftIdentityBaseAuthenticationBuilder(IServiceCollection, IConfigurationSection).")]
#endif
        internal MicrosoftIdentityWebAppAuthenticationBuilder(
            IServiceCollection services,
            string openIdConnectScheme,
            Action<MicrosoftIdentityOptions> configureMicrosoftIdentityOptions,
            IConfigurationSection? configurationSection)
            : base(services, configurationSection)
        {
            OpenIdConnectScheme = openIdConnectScheme;
            ConfigureMicrosoftIdentityOptions = Throws.IfNull(configureMicrosoftIdentityOptions);
        }

        private Action<MicrosoftIdentityOptions> ConfigureMicrosoftIdentityOptions { get; set; }

        /// <summary>
        /// The OpenID Connect scheme name to be used.
        /// </summary>
        public string OpenIdConnectScheme { get; private set; }

        /// <summary>
        /// The web app calls a web API.
        /// </summary>
        /// <param name="initialScopes">Initial scopes.</param>
        /// <returns>The builder itself for chaining.</returns>
#if NET6_0_OR_GREATER && !NET8_0_OR_GREATER
        [RequiresUnreferencedCode("Calls Microsoft.Identity.Web.MicrosoftIdentityBaseAuthenticationBuilder.MicrosoftIdentityBaseAuthenticationBuilder(IServiceCollection, IConfigurationSection).")]
#endif
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
#if NET6_0_OR_GREATER && !NET8_0_OR_GREATER
        [RequiresUnreferencedCode("Calls Microsoft.Identity.Web.MicrosoftIdentityWebAppAuthenticationBuilder.WebAppCallsWebApiImplementation(IServiceCollection, IEnumerable<string>, Action<MicrosoftIdentityOptions>, string, Action<ConfidentialClientApplicationOptions>.")]
#endif
        public MicrosoftIdentityAppCallsWebApiAuthenticationBuilder EnableTokenAcquisitionToCallDownstreamApi(
            Action<ConfidentialClientApplicationOptions>? configureConfidentialClientApplicationOptions,
            IEnumerable<string>? initialScopes = null)
        {
            WebAppCallsWebApiImplementation(
                Services,
                initialScopes,
                null, /* to avoid calling the delegate twice */
                OpenIdConnectScheme,
                configureConfidentialClientApplicationOptions);
            return new MicrosoftIdentityAppCallsWebApiAuthenticationBuilder(
                Services,
                ConfigurationSection);
        }

#if NET6_0_OR_GREATER
        [RequiresUnreferencedCode("Calls Microsoft.Identity.Web.ClientInfo.CreateFromJson(string).")]
#endif
        internal static void WebAppCallsWebApiImplementation(
            IServiceCollection services,
            IEnumerable<string>? initialScopes,
            Action<MicrosoftIdentityOptions>? configureMicrosoftIdentityOptions,
            string openIdConnectScheme,
            Action<ConfidentialClientApplicationOptions>? configureConfidentialClientApplicationOptions)
        {
            // When called from MISE, ensure that configuration options for MSAL.NET, HttpContext accessor
            // and the Token acquisition service (encapsulating MSAL.NET) are available through dependency injection.
            // When called from AddMicrosoftIdentityWebApp(delegate), should not be re-configured otherwise
            // the delegate would be called twice.
            if (configureMicrosoftIdentityOptions != null)
            {
                // Won't be null in the case where the caller is MISE (to ensure that the configuration for MSAL.NET
                // is available through DI).
                // Will be null when called from AddMicrosoftIdentityWebApp(delegate) to avoid calling the delegate twice.
                services.Configure(openIdConnectScheme, configureMicrosoftIdentityOptions);
            }
            if (configureConfidentialClientApplicationOptions != null)
            {
                services.Configure(openIdConnectScheme, configureConfidentialClientApplicationOptions);
            }

            services.AddHttpContextAccessor();

            if (AppServicesAuthenticationInformation.IsAppServicesAadAuthenticationEnabled)
            {
                services.AddScoped<ITokenAcquisition, AppServicesAuthenticationTokenAcquisition>();
                services.AddScoped<IAuthorizationHeaderProvider, DefaultAuthorizationHeaderProvider>();
                services.AddScoped<ITokenAcquirerFactory, DefaultTokenAcquirerFactoryImplementation>();
            }
            else
            {
                services.AddTokenAcquisition();

                _ = services.AddOptions<OpenIdConnectOptions>(openIdConnectScheme)
                   .Configure<IMergedOptionsStore, IOptionsMonitor<ConfidentialClientApplicationOptions>, IOptions<ConfidentialClientApplicationOptions>>((
                       options,
                       mergedOptionsMonitor,
                       ccaOptionsMonitor,
                       ccaOptions) =>
                   {
                       MergedOptions mergedOptions = mergedOptionsMonitor.Get(openIdConnectScheme);

                       MergedOptions.UpdateMergedOptionsFromConfidentialClientApplicationOptions(ccaOptions.Value, mergedOptions); // legacy scenario w/out auth scheme
                       MergedOptions.UpdateMergedOptionsFromConfidentialClientApplicationOptions(ccaOptionsMonitor.Get(openIdConnectScheme), mergedOptions); // w/auth scheme

                       options.ResponseType = OpenIdConnectResponseType.Code;

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
                           await tokenAcquisition.AddAccountToCacheFromAuthorizationCodeAsync(context, options.Scope, openIdConnectScheme).ConfigureAwait(false);
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

                               if (clientInfoFromServer != null && clientInfoFromServer.UniqueTenantIdentifier != null && clientInfoFromServer.UniqueObjectIdentifier != null)
                               {
                                   var identity = context!.Principal!.Identities.FirstOrDefault();
                                   if (identity != null)
                                   {
                                       var uniqueTenantIdentifierClaim = identity.FindFirst(c => c.Type == ClaimConstants.UniqueTenantIdentifier);
                                       var uniqueObjectIdentifierClaim = identity.FindFirst(c => c.Type == ClaimConstants.UniqueObjectIdentifier);
                                       if (uniqueTenantIdentifierClaim != null)
                                       {
                                           throw new InternalClaimDetectedException($"The claim \"{ClaimConstants.UniqueTenantIdentifier}\" is reserved for internal use by this library. To ensure proper functionality and avoid conflicts, please remove or rename this claim in your ID Token.")
                                           {
                                               Claim = uniqueTenantIdentifierClaim
                                           };
                                       }
                                       if (uniqueObjectIdentifierClaim != null)
                                       {
                                           throw new InternalClaimDetectedException($"The claim \"{ClaimConstants.UniqueObjectIdentifier}\" is reserved for internal use by this library. To ensure proper functionality and avoid conflicts, please remove or rename this claim in your ID Token.")
                                           {
                                               Claim = uniqueObjectIdentifierClaim
                                           };
                                       }

                                       identity.AddClaim(new Claim(ClaimConstants.UniqueTenantIdentifier, clientInfoFromServer.UniqueTenantIdentifier));
                                       identity.AddClaim(new Claim(ClaimConstants.UniqueObjectIdentifier, clientInfoFromServer.UniqueObjectIdentifier));
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
                           var tokenAcquisition = context!.HttpContext.RequestServices.GetRequiredService<ITokenAcquisitionInternal>();
                           await tokenAcquisition.RemoveAccountAsync(context!.HttpContext.User, openIdConnectScheme).ConfigureAwait(false);
                           await signOutHandler(context).ConfigureAwait(false);
                       };
                   });
            }
        }
    }
}

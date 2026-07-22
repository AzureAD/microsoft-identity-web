// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Security.Authentication;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Client;
using Microsoft.Identity.Web.TokenCacheProviders;
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
        [RequiresUnreferencedCode("Calls Microsoft.Identity.Web.MicrosoftIdentityBaseAuthenticationBuilder.MicrosoftIdentityBaseAuthenticationBuilder(IServiceCollection, IConfigurationSection).")]
        [RequiresDynamicCode("Calls Microsoft.Identity.Web.MicrosoftIdentityBaseAuthenticationBuilder.MicrosoftIdentityBaseAuthenticationBuilder(IServiceCollection, IConfigurationSection).")]
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
        [RequiresUnreferencedCode("Calls Microsoft.Identity.Web.MicrosoftIdentityBaseAuthenticationBuilder.MicrosoftIdentityBaseAuthenticationBuilder(IServiceCollection, IConfigurationSection).")]
        [RequiresDynamicCode("Calls Microsoft.Identity.Web.MicrosoftIdentityBaseAuthenticationBuilder.MicrosoftIdentityBaseAuthenticationBuilder(IServiceCollection, IConfigurationSection).")]
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
        [RequiresUnreferencedCode("Calls Microsoft.Identity.Web.MicrosoftIdentityWebAppAuthenticationBuilder.WebAppCallsWebApiImplementation(IServiceCollection, IEnumerable<string>, Action<MicrosoftIdentityOptions>, string, Action<ConfidentialClientApplicationOptions>.")]
        [RequiresDynamicCode("Calls Microsoft.Identity.Web.MicrosoftIdentityWebAppAuthenticationBuilder.WebAppCallsWebApiImplementation(IServiceCollection, IEnumerable<string>, Action<MicrosoftIdentityOptions>, string, Action<ConfidentialClientApplicationOptions>.")]
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

        [RequiresUnreferencedCode("Calls Microsoft.Identity.Web.ClientInfo.CreateFromJson(string).")]
        [RequiresDynamicCode("Calls Microsoft.Identity.Web.ClientInfo.CreateFromJson(string).")]
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

                       // Prevents the automatic (EnableTokenAcquisitionToCallDownstreamApi-less) authorization code
                       // redemption from also wiring itself up and redeeming the code a second time.
                       // See EnableAutomaticAuthorizationCodeRedemptionIfNeeded.
                       mergedOptions.AuthorizationCodeHandledByMicrosoftIdentityWeb = true;

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
                                   var identity = context!.Principal!.Identities.FirstOrDefault();
                                   if (identity != null)
                                   {
                                       if (clientInfoFromServer.UniqueTenantIdentifier != null)
                                       {
                                           var uniqueTenantIdentifierClaim = identity.FindFirst(c => c.Type == ClaimConstants.UniqueTenantIdentifier);
                                           if (uniqueTenantIdentifierClaim != null && !string.Equals(clientInfoFromServer.UniqueTenantIdentifier, uniqueTenantIdentifierClaim.Value, StringComparison.OrdinalIgnoreCase))
                                           {
                                               context.Fail(new AuthenticationException(string.Format(CultureInfo.InvariantCulture, IDWebErrorMessage.InternalClaimDetected, ClaimConstants.UniqueTenantIdentifier)));
                                               return;
                                           }
                                       }

                                       if (clientInfoFromServer.UniqueObjectIdentifier != null)
                                       {
                                           var uniqueObjectIdentifierClaim = identity.FindFirst(c => c.Type == ClaimConstants.UniqueObjectIdentifier);
                                           if (uniqueObjectIdentifierClaim != null && !string.Equals(clientInfoFromServer.UniqueObjectIdentifier, uniqueObjectIdentifierClaim.Value, StringComparison.OrdinalIgnoreCase))
                                           {
                                               context.Fail(new AuthenticationException(string.Format(CultureInfo.InvariantCulture, IDWebErrorMessage.InternalClaimDetected, ClaimConstants.UniqueObjectIdentifier)));
                                               return;
                                           }
                                       }

                                       if (clientInfoFromServer.UniqueTenantIdentifier != null && clientInfoFromServer.UniqueObjectIdentifier != null)
                                       {
                                           identity.AddClaim(new Claim(ClaimConstants.UniqueTenantIdentifier, clientInfoFromServer.UniqueTenantIdentifier));
                                           identity.AddClaim(new Claim(ClaimConstants.UniqueObjectIdentifier, clientInfoFromServer.UniqueObjectIdentifier));
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
                           var tokenAcquisition = context!.HttpContext.RequestServices.GetRequiredService<ITokenAcquisitionInternal>();
                           await tokenAcquisition.RemoveAccountAsync(context!.HttpContext.User, openIdConnectScheme).ConfigureAwait(false);
                           await signOutHandler(context).ConfigureAwait(false);
                       };
                   });
            }
        }

        /// <summary>
        /// Determines whether the given OpenID Connect response type requires an authorization code to be
        /// redeemed (i.e. contains the "code" response type, as in "code" or "code id_token").
        /// </summary>
        internal static bool RequiresAuthorizationCodeRedemption(string? responseType)
        {
            if (string.IsNullOrEmpty(responseType))
            {
                return false;
            }

            foreach (string part in responseType!.Split(' '))
            {
                if (string.Equals(part, OpenIdConnectResponseType.Code, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Determines whether the client credentials contain at least one credential that is not a plain
        /// client secret (e.g. a certificate, a signed assertion from managed identity, etc.). Such credentials
        /// require the authorization code to be redeemed by MSAL.NET, which knows how to build the corresponding
        /// client_assertion; the default OpenID Connect handler does not.
        /// </summary>
        internal static bool HasComplexClientCredential(IEnumerable<CredentialDescription>? clientCredentials)
        {
            return clientCredentials != null && clientCredentials.Any(c => c.SourceType != CredentialSource.ClientSecret);
        }

        /// <summary>
        /// Best-effort, synchronous equivalent of <see cref="HasComplexClientCredential(IEnumerable{CredentialDescription})"/>,
        /// used to peek at a <see cref="MicrosoftIdentityOptions"/> before it goes through the full options merge
        /// pipeline (which is only available lazily, at runtime). Also accounts for the legacy
        /// ClientCredentialsUsingManagedIdentity and ClientCertificates properties.
        /// </summary>
        internal static bool HasComplexClientCredential(MicrosoftIdentityOptions options)
        {
            if (HasComplexClientCredential(options.ClientCredentials))
            {
                return true;
            }

            if (options.ClientCredentialsUsingManagedIdentity != null && options.ClientCredentialsUsingManagedIdentity.IsEnabled)
            {
                return true;
            }

            return options.ClientCertificates != null && options.ClientCertificates.Any();
        }

        /// <summary>
        /// When a web app is configured with a complex client credential (not a plain client secret) together
        /// with ResponseType=code, the default ASP.NET Core OpenID Connect handler cannot redeem the
        /// authorization code itself (it does not know how to build the client_assertion Azure AD requires),
        /// which fails with AADSTS7000218. Microsoft.Identity.Web normally only redeems the code when
        /// EnableTokenAcquisitionToCallDownstreamApi() is called; this makes the token acquisition services
        /// available up front - before the caller has a chance to call EnableTokenAcquisitionToCallDownstreamApi() -
        /// so that automatic redemption can be wired-up later, once the merged options are available
        /// (see <see cref="EnableAutomaticAuthorizationCodeRedemptionIfNeeded"/>).
        /// This is a best-effort, synchronous peek at the options: it only sees what
        /// <paramref name="configureMicrosoftIdentityOptions"/> sets. When it misses a scenario that does need
        /// automatic redemption (for example because the complex credential is only visible after the full
        /// options merge), a warning is logged at runtime instead of silently failing.
        /// </summary>
        internal static void EnsureTokenAcquisitionServicesForComplexCredentials(
            IServiceCollection services,
            Action<MicrosoftIdentityOptions> configureMicrosoftIdentityOptions)
        {
            MicrosoftIdentityOptions probe = new MicrosoftIdentityOptions();
            try
            {
                configureMicrosoftIdentityOptions(probe);
            }
            catch
            {
                // Best-effort only: some delegates may not be safe to invoke outside of the regular Options
                // pipeline (e.g. if they depend on services that are not yet registered). Automatic detection
                // is simply skipped in that case; a warning is logged at runtime if it turns out to have been needed.
                return;
            }

            if (RequiresAuthorizationCodeRedemption(probe.ResponseType) && HasComplexClientCredential(probe))
            {
                services.AddTokenAcquisition();
                services.TryAddSingleton<IMsalTokenCacheProvider, NoopMsalTokenCacheProvider>();
            }
        }

        /// <summary>
        /// Wires-up MSAL-based authorization code redemption automatically when a complex client credential
        /// (not a plain client secret) is configured together with ResponseType=code, and
        /// EnableTokenAcquisitionToCallDownstreamApi() was not called. Runs as a PostConfigure step, guaranteed
        /// to execute after EnableTokenAcquisitionToCallDownstreamApi()'s own Configure step (if any), so that
        /// <see cref="MergedOptions.AuthorizationCodeHandledByMicrosoftIdentityWeb"/> can be used to avoid
        /// redeeming the code a second time.
        /// </summary>
        [RequiresUnreferencedCode("Calls Microsoft.Identity.Web.ITokenAcquisitionInternal.AddAccountToCacheFromAuthorizationCodeAsync(AuthorizationCodeReceivedContext, IEnumerable<string>, string).")]
        [RequiresDynamicCode("Calls Microsoft.Identity.Web.ITokenAcquisitionInternal.AddAccountToCacheFromAuthorizationCodeAsync(AuthorizationCodeReceivedContext, IEnumerable<string>, string).")]
        internal static void EnableAutomaticAuthorizationCodeRedemptionIfNeeded(
            OpenIdConnectOptions options,
            MergedOptions mergedOptions,
            string openIdConnectScheme,
            ILogger<MicrosoftIdentityWebAppAuthenticationBuilder> logger)
        {
            if (mergedOptions.AuthorizationCodeHandledByMicrosoftIdentityWeb)
            {
                // Either EnableTokenAcquisitionToCallDownstreamApi() was called explicitly, or automatic
                // redemption was already wired-up: nothing more to do.
                return;
            }

            // options.ResponseType is the authoritative, fully-merged value at PostConfigure time: it reflects
            // both the value that flowed from MicrosoftIdentityOptions and any override applied directly on the
            // OpenIdConnectOptions (e.g. via services.Configure<OpenIdConnectOptions>). mergedOptions.ResponseType
            // only carries the former, so checking it here would miss a ResponseType=code set on the OIDC options.
            if (!RequiresAuthorizationCodeRedemption(options.ResponseType) ||
                !HasComplexClientCredential(mergedOptions.ClientCredentials))
            {
                return;
            }

            mergedOptions.AuthorizationCodeHandledByMicrosoftIdentityWeb = true;
            WebAppAuthorizationCodeLogging.AutomaticRedemptionEnabled(logger, openIdConnectScheme);

            // This scope is needed to get a refresh token when users sign-in with their Microsoft personal accounts.
            if (!options.Scope.Contains(OidcConstants.ScopeOfflineAccess))
            {
                options.Scope.Add(OidcConstants.ScopeOfflineAccess);
            }

            var codeReceivedHandler = options.Events.OnAuthorizationCodeReceived;
            options.Events.OnAuthorizationCodeReceived = async context =>
            {
                // Let any application-supplied handler run first. Unlike the explicit
                // EnableTokenAcquisitionToCallDownstreamApi() path (which the developer opts into), this handler
                // is wired automatically, so it must not assume it is the only party redeeming the code. Apps
                // that predate this feature commonly worked around the issue by redeeming the code themselves in
                // their own OnAuthorizationCodeReceived handler; redeeming again here would replay the single-use
                // code and Azure AD would reject it. So only redeem with MSAL if the code is still unredeemed.
                await codeReceivedHandler(context).ConfigureAwait(false);

                if (context!.HandledCodeRedemption)
                {
                    return;
                }

                var tokenAcquisition = context.HttpContext.RequestServices.GetService<ITokenAcquisitionInternal>();
                if (tokenAcquisition != null)
                {
                    // AddAccountToCacheFromAuthorizationCodeAsync calls context.HandleCodeRedemption, so the
                    // default OpenID Connect handler will not attempt to redeem the code itself afterwards.
                    await tokenAcquisition.AddAccountToCacheFromAuthorizationCodeAsync(context, options.Scope, openIdConnectScheme).ConfigureAwait(false);
                }
                else
                {
                    // EnsureTokenAcquisitionServicesForComplexCredentials's synchronous peek did not detect
                    // this scenario. The default OpenID Connect handler will now attempt to redeem the code
                    // itself and fail with AADSTS7000218 because it is unaware of the complex credential.
                    WebAppAuthorizationCodeLogging.AutomaticRedemptionNotAvailable(logger, openIdConnectScheme);
                }
            };

            var signOutHandler = options.Events.OnRedirectToIdentityProviderForSignOut;
            options.Events.OnRedirectToIdentityProviderForSignOut = async context =>
            {
                var tokenAcquisition = context!.HttpContext.RequestServices.GetService<ITokenAcquisitionInternal>();
                if (tokenAcquisition != null)
                {
                    await tokenAcquisition.RemoveAccountAsync(context!.HttpContext.User, openIdConnectScheme).ConfigureAwait(false);
                }

                await signOutHandler(context).ConfigureAwait(false);
            };
        }
    }
}

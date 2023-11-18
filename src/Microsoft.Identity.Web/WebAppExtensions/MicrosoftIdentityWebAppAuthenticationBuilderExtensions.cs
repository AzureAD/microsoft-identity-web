// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OAuth.Claims;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Web.Resource;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using System.Linq;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// Extensions for the <see cref="AuthenticationBuilder"/> for startup initialization.
    /// </summary>
    public static class MicrosoftIdentityWebAppAuthenticationBuilderExtensions
    {
        /// <summary>
        /// Add authentication to a web app with Microsoft identity platform.
        /// This method expects the configuration file will have a section, named "AzureAd" as default,
        /// with the necessary settings to initialize authentication options.
        /// </summary>
        /// <param name="builder">The <see cref="AuthenticationBuilder"/> to which to add this configuration.</param>
        /// <param name="configuration">The configuration instance.</param>
        /// <param name="configSectionName">The configuration section with the necessary settings to initialize authentication options.</param>
        /// <param name="openIdConnectScheme">The OpenID Connect scheme name to be used. By default it uses "OpenIdConnect".</param>
        /// <param name="cookieScheme">The cookie-based scheme name to be used. By default it uses "Cookies".</param>
        /// <param name="subscribeToOpenIdConnectMiddlewareDiagnosticsEvents">Set to true if you want to debug, or just understand the OpenID Connect events.</param>
        /// <param name="displayName">A display name for the authentication handler.</param>
        /// <returns>The <see cref="MicrosoftIdentityWebAppAuthenticationBuilderWithConfiguration"/> builder for chaining.</returns>
#if NET6_0_OR_GREATER
        [RequiresUnreferencedCode("Calls a trim-incompatible AddMicrosoftIdentityWebApp.")]
#endif
        public static MicrosoftIdentityWebAppAuthenticationBuilderWithConfiguration AddMicrosoftIdentityWebApp(
            this AuthenticationBuilder builder,
            IConfiguration configuration,
            string configSectionName = Constants.AzureAd,
            string openIdConnectScheme = OpenIdConnectDefaults.AuthenticationScheme,
            string? cookieScheme = CookieAuthenticationDefaults.AuthenticationScheme,
            bool subscribeToOpenIdConnectMiddlewareDiagnosticsEvents = false,
            string? displayName = null)
        {
            if (configuration == null)
            {
                throw new ArgumentException(nameof(configuration));
            }

            if (string.IsNullOrEmpty(configSectionName))
            {
                throw new ArgumentException(nameof(configSectionName));
            }

            IConfigurationSection configurationSection = configuration.GetSection(configSectionName);

            return builder.AddMicrosoftIdentityWebApp(
                configurationSection,
                openIdConnectScheme,
                cookieScheme,
                subscribeToOpenIdConnectMiddlewareDiagnosticsEvents,
                displayName);
        }

        /// <summary>
        /// Add authentication with Microsoft identity platform.
        /// This method expects the configuration file will have a section, named "AzureAd" as default, with the necessary settings to initialize authentication options.
        /// </summary>
        /// <param name="builder">The <see cref="AuthenticationBuilder"/> to which to add this configuration.</param>
        /// <param name="configurationSection">The configuration section from which to get the options.</param>
        /// <param name="openIdConnectScheme">The OpenID Connect scheme name to be used. By default it uses "OpenIdConnect".</param>
        /// <param name="cookieScheme">The cookie-based scheme name to be used. By default it uses "Cookies".</param>
        /// <param name="subscribeToOpenIdConnectMiddlewareDiagnosticsEvents">Set to true if you want to debug, or just understand the OpenID Connect events.</param>
        /// <param name="displayName">A display name for the authentication handler.</param>
        /// <returns>The authentication builder for chaining.</returns>
#if NET6_0_OR_GREATER && !NET8_0_OR_GREATER
        [RequiresUnreferencedCode("Calls Microsoft.Extensions.Configuration.ConfigurationBinder.Bind(IConfiguration, Object).")]
#endif
        public static MicrosoftIdentityWebAppAuthenticationBuilderWithConfiguration AddMicrosoftIdentityWebApp(
            this AuthenticationBuilder builder,
            IConfigurationSection configurationSection,
            string openIdConnectScheme = OpenIdConnectDefaults.AuthenticationScheme,
            string? cookieScheme = CookieAuthenticationDefaults.AuthenticationScheme,
            bool subscribeToOpenIdConnectMiddlewareDiagnosticsEvents = false,
            string? displayName = null)
        {
            _ = Throws.IfNull(builder);
            _ = Throws.IfNull(configurationSection);

            return builder.AddMicrosoftIdentityWebAppWithConfiguration(
                options => configurationSection.Bind(options),
                null,
                openIdConnectScheme,
                cookieScheme,
                subscribeToOpenIdConnectMiddlewareDiagnosticsEvents,
                displayName,
                configurationSection);
        }

        /// <summary>
        /// Add authentication with Microsoft identity platform.
        /// </summary>
        /// <param name="builder">The <see cref="AuthenticationBuilder"/> to which to add this configuration.</param>
        /// <param name="configureMicrosoftIdentityOptions">The action to configure <see cref="MicrosoftIdentityOptions"/>.</param>
        /// <param name="configureCookieAuthenticationOptions">The action to configure <see cref="CookieAuthenticationOptions"/>.</param>
        /// <param name="openIdConnectScheme">The OpenID Connect scheme name to be used. By default it uses "OpenIdConnect".</param>
        /// <param name="cookieScheme">The cookie-based scheme name to be used. By default it uses "Cookies".</param>
        /// <param name="subscribeToOpenIdConnectMiddlewareDiagnosticsEvents">Set to true if you want to debug, or just understand the OpenID Connect events.</param>
        /// <param name="displayName">A display name for the authentication handler.</param>
        /// <returns>The authentication builder for chaining.</returns>
#if NET6_0_OR_GREATER && !NET8_0_OR_GREATER
        [RequiresUnreferencedCode("Microsoft.Identity.Web.MicrosoftIdentityWebAppAuthenticationBuilderExtensions.AddMicrosoftWebAppWithoutConfiguration(AuthenticationBuilder, Action<MicrosoftIdentityOptions>, Action<CookieAuthenticationOptions>, String, String, Boolean, String).")]
#endif
        public static MicrosoftIdentityWebAppAuthenticationBuilder AddMicrosoftIdentityWebApp(
            this AuthenticationBuilder builder,
            Action<MicrosoftIdentityOptions> configureMicrosoftIdentityOptions,
            Action<CookieAuthenticationOptions>? configureCookieAuthenticationOptions = null,
            string openIdConnectScheme = OpenIdConnectDefaults.AuthenticationScheme,
            string? cookieScheme = CookieAuthenticationDefaults.AuthenticationScheme,
            bool subscribeToOpenIdConnectMiddlewareDiagnosticsEvents = false,
            string? displayName = null)
        {
            _ = Throws.IfNull(builder);

            return builder.AddMicrosoftWebAppWithoutConfiguration(
                configureMicrosoftIdentityOptions,
                configureCookieAuthenticationOptions,
                openIdConnectScheme,
                cookieScheme,
                subscribeToOpenIdConnectMiddlewareDiagnosticsEvents,
                displayName);
        }

        /// <summary>
        /// Add authentication with Microsoft identity platform.
        /// </summary>
        /// <param name="builder">The <see cref="AuthenticationBuilder"/> to which to add this configuration.</param>
        /// <param name="configureMicrosoftIdentityOptions">The action to configure <see cref="MicrosoftIdentityOptions"/>.</param>
        /// <param name="configureCookieAuthenticationOptions">The action to configure <see cref="CookieAuthenticationOptions"/>.</param>
        /// <param name="openIdConnectScheme">The OpenID Connect scheme name to be used. By default it uses "OpenIdConnect".</param>
        /// <param name="cookieScheme">The cookie-based scheme name to be used. By default it uses "Cookies".</param>
        /// <param name="subscribeToOpenIdConnectMiddlewareDiagnosticsEvents">Set to true if you want to debug, or just understand the OpenID Connect events.</param>
        /// <param name="displayName">A display name for the authentication handler.</param>
        /// <param name="configurationSection">Configuration section.</param>
        /// <returns>The authentication builder for chaining.</returns>
#if NET6_0_OR_GREATER && !NET8_0_OR_GREATER
        [RequiresUnreferencedCode("Calls Microsoft.Identity.Web.MicrosoftIdentityWebAppAuthenticationBuilderWithConfiguration.MicrosoftIdentityWebAppAuthenticationBuilderWithConfiguration(IServiceCollection, String, Action<MicrosoftIdentityOptions>, IConfigurationSection)")]
#endif
        private static MicrosoftIdentityWebAppAuthenticationBuilderWithConfiguration AddMicrosoftIdentityWebAppWithConfiguration(
                this AuthenticationBuilder builder,
                Action<MicrosoftIdentityOptions> configureMicrosoftIdentityOptions,
                Action<CookieAuthenticationOptions>? configureCookieAuthenticationOptions,
                string openIdConnectScheme,
                string? cookieScheme,
                bool subscribeToOpenIdConnectMiddlewareDiagnosticsEvents,
                string? displayName,
                IConfigurationSection configurationSection)
        {
            AddMicrosoftIdentityWebAppInternal(
                builder,
                configureMicrosoftIdentityOptions,
                configureCookieAuthenticationOptions,
                openIdConnectScheme,
                cookieScheme,
                subscribeToOpenIdConnectMiddlewareDiagnosticsEvents,
                displayName);

            return new MicrosoftIdentityWebAppAuthenticationBuilderWithConfiguration(
                builder.Services,
                openIdConnectScheme,
                configureMicrosoftIdentityOptions,
                configurationSection);
        }

        /// <summary>
        /// Add authentication with Microsoft identity platform.
        /// </summary>
        /// <param name="builder">The <see cref="AuthenticationBuilder"/> to which to add this configuration.</param>
        /// <param name="configureMicrosoftIdentityOptions">The action to configure <see cref="MicrosoftIdentityOptions"/>.</param>
        /// <param name="configureCookieAuthenticationOptions">The action to configure <see cref="CookieAuthenticationOptions"/>.</param>
        /// <param name="openIdConnectScheme">The OpenID Connect scheme name to be used. By default it uses "OpenIdConnect".</param>
        /// <param name="cookieScheme">The cookie-based scheme name to be used. By default it uses "Cookies".</param>
        /// <param name="subscribeToOpenIdConnectMiddlewareDiagnosticsEvents">Set to true if you want to debug, or just understand the OpenID Connect events.</param>
        /// <param name="displayName">A display name for the authentication handler.</param>
        /// <returns>The authentication builder for chaining.</returns>
#if NET6_0_OR_GREATER && !NET8_0_OR_GREATER
        [RequiresUnreferencedCode("Calls Microsoft.Identity.Web.MicrosoftIdentityWebAppAuthenticationBuilder.MicrosoftIdentityWebAppAuthenticationBuilder(IServiceCollection, String, Action<MicrosoftIdentityOptions>, IConfigurationSection)")]
#endif
        private static MicrosoftIdentityWebAppAuthenticationBuilder AddMicrosoftWebAppWithoutConfiguration(
        this AuthenticationBuilder builder,
        Action<MicrosoftIdentityOptions> configureMicrosoftIdentityOptions,
        Action<CookieAuthenticationOptions>? configureCookieAuthenticationOptions,
        string openIdConnectScheme,
        string? cookieScheme,
        bool subscribeToOpenIdConnectMiddlewareDiagnosticsEvents,
        string? displayName)
        {
            if (!AppServicesAuthenticationInformation.IsAppServicesAadAuthenticationEnabled)
            {
                AddMicrosoftIdentityWebAppInternal(
                builder,
                configureMicrosoftIdentityOptions,
                configureCookieAuthenticationOptions,
                openIdConnectScheme,
                cookieScheme,
                subscribeToOpenIdConnectMiddlewareDiagnosticsEvents,
                displayName);
            }
            else
            {
                builder.Services.AddAuthentication(AppServicesAuthenticationDefaults.AuthenticationScheme)
                  .AddAppServicesAuthentication();
            }

            return new MicrosoftIdentityWebAppAuthenticationBuilder(
                builder.Services,
                openIdConnectScheme,
                configureMicrosoftIdentityOptions,
                null);
        }

        private static void AddMicrosoftIdentityWebAppInternal(
            AuthenticationBuilder builder,
            Action<MicrosoftIdentityOptions> configureMicrosoftIdentityOptions,
            Action<CookieAuthenticationOptions>? configureCookieAuthenticationOptions,
            string openIdConnectScheme,
            string? cookieScheme,
            bool subscribeToOpenIdConnectMiddlewareDiagnosticsEvents,
            string? displayName)
        {
            _ = Throws.IfNull(builder);
            _ = Throws.IfNull(configureMicrosoftIdentityOptions);

            if (builder.Services.FirstOrDefault(s => s.ImplementationType == typeof(MicrosoftIdentityOptionsMerger)) == null)
            {
                builder.Services.AddSingleton<IPostConfigureOptions<MicrosoftIdentityOptions>, MicrosoftIdentityOptionsMerger>();
            }

            builder.Services.Configure(openIdConnectScheme, configureMicrosoftIdentityOptions);
            builder.Services.AddSingleton<IMergedOptionsStore, MergedOptionsStore>();
            builder.Services.AddHttpClient();

            if (!string.IsNullOrEmpty(cookieScheme))
            {
                Action<CookieAuthenticationOptions> emptyOption = option => { };
                builder.AddCookie(cookieScheme, configureCookieAuthenticationOptions ?? emptyOption);
            }

            builder.Services.TryAddSingleton<MicrosoftIdentityIssuerValidatorFactory>();
            builder.Services.TryAddSingleton<ILoginErrorAccessor>(ctx =>
            {
                // ITempDataDictionaryFactory is not always available, so we don't require it
                var tempFactory = ctx.GetService<ITempDataDictionaryFactory>();
                var env = ctx.GetService<IHostEnvironment>(); // ex. Azure Functions will not have an env.

                if (env != null)
                {
                    return TempDataLoginErrorAccessor.Create(tempFactory, env.IsDevelopment());
                }
                else
                {
                    return TempDataLoginErrorAccessor.Create(tempFactory, false);
                }
            });

            if (subscribeToOpenIdConnectMiddlewareDiagnosticsEvents)
            {
                builder.Services.AddSingleton<IOpenIdConnectMiddlewareDiagnostics, OpenIdConnectMiddlewareDiagnostics>();
            }

            if (AppServicesAuthenticationInformation.IsAppServicesAadAuthenticationEnabled)
            {
                builder.Services.AddAuthentication(AppServicesAuthenticationDefaults.AuthenticationScheme)
                    .AddAppServicesAuthentication();
                return;
            }

            if (!string.IsNullOrEmpty(displayName))
            {
                builder.AddOpenIdConnect(openIdConnectScheme, displayName: displayName, options => { });
            }
            else
            {
                builder.AddOpenIdConnect(openIdConnectScheme, options => { });
            }

            builder.Services.AddOptions<OpenIdConnectOptions>(openIdConnectScheme)
                .Configure<IServiceProvider, IMergedOptionsStore, IOptionsMonitor<MicrosoftIdentityOptions>, IOptions<MicrosoftIdentityOptions>>((
                options,
                serviceProvider,
                mergedOptionsMonitor,
                msIdOptionsMonitor,
                msIdOptions) =>
                {
                    MicrosoftIdentityBaseAuthenticationBuilder.SetIdentityModelLogger(serviceProvider);

                    msIdOptionsMonitor.Get(openIdConnectScheme); // needed for firing the PostConfigure.
                    MergedOptions mergedOptions = mergedOptionsMonitor.Get(openIdConnectScheme);

                    MergedOptionsValidation.Validate(mergedOptions);

                    if (mergedOptions.Authority != null)
                    {
                        mergedOptions.Authority = AuthorityHelpers.BuildCiamAuthorityIfNeeded(mergedOptions.Authority);
                        if (mergedOptions.ExtraQueryParameters != null)
                        {
                            options.MetadataAddress = mergedOptions.Authority + "/.well-known/openid-configuration?" + string.Join("&", mergedOptions.ExtraQueryParameters.Select(p => $"{p.Key}={p.Value}"));
                        }
                    }

                    PopulateOpenIdOptionsFromMergedOptions(options, mergedOptions);

                    var b2cOidcHandlers = new AzureADB2COpenIDConnectEventHandlers(
                        openIdConnectScheme,
                        mergedOptions,
                        serviceProvider.GetRequiredService<ILoginErrorAccessor>());

                    if (!string.IsNullOrEmpty(cookieScheme))
                    {
                        options.SignInScheme = cookieScheme;
                    }

                    if (string.IsNullOrWhiteSpace(options.Authority))
                    {
                        options.Authority = AuthorityHelpers.BuildAuthority(mergedOptions);
                    }

                    // This is a Microsoft identity platform web app
                    options.Authority = AuthorityHelpers.EnsureAuthorityIsV2(options.Authority);

                    // B2C doesn't have preferred_username claims
                    if (mergedOptions.IsB2C)
                    {
                        options.TokenValidationParameters.NameClaimType = ClaimConstants.Name;
                    }
                    else
                    {
                        options.TokenValidationParameters.NameClaimType = ClaimConstants.PreferredUserName;
                    }

                    // If the developer registered an IssuerValidator, do not overwrite it
                    if (options.TokenValidationParameters.ValidateIssuer && options.TokenValidationParameters.IssuerValidator == null)
                    {
                        // If you want to restrict the users that can sign-in to several organizations
                        // Set the tenant value in the appsettings.json file to 'organizations', and add the
                        // issuers you want to accept to options.TokenValidationParameters.ValidIssuers collection
                        MicrosoftIdentityIssuerValidatorFactory microsoftIdentityIssuerValidatorFactory =
                        serviceProvider.GetRequiredService<MicrosoftIdentityIssuerValidatorFactory>();

                        options.TokenValidationParameters.IssuerValidator =
                        microsoftIdentityIssuerValidatorFactory.GetAadIssuerValidator(options.Authority).Validate;
                    }

                    // Avoids having users being presented the select account dialog when they are already signed-in
                    // for instance when going through incremental consent
                    var redirectToIdpHandler = options.Events.OnRedirectToIdentityProvider;
                    options.Events.OnRedirectToIdentityProvider = async context =>
                    {
                        var loginHint = context.Properties.GetParameter<string>(OpenIdConnectParameterNames.LoginHint);
                        if (!string.IsNullOrWhiteSpace(loginHint))
                        {
                            context.ProtocolMessage.LoginHint = loginHint;

                            context.ProtocolMessage.SetParameter(Constants.XAnchorMailbox, $"{Constants.Upn}:{loginHint}");
                            // delete the login_hint from the Properties when we are done otherwise
                            // it will take up extra space in the cookie.
                            context.Properties.Parameters.Remove(OpenIdConnectParameterNames.LoginHint);
                        }

                        var domainHint = context.Properties.GetParameter<string>(OpenIdConnectParameterNames.DomainHint);
                        if (!string.IsNullOrWhiteSpace(domainHint))
                        {
                            context.ProtocolMessage.DomainHint = domainHint;

                            // delete the domain_hint from the Properties when we are done otherwise
                            // it will take up extra space in the cookie.
                            context.Properties.Parameters.Remove(OpenIdConnectParameterNames.DomainHint);
                        }

                        context.ProtocolMessage.SetParameter(Constants.ClientInfo, Constants.One);
                        context.ProtocolMessage.SetParameter(Constants.TelemetryHeaderKey, IdHelper.CreateTelemetryInfo());

                        // Additional claims
                        if (context.Properties.Items.TryGetValue(OidcConstants.AdditionalClaims, out var additionClaims))
                        {
                            context.ProtocolMessage.SetParameter(
                                OidcConstants.AdditionalClaims,
                                additionClaims);
                        }

                        if (mergedOptions.ExtraQueryParameters != null)
                        {
                            foreach (var ExtraQP in mergedOptions.ExtraQueryParameters)
                            {
                                context.ProtocolMessage.SetParameter(ExtraQP.Key, ExtraQP.Value);
                            }
                        }

                        if (mergedOptions.IsB2C)
                        {
                            // When a new Challenge is returned using any B2C user flow different than susi, we must change
                            // the ProtocolMessage.IssuerAddress to the desired user flow otherwise the redirect would use the susi user flow
                            await b2cOidcHandlers.OnRedirectToIdentityProvider(context).ConfigureAwait(false);
                        }

                        await redirectToIdpHandler(context).ConfigureAwait(false);
                    };

                    if (mergedOptions.IsB2C)
                    {
                        var remoteFailureHandler = options.Events.OnRemoteFailure;
                        options.Events.OnRemoteFailure = async context =>
                        {
                            // Handles the error when a user cancels an action on the Azure Active Directory B2C UI.
                            // Handle the error code that Azure Active Directory B2C throws when trying to reset a password from the login page
                            // because password reset is not supported by a "sign-up or sign-in user flow".
                            await b2cOidcHandlers.OnRemoteFailure(context).ConfigureAwait(false);

                            await remoteFailureHandler(context).ConfigureAwait(false);
                        };
                    }

                    if (subscribeToOpenIdConnectMiddlewareDiagnosticsEvents)
                    {
                        var diagnostics = serviceProvider.GetRequiredService<IOpenIdConnectMiddlewareDiagnostics>();

                        diagnostics.Subscribe(options.Events);
                    }
                });
        }

        internal static void PopulateOpenIdOptionsFromMergedOptions(
            OpenIdConnectOptions options,
            MergedOptions mergedOptions)
        {
            options.Authority = mergedOptions.Authority;
            options.ClientId = mergedOptions.ClientId;
            options.ClientSecret = mergedOptions.ClientSecret ?? mergedOptions.ClientCredentials?.FirstOrDefault(c => c.CredentialType == Abstractions.CredentialType.Secret)?.ClientSecret;
            options.Configuration = mergedOptions.Configuration;
            options.ConfigurationManager = mergedOptions.ConfigurationManager;
            options.GetClaimsFromUserInfoEndpoint = mergedOptions.GetClaimsFromUserInfoEndpoint;

            if (options.ClaimActions != mergedOptions.ClaimActions)
            {
                var claimActionArray = options.ClaimActions.ToArray();
                foreach (ClaimAction claimAction in mergedOptions.ClaimActions)
                {
                    if (!claimActionArray.Any((c => c.ClaimType == claimAction.ClaimType && c.ValueType == claimAction.ValueType)))
                    {
                        options.ClaimActions.Add(claimAction);
                    }
                }
            }

            options.RequireHttpsMetadata = mergedOptions.RequireHttpsMetadata;
            options.MetadataAddress = mergedOptions.MetadataAddress;
            options.MaxAge = mergedOptions.MaxAge;
            options.ProtocolValidator = mergedOptions.ProtocolValidator;
            options.SignedOutCallbackPath = mergedOptions.SignedOutCallbackPath;
            options.SignedOutRedirectUri = mergedOptions.SignedOutRedirectUri;
            options.RefreshOnIssuerKeyNotFound = mergedOptions.RefreshOnIssuerKeyNotFound;
            options.AuthenticationMethod = mergedOptions.AuthenticationMethod;
            options.Resource = mergedOptions.Resource;
            options.ResponseMode = mergedOptions.ResponseMode;
            options.ResponseType = mergedOptions.ResponseType;
            options.Prompt = mergedOptions.Prompt;

            if (options.Scope != mergedOptions.Scope)
            {
                var scopeArray = options.Scope.ToArray();
                foreach (string scope in mergedOptions.Scope)
                {
                    if (!string.IsNullOrWhiteSpace(scope) && !scopeArray.Any(s => string.Equals(s, scope, StringComparison.OrdinalIgnoreCase)))
                    {
                        options.Scope.Add(scope);
                    }
                }
            }

            options.RemoteSignOutPath = mergedOptions.RemoteSignOutPath;
            options.SignOutScheme = mergedOptions.SignOutScheme;
            options.StateDataFormat = mergedOptions.StateDataFormat;
            options.StringDataFormat = mergedOptions.StringDataFormat;
#if NET8_0_OR_GREATER
            options.TokenHandler = mergedOptions.TokenHandler;
#else
            options.SecurityTokenValidator = mergedOptions.SecurityTokenValidator;
#endif
            options.TokenValidationParameters = mergedOptions.TokenValidationParameters;
            options.UseTokenLifetime = mergedOptions.UseTokenLifetime;
            options.SkipUnrecognizedRequests = mergedOptions.SkipUnrecognizedRequests;
            options.DisableTelemetry = mergedOptions.DisableTelemetry;
            options.NonceCookie = mergedOptions.NonceCookie;
            options.UsePkce = mergedOptions.UsePkce;
#if NET5_0_OR_GREATER
            options.AutomaticRefreshInterval = mergedOptions.AutomaticRefreshInterval;
            options.RefreshInterval = mergedOptions.RefreshInterval;
            options.MapInboundClaims = mergedOptions.MapInboundClaims;
#endif
            options.BackchannelTimeout = mergedOptions.BackchannelTimeout;
            options.BackchannelHttpHandler = mergedOptions.BackchannelHttpHandler;
            options.Backchannel = mergedOptions.Backchannel;
            options.DataProtectionProvider = mergedOptions.DataProtectionProvider;
            options.CallbackPath = mergedOptions.CallbackPath;
            options.AccessDeniedPath = mergedOptions.AccessDeniedPath;
            options.ReturnUrlParameter = mergedOptions.ReturnUrlParameter;
            options.SignInScheme = mergedOptions.SignInScheme;
            options.RemoteAuthenticationTimeout = mergedOptions.RemoteAuthenticationTimeout;
            options.SaveTokens = mergedOptions.SaveTokens;
            options.CorrelationCookie = mergedOptions.CorrelationCookie;
            options.ClaimsIssuer = mergedOptions.ClaimsIssuer;
            options.Events = mergedOptions.Events;
            options.EventsType = mergedOptions.EventsType;
            options.ForwardDefault = mergedOptions.ForwardDefault;
            options.ForwardAuthenticate = mergedOptions.ForwardAuthenticate;
            options.ForwardChallenge = mergedOptions.ForwardChallenge;
            options.ForwardForbid = mergedOptions.ForwardForbid;
            options.ForwardSignIn = mergedOptions.ForwardSignIn;
            options.ForwardSignOut = mergedOptions.ForwardSignOut;
            options.ForwardDefaultSelector = mergedOptions.ForwardDefaultSelector;
#if NET8_0_OR_GREATER
            options.TimeProvider = mergedOptions.TimeProvider;
            options.UseSecurityTokenValidator = mergedOptions.UseSecurityTokenValidator;
            options.TokenHandler = mergedOptions.TokenHandler;
#endif
        }
    }
}

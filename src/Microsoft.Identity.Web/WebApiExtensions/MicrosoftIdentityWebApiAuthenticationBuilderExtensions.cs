// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.ComponentModel;
using System.Linq;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Web.Resource;
using Microsoft.IdentityModel.Tokens;

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// Extensions for <see cref="AuthenticationBuilder"/> for startup initialization of web APIs.
    /// </summary>
    public static partial class MicrosoftIdentityWebApiAuthenticationBuilderExtensions
    {
        /// <summary>
        /// Protects the web API with Microsoft identity platform (formerly Azure AD v2.0).
        /// This method expects the configuration file will have a section, named "AzureAd" as default, with the necessary settings to initialize authentication options.
        /// </summary>
        /// <param name="builder">The <see cref="AuthenticationBuilder"/> to which to add this configuration.</param>
        /// <param name="configuration">The configuration instance.</param>
        /// <param name="configSectionName">The configuration section with the necessary settings to initialize authentication options.</param>
        /// <param name="jwtBearerScheme">The JWT bearer scheme name to be used. By default it uses "Bearer".</param>
        /// <param name="subscribeToJwtBearerMiddlewareDiagnosticsEvents">
        /// Set to true if you want to debug, or just understand the JWT bearer events.
        /// </param>
        /// <returns>The authentication builder to chain.</returns>
        public static MicrosoftIdentityWebApiAuthenticationBuilderWithConfiguration AddMicrosoftIdentityWebApi(
        this AuthenticationBuilder builder,
        IConfiguration configuration,
        string configSectionName = IDWebConstants.AzureAd,
        string jwtBearerScheme = JwtBearerDefaults.AuthenticationScheme,
        bool subscribeToJwtBearerMiddlewareDiagnosticsEvents = false)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            if (configSectionName == null)
            {
                throw new ArgumentNullException(nameof(configSectionName));
            }

            IConfigurationSection configurationSection = configuration.GetSection(configSectionName);

            return builder.AddMicrosoftIdentityWebApi(
                configurationSection,
                jwtBearerScheme,
                subscribeToJwtBearerMiddlewareDiagnosticsEvents);
        }

        /// <summary>
        /// Protects the web API with Microsoft identity platform (formerly Azure AD v2.0).
        /// This method expects the configuration file will have a section, named "AzureAd" as default, with the necessary settings to initialize authentication options.
        /// </summary>
        /// <param name="builder">The <see cref="AuthenticationBuilder"/> to which to add this configuration.</param>
        /// <param name="configurationSection">The configuration second from which to fill-in the options.</param>
        /// <param name="jwtBearerScheme">The JWT bearer scheme name to be used. By default it uses "Bearer".</param>
        /// <param name="subscribeToJwtBearerMiddlewareDiagnosticsEvents">
        /// Set to true if you want to debug, or just understand the JWT bearer events.
        /// </param>
        /// <returns>The authentication builder to chain.</returns>
        public static MicrosoftIdentityWebApiAuthenticationBuilderWithConfiguration AddMicrosoftIdentityWebApi(
            this AuthenticationBuilder builder,
            IConfigurationSection configurationSection,
            string jwtBearerScheme = JwtBearerDefaults.AuthenticationScheme,
            bool subscribeToJwtBearerMiddlewareDiagnosticsEvents = false)
        {
            if (configurationSection == null)
            {
                throw new ArgumentNullException(nameof(configurationSection));
            }

            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            AddMicrosoftIdentityWebApiImplementation(
                builder,
                options => configurationSection.Bind(options),
                options => configurationSection.Bind(options),
                jwtBearerScheme,
                subscribeToJwtBearerMiddlewareDiagnosticsEvents);

            return new MicrosoftIdentityWebApiAuthenticationBuilderWithConfiguration(
                builder.Services,
                jwtBearerScheme,
                options => configurationSection.Bind(options),
                options => configurationSection.Bind(options),
                configurationSection);
        }

        /// <summary>
        /// Protects the web API with Microsoft identity platform (formerly Azure AD v2.0).
        /// </summary>
        /// <param name="builder">The <see cref="AuthenticationBuilder"/> to which to add this configuration.</param>
        /// <param name="configureJwtBearerOptions">The action to configure <see cref="JwtBearerOptions"/>.</param>
        /// <param name="configureMicrosoftIdentityOptions">The action to configure the <see cref="MicrosoftIdentityOptions"/>.</param>
        /// <param name="jwtBearerScheme">The JWT bearer scheme name to be used. By default it uses "Bearer".</param>
        /// <param name="subscribeToJwtBearerMiddlewareDiagnosticsEvents">
        /// Set to true if you want to debug, or just understand the JWT bearer events.</param>
        /// <returns>The authentication builder to chain.</returns>
        public static MicrosoftIdentityWebApiAuthenticationBuilder AddMicrosoftIdentityWebApi(
            this AuthenticationBuilder builder,
            Action<JwtBearerOptions> configureJwtBearerOptions,
            Action<MicrosoftIdentityOptions> configureMicrosoftIdentityOptions,
            string jwtBearerScheme = JwtBearerDefaults.AuthenticationScheme,
            bool subscribeToJwtBearerMiddlewareDiagnosticsEvents = false)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (configureJwtBearerOptions == null)
            {
                throw new ArgumentNullException(nameof(configureJwtBearerOptions));
            }

            if (configureMicrosoftIdentityOptions == null)
            {
                throw new ArgumentNullException(nameof(configureMicrosoftIdentityOptions));
            }

            AddMicrosoftIdentityWebApiImplementation(
                builder,
                configureJwtBearerOptions,
                configureMicrosoftIdentityOptions,
                jwtBearerScheme,
                subscribeToJwtBearerMiddlewareDiagnosticsEvents);

            return new MicrosoftIdentityWebApiAuthenticationBuilder(
                builder.Services,
                jwtBearerScheme,
                configureJwtBearerOptions,
                configureMicrosoftIdentityOptions,
                null);
        }

        private static void AddMicrosoftIdentityWebApiImplementation(
            AuthenticationBuilder builder,
            Action<JwtBearerOptions> configureJwtBearerOptions,
            Action<MicrosoftIdentityOptions> configureMicrosoftIdentityOptions,
            string jwtBearerScheme,
            bool subscribeToJwtBearerMiddlewareDiagnosticsEvents)
        {
            builder.AddJwtBearer(jwtBearerScheme, configureJwtBearerOptions);
            builder.Services.Configure(jwtBearerScheme, configureMicrosoftIdentityOptions);

            builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IValidateOptions<MicrosoftIdentityOptions>, MicrosoftIdentityOptionsValidation>());
            builder.Services.AddHttpContextAccessor();
            builder.Services.AddHttpClient();

            if (subscribeToJwtBearerMiddlewareDiagnosticsEvents)
            {
                builder.Services.AddSingleton<IJwtBearerMiddlewareDiagnostics, JwtBearerMiddlewareDiagnostics>();
            }

            // Change the authentication configuration to accommodate the Microsoft identity platform endpoint (v2.0).
            builder.Services.AddOptions<JwtBearerOptions>(jwtBearerScheme)
                .Configure<IServiceProvider, IOptionsMonitor<MicrosoftIdentityOptions>>((options, serviceProvider, microsoftIdentityOptionsMonitor) =>
                {
                    var microsoftIdentityOptions = microsoftIdentityOptionsMonitor.Get(jwtBearerScheme);

                    if (string.IsNullOrWhiteSpace(options.Authority))
                    {
                        options.Authority = AuthorityHelpers.BuildAuthority(microsoftIdentityOptions);
                    }

                    // This is a Microsoft identity platform web API
                    options.Authority = AuthorityHelpers.EnsureAuthorityIsV2(options.Authority);

                    options.TokenValidationParameters = options.TokenValidationParameters.Clone();

                    if (options.TokenValidationParameters.AudienceValidator == null
                     && options.TokenValidationParameters.ValidAudience == null
                     && options.TokenValidationParameters.ValidAudiences == null)
                    {
                        RegisterValidAudience registerAudience = new RegisterValidAudience();
                        registerAudience.RegisterAudienceValidation(
                            options.TokenValidationParameters,
                            microsoftIdentityOptions);
                    }

                    // If the developer registered an IssuerValidator, do not overwrite it
                    if (options.TokenValidationParameters.IssuerValidator == null)
                    {
                        // Instead of using the default validation (validating against a single tenant, as we do in line of business apps),
                        // we inject our own multi-tenant validation logic (which even accepts both v1.0 and v2.0 tokens)
                        options.TokenValidationParameters.IssuerValidator = AadIssuerValidator.GetIssuerValidator(options.Authority).Validate;
                    }

                    // If you provide a token decryption certificate, it will be used to decrypt the token
                    if (microsoftIdentityOptions.TokenDecryptionCertificates != null)
                    {
                        options.TokenValidationParameters.TokenDecryptionKey =
                            new X509SecurityKey(DefaultCertificateLoader.LoadFirstCertificate(microsoftIdentityOptions.TokenDecryptionCertificates));
                    }

                    if (options.Events == null)
                    {
                        options.Events = new JwtBearerEvents();
                    }

                    // When an access token for our own web API is validated, we add it to MSAL.NET's cache so that it can
                    // be used from the controllers.
                    var tokenValidatedHandler = options.Events.OnTokenValidated;
                    options.Events.OnTokenValidated = async context =>
                    {
                        // This check is required to ensure that the web API only accepts tokens from tenants where it has been consented and provisioned.
                        if (!context.Principal.Claims.Any(x => x.Type == ClaimConstants.Scope)
                        && !context.Principal.Claims.Any(y => y.Type == ClaimConstants.Scp)
                        && !context.Principal.Claims.Any(y => y.Type == ClaimConstants.Roles)
                        && !context.Principal.Claims.Any(y => y.Type == ClaimConstants.Role))
                        {
                            throw new UnauthorizedAccessException(IDWebErrorMessage.NeitherScopeOrRolesClaimFoundInToken);
                        }

                        await tokenValidatedHandler(context).ConfigureAwait(false);
                    };

                    if (subscribeToJwtBearerMiddlewareDiagnosticsEvents)
                    {
                        var diagnostics = serviceProvider.GetRequiredService<IJwtBearerMiddlewareDiagnostics>();

                        diagnostics.Subscribe(options.Events);
                    }
                });
        }

#pragma warning disable SA1124 // Do not use regions
        #region Obsolete methods

        /// <summary>
        /// Protects the web API with Microsoft identity platform (formerly Azure AD v2.0).
        /// This method expects the configuration file will have a section, named "AzureAd" as default, with the necessary settings to initialize authentication options.
        /// </summary>
        /// <param name="builder">The <see cref="AuthenticationBuilder"/> to which to add this configuration.</param>
        /// <param name="configuration">The configuration instance.</param>
        /// <param name="configSectionName">The configuration section with the necessary settings to initialize authentication options.</param>
        /// <param name="jwtBearerScheme">The JWT bearer scheme name to be used. By default it uses "Bearer".</param>
        /// <param name="subscribeToJwtBearerMiddlewareDiagnosticsEvents">
        /// Set to true if you want to debug, or just understand the JWT bearer events.
        /// </param>
        /// <returns>The authentication builder to chain.</returns>
        [Obsolete("Rather use AddMicrosoftIdentityWebApi(). See https://aka.ms/ms-id-web/0.3.0-preview")]
#pragma warning restore SA1124 // Do not use regions
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static MicrosoftIdentityWebApiAuthenticationBuilderWithConfiguration AddMicrosoftWebApi(
            this AuthenticationBuilder builder,
            IConfiguration configuration,
            string configSectionName = IDWebConstants.AzureAd,
            string jwtBearerScheme = JwtBearerDefaults.AuthenticationScheme,
            bool subscribeToJwtBearerMiddlewareDiagnosticsEvents = false)
        {
            return builder.AddMicrosoftIdentityWebApi(
                configuration,
                configSectionName,
                jwtBearerScheme,
                subscribeToJwtBearerMiddlewareDiagnosticsEvents);
        }

        /// <summary>
        /// Protects the web API with Microsoft identity platform (formerly Azure AD v2.0).
        /// </summary>
        /// <param name="builder">The <see cref="AuthenticationBuilder"/> to which to add this configuration.</param>
        /// <param name="configureJwtBearerOptions">The action to configure <see cref="JwtBearerOptions"/>.</param>
        /// <param name="configureMicrosoftIdentityOptions">The action to configure the <see cref="MicrosoftIdentityOptions"/>.</param>
        /// <param name="jwtBearerScheme">The JWT bearer scheme name to be used. By default it uses "Bearer".</param>
        /// <param name="subscribeToJwtBearerMiddlewareDiagnosticsEvents">
        /// Set to true if you want to debug, or just understand the JWT bearer events.</param>
        /// <returns>The authentication builder to chain.</returns>
        [Obsolete("Rather use AddMicrosoftIdentityWebApi(). See https://aka.ms/ms-id-web/0.3.0-preview")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static MicrosoftIdentityWebApiAuthenticationBuilder AddMicrosoftWebApi(
            this AuthenticationBuilder builder,
            Action<JwtBearerOptions> configureJwtBearerOptions,
            Action<MicrosoftIdentityOptions> configureMicrosoftIdentityOptions,
            string jwtBearerScheme = JwtBearerDefaults.AuthenticationScheme,
            bool subscribeToJwtBearerMiddlewareDiagnosticsEvents = false)
        {
            return builder.AddMicrosoftIdentityWebApi(
                configureJwtBearerOptions,
                configureMicrosoftIdentityOptions,
                jwtBearerScheme,
                subscribeToJwtBearerMiddlewareDiagnosticsEvents);
        }
        #endregion
    }
}

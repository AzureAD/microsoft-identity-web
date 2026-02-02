// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Client;
using Microsoft.Identity.Web.Resource;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Microsoft.IdentityModel.Validators;

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// Extensions for <see cref="AuthenticationBuilder"/> for startup initialization of web APIs.
    /// </summary>
    public static class MicrosoftIdentityWebApiAuthenticationBuilderExtensions
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
#if NET6_0_OR_GREATER && !NET8_0_OR_GREATER
        [RequiresUnreferencedCode("Calls Microsoft.Identity.Web.MicrosoftIdentityWebApiAuthneticationBuilderExtensions.AddMicrosoftIdentityWebApi(AuthenticationBuilder, IConfigurationSection, string, bool).")]
#endif
        [SuppressMessage("ApiDesign", "RS0027", Justification = "Existing overload preserved for backward compatibility. New overloads with more parameters added for AOT support.")]
        public static MicrosoftIdentityWebApiAuthenticationBuilderWithConfiguration AddMicrosoftIdentityWebApi(
        this AuthenticationBuilder builder,
        IConfiguration configuration,
        string configSectionName = Constants.AzureAd,
        string jwtBearerScheme = JwtBearerDefaults.AuthenticationScheme,
        bool subscribeToJwtBearerMiddlewareDiagnosticsEvents = false)
        {
            _ = Throws.IfNull(configuration);
            _ = Throws.IfNull(configSectionName);

            IConfigurationSection configurationSection = configuration.GetSection(configSectionName);

            return builder.AddMicrosoftIdentityWebApi(
                configurationSection,
                jwtBearerScheme,
                subscribeToJwtBearerMiddlewareDiagnosticsEvents);
        }

        /// <summary>
        /// Protects the web API with Microsoft identity platform (formerly Azure AD v2.0).
        /// This method expects the configuration file will have a section, named "AzureAd" as default, with the necessary settings to initialize authentication options.
        /// This overload allows optional configuration of JwtBearerOptions.
        /// </summary>
        /// <param name="builder">The <see cref="AuthenticationBuilder"/> to which to add this configuration.</param>
        /// <param name="configuration">The configuration instance.</param>
        /// <param name="configSectionName">The configuration section with the necessary settings to initialize authentication options.</param>
        /// <param name="jwtBearerScheme">The JWT bearer scheme name to be used. By default it uses "Bearer".</param>
        /// <param name="configureJwtBearerOptions">Optional action to configure <see cref="JwtBearerOptions"/>.</param>
        /// <param name="subscribeToJwtBearerMiddlewareDiagnosticsEvents">
        /// Set to true if you want to debug, or just understand the JWT bearer events.
        /// </param>
        /// <returns>The authentication builder to chain.</returns>
#if NET6_0_OR_GREATER && !NET8_0_OR_GREATER
        [RequiresUnreferencedCode("Microsoft.Extensions.Configuration.ConfigurationBinder.Bind(IConfiguration, Object).")]
#endif
        public static AuthenticationBuilder AddMicrosoftIdentityWebApi(
            this AuthenticationBuilder builder,
            IConfiguration configuration,
            string configSectionName,
            string jwtBearerScheme,
            Action<JwtBearerOptions>? configureJwtBearerOptions,
            bool subscribeToJwtBearerMiddlewareDiagnosticsEvents)
        {
            _ = Throws.IfNull(configuration);
            _ = Throws.IfNull(configSectionName);

            IConfigurationSection configurationSection = configuration.GetSection(configSectionName);

            return builder.AddMicrosoftIdentityWebApi(
                configurationSection,
                jwtBearerScheme,
                configureJwtBearerOptions,
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
#if NET6_0_OR_GREATER && !NET8_0_OR_GREATER
        [RequiresUnreferencedCode("Microsoft.Extensions.Configuration.ConfigurationBinder.Bind(IConfiguration, Object).")]
#endif
        [SuppressMessage("ApiDesign", "RS0027", Justification = "Existing overload preserved for backward compatibility. New overloads with more parameters added for AOT support.")]
        public static MicrosoftIdentityWebApiAuthenticationBuilderWithConfiguration AddMicrosoftIdentityWebApi(
            this AuthenticationBuilder builder,
            IConfigurationSection configurationSection,
            string jwtBearerScheme = JwtBearerDefaults.AuthenticationScheme,
            bool subscribeToJwtBearerMiddlewareDiagnosticsEvents = false)
        {
            _ = Throws.IfNull(configurationSection);
            _ = Throws.IfNull(builder);
            
            AddMicrosoftIdentityWebApiImplementation(
                builder,
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
        /// This method expects the configuration file will have a section with the necessary settings to initialize authentication options.
        /// This overload allows optional configuration of JwtBearerOptions.
        /// </summary>
        /// <param name="builder">The <see cref="AuthenticationBuilder"/> to which to add this configuration.</param>
        /// <param name="configurationSection">The configuration section from which to fill-in the options.</param>
        /// <param name="jwtBearerScheme">The JWT bearer scheme name to be used. By default it uses "Bearer".</param>
        /// <param name="configureJwtBearerOptions">Optional action to configure <see cref="JwtBearerOptions"/>.</param>
        /// <param name="subscribeToJwtBearerMiddlewareDiagnosticsEvents">
        /// Set to true if you want to debug, or just understand the JWT bearer events.
        /// </param>
        /// <returns>The authentication builder to chain.</returns>
#if NET6_0_OR_GREATER && !NET8_0_OR_GREATER
        [RequiresUnreferencedCode("Microsoft.Extensions.Configuration.ConfigurationBinder.Bind(IConfiguration, Object).")]
#endif
        public static AuthenticationBuilder AddMicrosoftIdentityWebApi(
            this AuthenticationBuilder builder,
            IConfigurationSection configurationSection,
            string jwtBearerScheme,
            Action<JwtBearerOptions>? configureJwtBearerOptions,
            bool subscribeToJwtBearerMiddlewareDiagnosticsEvents)
        {
            _ = Throws.IfNull(configurationSection);
            _ = Throws.IfNull(builder);

            return builder.AddMicrosoftIdentityWebApi(
                (MicrosoftIdentityApplicationOptions options) => configurationSection.Bind(options),
                jwtBearerScheme,
                configureJwtBearerOptions,
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
#if NET6_0_OR_GREATER && !NET8_0_OR_GREATER
        [RequiresUnreferencedCode("Microsoft.Identity.Web.MicrosoftIdentityWebApiAuthenticationBuilder.MicrosoftIdentityWebApiAuthenticationBuilder(IServiceCollection, String, Action<JwtBearerOptions>, Action<MicrosoftIdentityOptions>, IConfigurationSection).")]
#endif
        [SuppressMessage("ApiDesign", "RS0027", Justification = "Existing overload preserved for backward compatibility. New overloads with more parameters added for AOT support.")]
        public static MicrosoftIdentityWebApiAuthenticationBuilder AddMicrosoftIdentityWebApi(
            this AuthenticationBuilder builder,
            Action<JwtBearerOptions> configureJwtBearerOptions,
            Action<MicrosoftIdentityOptions> configureMicrosoftIdentityOptions,
            string jwtBearerScheme = JwtBearerDefaults.AuthenticationScheme,
            bool subscribeToJwtBearerMiddlewareDiagnosticsEvents = false)
        {
            _ = Throws.IfNull(builder);
            _ = Throws.IfNull(configureJwtBearerOptions);
            _ = Throws.IfNull(configureMicrosoftIdentityOptions);

            AddMicrosoftIdentityWebApiImplementation(
                builder,
                configureJwtBearerOptions,
                jwtBearerScheme,
                subscribeToJwtBearerMiddlewareDiagnosticsEvents);

            return new MicrosoftIdentityWebApiAuthenticationBuilder(
                builder.Services,
                jwtBearerScheme,
                configureJwtBearerOptions,
                configureMicrosoftIdentityOptions,
                null);
        }

        /// <summary>
        /// Protects the web API with Microsoft identity platform using AOT-compatible configuration.
        /// This overload accepts an action to configure <see cref="MicrosoftIdentityApplicationOptions"/> programmatically,
        /// making it suitable for Native AOT scenarios where reflection-based configuration binding is not available.
        /// </summary>
        /// <param name="builder">The <see cref="AuthenticationBuilder"/> to which to add this configuration.</param>
        /// <param name="configureOptions">The action to configure <see cref="MicrosoftIdentityApplicationOptions"/>.</param>
        /// <param name="jwtBearerScheme">The JWT bearer scheme name to be used.</param>
        /// <param name="configureJwtBearerOptions">Optional action to configure <see cref="JwtBearerOptions"/>.</param>
        /// <param name="subscribeToJwtBearerMiddlewareDiagnosticsEvents">
        /// Set to true if you want to debug, or just understand the JWT bearer events.</param>
        /// <returns>The authentication builder to chain.</returns>
        public static AuthenticationBuilder AddMicrosoftIdentityWebApi(
            this AuthenticationBuilder builder,
            Action<MicrosoftIdentityApplicationOptions> configureOptions,
            string jwtBearerScheme,
            Action<JwtBearerOptions>? configureJwtBearerOptions,
            bool subscribeToJwtBearerMiddlewareDiagnosticsEvents)
        {
            _ = Throws.IfNull(builder);
            _ = Throws.IfNull(configureOptions);

            return AddMicrosoftIdentityWebApiWithApplicationOptions(
                builder,
                configureOptions,
                jwtBearerScheme,
                configureJwtBearerOptions,
                subscribeToJwtBearerMiddlewareDiagnosticsEvents);
        }

        /// <summary>
        /// Protects the web API with Microsoft Entra ID (Azure AD) using AOT-compatible configuration.
        /// This overload is specifically for Entra ID (non-B2C) scenarios and accepts an action to configure 
        /// <see cref="MicrosoftEntraApplicationOptions"/> programmatically.
        /// </summary>
        /// <param name="builder">The <see cref="AuthenticationBuilder"/> to which to add this configuration.</param>
        /// <param name="configureOptions">The action to configure <see cref="MicrosoftEntraApplicationOptions"/>.</param>
        /// <param name="jwtBearerScheme">The JWT bearer scheme name to be used.</param>
        /// <param name="configureJwtBearerOptions">Optional action to configure <see cref="JwtBearerOptions"/>.</param>
        /// <param name="subscribeToJwtBearerMiddlewareDiagnosticsEvents">
        /// Set to true if you want to debug, or just understand the JWT bearer events.</param>
        /// <returns>The authentication builder to chain.</returns>
        public static AuthenticationBuilder AddMicrosoftIdentityWebApi(
            this AuthenticationBuilder builder,
            Action<MicrosoftEntraApplicationOptions> configureOptions,
            string jwtBearerScheme,
            Action<JwtBearerOptions>? configureJwtBearerOptions,
            bool subscribeToJwtBearerMiddlewareDiagnosticsEvents)
        {
            _ = Throws.IfNull(builder);
            _ = Throws.IfNull(configureOptions);

            // MicrosoftEntraApplicationOptions inherits from MicrosoftIdentityApplicationOptions
            // Configure using Entra options but pass as base type
            return builder.AddMicrosoftIdentityWebApi(
                (MicrosoftIdentityApplicationOptions options) =>
                {
                    // Create a temporary Entra options instance to configure
                    var entraOptions = new MicrosoftEntraApplicationOptions();
                    configureOptions(entraOptions);
                    
                    // Copy all properties from entraOptions to options
                    options.ClientId = entraOptions.ClientId;
                    options.Instance = entraOptions.Instance;
                    options.TenantId = entraOptions.TenantId;
                    options.Authority = entraOptions.Authority;
                    options.ClientCredentials = entraOptions.ClientCredentials;
                    options.TokenDecryptionCredentials = entraOptions.TokenDecryptionCredentials;
                    options.ClientCapabilities = entraOptions.ClientCapabilities;
                    options.AzureRegion = entraOptions.AzureRegion;
                    options.EnablePiiLogging = entraOptions.EnablePiiLogging;
                    options.AllowWebApiToBeAuthorizedByACL = entraOptions.AllowWebApiToBeAuthorizedByACL;
                    options.Audience = entraOptions.Audience;
                    options.Audiences = entraOptions.Audiences;
                },
                jwtBearerScheme,
                configureJwtBearerOptions,
                subscribeToJwtBearerMiddlewareDiagnosticsEvents);
        }

        private static AuthenticationBuilder AddMicrosoftIdentityWebApiWithApplicationOptions(
            AuthenticationBuilder builder,
            Action<MicrosoftIdentityApplicationOptions> configureOptions,
            string jwtBearerScheme,
            Action<JwtBearerOptions>? configureJwtBearerOptions,
            bool subscribeToJwtBearerMiddlewareDiagnosticsEvents)
        {
            // 1. Register MicrosoftIdentityApplicationOptions
            builder.Services.Configure(jwtBearerScheme, configureOptions);

            // 2. Register JwtBearer scheme with optional user configuration
            builder.AddJwtBearer(jwtBearerScheme, configureJwtBearerOptions ?? (_ => { }));

            // 3. Infrastructure services
            builder.Services.AddHttpContextAccessor();
            builder.Services.AddHttpClient();
            builder.Services.TryAddSingleton<MicrosoftIdentityIssuerValidatorFactory>();
            builder.Services.AddRequiredScopeAuthorization();
            builder.Services.AddRequiredScopeOrAppPermissionAuthorization();
            builder.Services.AddOptions<AadIssuerValidatorOptions>();

            if (subscribeToJwtBearerMiddlewareDiagnosticsEvents)
            {
                builder.Services.AddTransient<IJwtBearerMiddlewareDiagnostics, JwtBearerMiddlewareDiagnostics>();
            }

            // 4. Configure JwtBearerOptions FROM MicrosoftIdentityApplicationOptions
            builder.Services.AddOptions<JwtBearerOptions>(jwtBearerScheme)
                .Configure<IServiceProvider, IOptionsMonitor<MicrosoftIdentityApplicationOptions>>((
                    options,
                    serviceProvider,
                    appOptionsMonitor) =>
                {
                    MicrosoftIdentityBaseAuthenticationBuilder.SetIdentityModelLogger(serviceProvider);
                    var appOptions = appOptionsMonitor.Get(jwtBearerScheme);
                    ConfigureJwtBearerOptionsFromApplicationOptions(
                        options,
                        appOptions,
                        serviceProvider,
                        jwtBearerScheme,
                        subscribeToJwtBearerMiddlewareDiagnosticsEvents);
                });

            // 5. Configure ConfidentialClientApplicationOptions FROM MicrosoftIdentityApplicationOptions
            builder.Services.AddOptions<ConfidentialClientApplicationOptions>(jwtBearerScheme)
                .Configure<IOptionsMonitor<MicrosoftIdentityApplicationOptions>>((
                    ccaOptions,
                    appOptionsMonitor) =>
                {
                    var appOptions = appOptionsMonitor.Get(jwtBearerScheme);
                    ConfigureConfidentialClientOptionsFromApplicationOptions(ccaOptions, appOptions);
                });

            return builder;
        }

        private static void ConfigureJwtBearerOptionsFromApplicationOptions(
            JwtBearerOptions options,
            MicrosoftIdentityApplicationOptions appOptions,
            IServiceProvider serviceProvider,
            string jwtBearerScheme,
            bool subscribeToJwtBearerMiddlewareDiagnosticsEvents)
        {
            // Validate configuration
            ValidateMicrosoftIdentityApplicationOptions(appOptions);

            // Build and set authority
            string authority = BuildAuthorityFromApplicationOptions(appOptions);
            options.Authority = AuthorityHelpers.EnsureAuthorityIsV2(authority);

            // Audience validation (only if not already configured)
            if (options.TokenValidationParameters.AudienceValidator == null
                && options.TokenValidationParameters.ValidAudience == null
                && options.TokenValidationParameters.ValidAudiences == null)
            {
                ConfigureAudienceValidationFromApplicationOptions(options.TokenValidationParameters, appOptions);
            }

            // Issuer validation (only if not already configured)
            if (options.TokenValidationParameters.ValidateIssuer
                && options.TokenValidationParameters.IssuerValidator == null)
            {
                var issuerValidatorFactory = serviceProvider.GetRequiredService<MicrosoftIdentityIssuerValidatorFactory>();
                options.TokenValidationParameters.IssuerValidator =
                    issuerValidatorFactory.GetAadIssuerValidator(options.Authority).Validate;
            }

            // Token decryption
            if (appOptions.TokenDecryptionCredentials?.Any() == true)
            {
                ConfigureTokenDecryptionFromApplicationOptions(options.TokenValidationParameters, appOptions);
            }

            // =========================================================================
            // EVENTS SETUP
            // =========================================================================
            options.Events ??= new JwtBearerEvents();
            options.TokenValidationParameters.EnableAadSigningKeyIssuerValidation();

            // Configuration manager setup
            var existingOnMessageReceivedHandler = options.Events.OnMessageReceived;
            options.Events.OnMessageReceived = async context =>
            {
                context.Options.TokenValidationParameters.ConfigurationManager ??=
                    context.Options.ConfigurationManager as BaseConfigurationManager;
                await existingOnMessageReceivedHandler(context).ConfigureAwait(false);
            };

            // =========================================================================
            // CRITICAL: Always store the validated token for TokenAcquisition (OBO flow)
            // This enables:
            //   services.AddMicrosoftIdentityWebApi(...)
            //   services.AddTokenAcquisition()
            // to work without needing EnableTokenAcquisitionToCallDownstreamApi()
            // =========================================================================
            var existingOnTokenValidatedHandler = options.Events.OnTokenValidated;
            options.Events.OnTokenValidated = async context =>
            {
                // Store the SecurityToken so TokenAcquisition can use it for OBO
                // Only store if it's a JWT type that can be used for OBO
                if (context.SecurityToken is JwtSecurityToken or JsonWebToken)
                {
                    context.HttpContext.StoreTokenUsedToCallWebAPI(context.SecurityToken);
                }

                await existingOnTokenValidatedHandler(context).ConfigureAwait(false);
            };

            // Claims validation (unless ACL mode)
            if (!appOptions.AllowWebApiToBeAuthorizedByACL)
            {
                ChainOnTokenValidatedEventForClaimsValidation(options.Events, jwtBearerScheme);
            }

            // Diagnostics subscription
            if (subscribeToJwtBearerMiddlewareDiagnosticsEvents)
            {
                var diagnostics = serviceProvider.GetRequiredService<IJwtBearerMiddlewareDiagnostics>();
                diagnostics.Subscribe(options.Events);
            }
        }

        private static string BuildAuthorityFromApplicationOptions(MicrosoftIdentityApplicationOptions options)
        {
            if (!string.IsNullOrEmpty(options.Authority))
            {
                return options.Authority;
            }

            string instance = options.Instance?.TrimEnd('/') ?? "https://login.microsoftonline.com";

            // B2C authority format
            bool isB2C = !string.IsNullOrWhiteSpace(options.SignUpSignInPolicyId);
            if (isB2C)
            {
                return $"{instance}/{options.Domain}/{options.SignUpSignInPolicyId}/v2.0";
            }

            // AAD authority format
            string tenantId = options.TenantId ?? "common";
            return $"{instance}/{tenantId}/v2.0";
        }

        private static void ValidateMicrosoftIdentityApplicationOptions(MicrosoftIdentityApplicationOptions options)
        {
            if (string.IsNullOrEmpty(options.ClientId))
            {
                throw new ArgumentNullException(nameof(options.ClientId),
                    string.Format(CultureInfo.InvariantCulture, IDWebErrorMessage.ConfigurationOptionRequired, nameof(options.ClientId)));
            }

            if (string.IsNullOrEmpty(options.Authority))
            {
                if (string.IsNullOrEmpty(options.Instance))
                {
                    throw new ArgumentNullException(nameof(options.Instance),
                        string.Format(CultureInfo.InvariantCulture, IDWebErrorMessage.ConfigurationOptionRequired, nameof(options.Instance)));
                }

                bool isB2C = !string.IsNullOrWhiteSpace(options.SignUpSignInPolicyId);
                if (isB2C)
                {
                    if (string.IsNullOrEmpty(options.Domain))
                    {
                        throw new ArgumentNullException(nameof(options.Domain),
                            string.Format(CultureInfo.InvariantCulture, IDWebErrorMessage.ConfigurationOptionRequired, nameof(options.Domain)));
                    }
                }
                else
                {
                    if (string.IsNullOrEmpty(options.TenantId))
                    {
                        throw new ArgumentNullException(nameof(options.TenantId),
                            string.Format(CultureInfo.InvariantCulture, IDWebErrorMessage.ConfigurationOptionRequired, nameof(options.TenantId)));
                    }
                }
            }
        }

        private static void ConfigureConfidentialClientOptionsFromApplicationOptions(
            ConfidentialClientApplicationOptions ccaOptions,
            MicrosoftIdentityApplicationOptions appOptions)
        {
            ccaOptions.ClientId = appOptions.ClientId;
            ccaOptions.Instance = appOptions.Instance;
            ccaOptions.TenantId = appOptions.TenantId;
            ccaOptions.AzureRegion = appOptions.AzureRegion;
            ccaOptions.ClientCapabilities = appOptions.ClientCapabilities;
            ccaOptions.EnablePiiLogging = appOptions.EnablePiiLogging;

            // Client secret from credentials
            var secretCredential = appOptions.ClientCredentials?
                .FirstOrDefault(c => c.CredentialType == CredentialType.Secret);
            if (secretCredential != null)
            {
                ccaOptions.ClientSecret = secretCredential.ClientSecret;
            }
        }

        private static void ConfigureAudienceValidationFromApplicationOptions(
            TokenValidationParameters tokenValidationParameters,
            MicrosoftIdentityApplicationOptions options)
        {
            // At this point, ClientId has been validated by ValidateMicrosoftIdentityApplicationOptions
            var validAudiences = new List<string> { options.ClientId! };

            // Add api://{clientId} format
            if (!options.ClientId!.StartsWith("api://", StringComparison.OrdinalIgnoreCase))
            {
                validAudiences.Add($"api://{options.ClientId}");
            }

            tokenValidationParameters.ValidAudiences = validAudiences;
        }

        private static void ConfigureTokenDecryptionFromApplicationOptions(
            TokenValidationParameters tokenValidationParameters,
            MicrosoftIdentityApplicationOptions options)
        {
            // At this point, TokenDecryptionCredentials has been validated to be non-null
            var certificates = DefaultCertificateLoader.LoadAllCertificates(
                options.TokenDecryptionCredentials!.OfType<CertificateDescription>());
            var keys = certificates.Select(c => new X509SecurityKey(c));
            tokenValidationParameters.TokenDecryptionKeys = keys;
        }

        private static void AddMicrosoftIdentityWebApiImplementation(
            AuthenticationBuilder builder,
            Action<JwtBearerOptions> configureJwtBearerOptions,
            string jwtBearerScheme,
            bool subscribeToJwtBearerMiddlewareDiagnosticsEvents)
        {
            builder.AddJwtBearer(jwtBearerScheme, configureJwtBearerOptions);
            builder.Services.AddSingleton<IMergedOptionsStore, MergedOptionsStore>();

            builder.Services.AddHttpContextAccessor();
            builder.Services.AddHttpClient();
            builder.Services.TryAddSingleton<MicrosoftIdentityIssuerValidatorFactory>();
            builder.Services.AddRequiredScopeAuthorization();
            builder.Services.AddRequiredScopeOrAppPermissionAuthorization();
            builder.Services.AddOptions<AadIssuerValidatorOptions>();
            if (!HasImplementationType(builder.Services, typeof(MicrosoftIdentityOptionsMerger)))
            {
                builder.Services.TryAddSingleton<IPostConfigureOptions<MicrosoftIdentityOptions>, MicrosoftIdentityOptionsMerger>();
            }
            if (!HasImplementationType(builder.Services, typeof(JwtBearerOptionsMerger)))
            {
                builder.Services.TryAddSingleton<IPostConfigureOptions<JwtBearerOptions>, JwtBearerOptionsMerger>();
            }
            if (subscribeToJwtBearerMiddlewareDiagnosticsEvents)
            {
                builder.Services.AddTransient<IJwtBearerMiddlewareDiagnostics, JwtBearerMiddlewareDiagnostics>();
            }

            // Change the authentication configuration to accommodate the Microsoft identity platform endpoint (v2.0).
            builder.Services.AddOptions<JwtBearerOptions>(jwtBearerScheme)
                .Configure<IServiceProvider, IMergedOptionsStore, IOptionsMonitor<MicrosoftIdentityOptions>>((
                options,
                serviceProvider,
                mergedOptionsMonitor,
                msIdOptionsMonitor) =>
                {
                    MicrosoftIdentityBaseAuthenticationBuilder.SetIdentityModelLogger(serviceProvider);
                    msIdOptionsMonitor.Get(jwtBearerScheme); // needed for firing the PostConfigure.
                    MergedOptions mergedOptions = mergedOptionsMonitor.Get(jwtBearerScheme);

                    // Process OIDC compliant tenants
                    if (mergedOptions.Authority != null)
                    {
                        mergedOptions.Authority = AuthorityHelpers.GetAuthorityWithoutQueryIfNeeded(mergedOptions);
                        mergedOptions.Authority = AuthorityHelpers.BuildCiamAuthorityIfNeeded(mergedOptions.Authority, out bool preserveAuthority);
                        mergedOptions.PreserveAuthority = preserveAuthority;
                        options.Authority = mergedOptions.Authority;
                    }

                    // Validate the configuration of the web API
                    MergedOptionsValidation.Validate(mergedOptions);

                    // Ensure a well-formed authority was provided
                    if (string.IsNullOrWhiteSpace(options.Authority))
                    {
                        options.Authority = AuthorityHelpers.BuildAuthority(mergedOptions);
                    }

                    // This is a Microsoft identity platform web API
                    options.Authority = AuthorityHelpers.EnsureAuthorityIsV2(options.Authority);

                    if (options.TokenValidationParameters.AudienceValidator == null
                     && options.TokenValidationParameters.ValidAudience == null
                     && options.TokenValidationParameters.ValidAudiences == null)
                    {
                        RegisterValidAudience registerAudience = new RegisterValidAudience();
                        registerAudience.RegisterAudienceValidation(
                            options.TokenValidationParameters,
                            mergedOptions);
                    }

                    // If the developer registered an IssuerValidator, do not overwrite it
                    if (options.TokenValidationParameters.ValidateIssuer && options.TokenValidationParameters.IssuerValidator == null)
                    {
                        // Instead of using the default validation (validating against a single tenant, as we do in line of business apps),
                        // we inject our own multi-tenant validation logic (which even accepts both v1.0 and v2.0 tokens)
                        MicrosoftIdentityIssuerValidatorFactory microsoftIdentityIssuerValidatorFactory =
                        serviceProvider.GetRequiredService<MicrosoftIdentityIssuerValidatorFactory>();

                        options.TokenValidationParameters.IssuerValidator =
                        microsoftIdentityIssuerValidatorFactory.GetAadIssuerValidator(options.Authority).Validate;
                    }

                    // If you provide a token decryption certificate, it will be used to decrypt the token
                    // TODO use the credential loader
                    if (mergedOptions.TokenDecryptionCredentials != null)
                    {
                        DefaultCertificateLoader.UserAssignedManagedIdentityClientId = mergedOptions.UserAssignedManagedIdentityClientId;
                        IEnumerable<X509Certificate2?> certificates = DefaultCertificateLoader.LoadAllCertificates(mergedOptions.TokenDecryptionCredentials.OfType<CertificateDescription>());
                        IEnumerable<X509SecurityKey> keys = certificates.Select(c => new X509SecurityKey(c));
                        options.TokenValidationParameters.TokenDecryptionKeys = keys;
                    }

                    if (options.Events == null)
                    {
                        options.Events = new JwtBearerEvents();
                    }

                    options.TokenValidationParameters.EnableAadSigningKeyIssuerValidation();
                    options.Events.OnMessageReceived += async context =>
                    {
                        context.Options.TokenValidationParameters.ConfigurationManager ??= options.ConfigurationManager as BaseConfigurationManager;
                        await Task.CompletedTask.ConfigureAwait(false);
                    };

                    // When an access token for our own web API is validated, we add it to MSAL.NET's cache so that it can
                    // be used from the controllers.

                    if (!mergedOptions.AllowWebApiToBeAuthorizedByACL)
                    {
                        ChainOnTokenValidatedEventForClaimsValidation(options.Events, jwtBearerScheme);
                    }

                    if (subscribeToJwtBearerMiddlewareDiagnosticsEvents)
                    {
                        var diagnostics = serviceProvider.GetRequiredService<IJwtBearerMiddlewareDiagnostics>();

                        diagnostics.Subscribe(options.Events);
                    }
                });
        }

        /// <summary>
        /// In order to ensure that the Web API only accepts tokens from tenants where it has been consented and provisioned, a token that
        /// has neither Roles nor Scopes claims should be rejected. To enforce that rule, add an event handler to the beginning of the
        /// <see cref="JwtBearerEvents.OnTokenValidated"/> handler chain that rejects tokens that don't meet the rules.
        /// </summary>
        /// <param name="events">The <see cref="JwtBearerEvents"/> object to modify.</param>
        /// <param name="jwtBearerScheme">The JWT bearer scheme name to be used. By default it uses "Bearer".</param>
        internal static void ChainOnTokenValidatedEventForClaimsValidation(JwtBearerEvents events, string jwtBearerScheme)
        {
            var tokenValidatedHandler = events.OnTokenValidated;
            events.OnTokenValidated = async context =>
            {
                if (!context!.Principal!.Claims.Any(x => x.Type == ClaimConstants.Scope
                        || x.Type == ClaimConstants.Scp
                        || x.Type == ClaimConstants.Roles
                        || x.Type == ClaimConstants.Role))
                {
                    context.Fail(string.Format(CultureInfo.InvariantCulture, IDWebErrorMessage.NeitherScopeOrRolesClaimFoundInToken, jwtBearerScheme));
                }

                await tokenValidatedHandler(context).ConfigureAwait(false);
            };
        }

        private static bool HasImplementationType(IServiceCollection services, Type implementationType)
        {
            return services.Any(s =>
#if NET8_0_OR_GREATER
                s.ServiceKey is null &&
#endif
                s.ImplementationType == implementationType);
        }
    }
}

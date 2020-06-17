// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
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
    /// Extensions for AuthenticationBuilder for startup initialization of Web APIs.
    /// </summary>
    public static partial class WebApiAuthenticationBuilderExtensions
    {
        /// <summary>
        /// Protects the Web API with Microsoft identity platform (formerly Azure AD v2.0)
        /// This method expects the configuration file will have a section, named "AzureAd" as default, with the necessary settings to initialize authentication options.
        /// </summary>
        /// <param name="builder">AuthenticationBuilder to which to add this configuration.</param>
        /// <param name="configuration">The Configuration object.</param>
        /// <param name="configSectionName">The configuration section with the necessary settings to initialize authentication options.</param>
        /// <param name="jwtBearerScheme">The JwtBearer scheme name to be used. By default it uses "Bearer".</param>
        /// <param name="subscribeToJwtBearerMiddlewareDiagnosticsEvents">
        /// Set to true if you want to debug, or just understand the JwtBearer events.
        /// </param>
        /// <returns>The authentication builder to chain.</returns>
        public static AuthenticationBuilder AddMicrosoftWebApi(
            this AuthenticationBuilder builder,
            IConfiguration configuration,
            string configSectionName = "AzureAd",
            string jwtBearerScheme = JwtBearerDefaults.AuthenticationScheme,
            bool subscribeToJwtBearerMiddlewareDiagnosticsEvents = false)
        {
            return builder.AddMicrosoftWebApi(
                options => configuration.Bind(configSectionName, options),
                options => configuration.Bind(configSectionName, options),
                jwtBearerScheme,
                subscribeToJwtBearerMiddlewareDiagnosticsEvents);
        }

        /// <summary>
        /// Protects the Web API with Microsoft identity platform (formerly Azure AD v2.0)
        /// This method expects the configuration file will have a section, named "AzureAd" as default, with the necessary settings to initialize authentication options.
        /// </summary>
        /// <param name="builder">AuthenticationBuilder to which to add this configuration.</param>
        /// <param name="configureJwtBearerOptions">The action to configure <see cref="JwtBearerOptions"/>.</param>
        /// <param name="configureMicrosoftIdentityOptions">The action to configure the <see cref="MicrosoftIdentityOptions"/>
        /// configuration options.</param>
        /// <param name="jwtBearerScheme">The JwtBearer scheme name to be used. By default it uses "Bearer".</param>
        /// <param name="subscribeToJwtBearerMiddlewareDiagnosticsEvents">
        /// Set to true if you want to debug, or just understand the JwtBearer events.
        /// </param>
        /// <returns>The authentication builder to chain.</returns>
        public static AuthenticationBuilder AddMicrosoftWebApi(
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

            builder.Services.Configure(jwtBearerScheme, configureJwtBearerOptions);
            builder.Services.Configure<MicrosoftIdentityOptions>(configureMicrosoftIdentityOptions);

            builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IValidateOptions<MicrosoftIdentityOptions>, MicrosoftIdentityOptionsValidation>());
            builder.Services.AddHttpContextAccessor();
            builder.Services.AddSingleton<IJwtBearerMiddlewareDiagnostics, JwtBearerMiddlewareDiagnostics>();
            builder.Services.AddHttpClient();

            // Change the authentication configuration to accommodate the Microsoft identity platform endpoint (v2.0).
            builder.AddJwtBearer(jwtBearerScheme, options =>
            {
                // TODO:
                // Suspect. Why not get the IOption<MicrosoftIdentityOptions>?
                var microsoftIdentityOptions = new MicrosoftIdentityOptions(); // configuration.GetSection(configSectionName).Get<MicrosoftIdentityOptions>();
                configureMicrosoftIdentityOptions(microsoftIdentityOptions);

                if (string.IsNullOrWhiteSpace(options.Authority))
                {
                    options.Authority = AuthorityHelpers.BuildAuthority(microsoftIdentityOptions);
                }

                // This is a Microsoft identity platform Web API
                options.Authority = AuthorityHelpers.EnsureAuthorityIsV2(options.Authority);

                if (options.TokenValidationParameters.AudienceValidator == null)
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

                // When an access token for our own Web API is validated, we add it to MSAL.NET's cache so that it can
                // be used from the controllers.
                var tokenValidatedHandler = options.Events.OnTokenValidated;
                options.Events.OnTokenValidated = async context =>
                {
                    // This check is required to ensure that the Web API only accepts tokens from tenants where it has been consented and provisioned.
                    if (!context.Principal.Claims.Any(x => x.Type == ClaimConstants.Scope)
                    && !context.Principal.Claims.Any(y => y.Type == ClaimConstants.Scp)
                    && !context.Principal.Claims.Any(y => y.Type == ClaimConstants.Roles)
                    && !context.Principal.Claims.Any(y => y.Type == ClaimConstants.Role))
                    {
                        throw new UnauthorizedAccessException("Neither scope or roles claim was found in the bearer token.");
                    }

                    await tokenValidatedHandler(context).ConfigureAwait(false);
                };

                if (subscribeToJwtBearerMiddlewareDiagnosticsEvents)
                {
                    var diags = builder.Services.BuildServiceProvider().GetRequiredService<IJwtBearerMiddlewareDiagnostics>();

                    diags.Subscribe(options.Events);
                }
            });

            return builder;
        }
    }
}

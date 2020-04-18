// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Web.Resource;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// Extensions for AuthenticationBuilder for startup initialization of Web APIs.
    /// </summary>
    public static class WebApiAuthenticationBuilderExtensions
    {
        /// <summary>
        /// Protects the Web API with Microsoft identity platform (formerly Azure AD v2.0)
        /// This method expects the configuration file will have a section, named "AzureAd" as default, with the necessary settings to initialize authentication options.
        /// </summary>
        /// <param name="builder">AuthenticationBuilder to which to add this configuration.</param>
        /// <param name="configuration">The Configuration object.</param>
        /// <param name="configSectionName">The configuration section with the necessary settings to initialize authentication options.</param>
        /// <param name="jwtBearerScheme">The JwtBearer scheme name to be used. By default it uses "Bearer".</param>
        /// <param name="tokenDecryptionCertificate">Token decryption certificate (null by default).</param>
        /// <param name="subscribeToJwtBearerMiddlewareDiagnosticsEvents">
        /// Set to true if you want to debug, or just understand the JwtBearer events.
        /// </param>
        /// <returns>The authentication builder to chain.</returns>
        public static AuthenticationBuilder AddProtectedWebApi(
            this AuthenticationBuilder builder,
            IConfiguration configuration,
            string configSectionName = "AzureAd",
            string jwtBearerScheme = JwtBearerDefaults.AuthenticationScheme,
            X509Certificate2 tokenDecryptionCertificate = null,
            bool subscribeToJwtBearerMiddlewareDiagnosticsEvents = false)
        {
            return builder.AddProtectedWebApi(options => configuration.Bind(configSectionName, options),
                options => configuration.Bind(configSectionName, options),
                tokenDecryptionCertificate,
                jwtBearerScheme,
                subscribeToJwtBearerMiddlewareDiagnosticsEvents);
        }

        /// <summary>
        /// Protects the Web API with Microsoft identity platform (formerly Azure AD v2.0)
        /// This method expects the configuration file will have a section, named "AzureAd" as default, with the necessary settings to initialize authentication options.
        /// </summary>
        /// <param name="builder">AuthenticationBuilder to which to add this configuration.</param>
        /// <param name="configureJwtBearerOptions">The action to configure <see cref="JwtBearerOptions"/>.</param>
        /// <param name="configureMicrosoftIdentityOptions">The action to configure <see cref="configureMicrosoftIdentityOptions"/>.</param>
        /// <param name="jwtBearerScheme">The JwtBearer scheme name to be used. By default it uses "Bearer".</param>
        /// <param name="configureJwtBearerOptions">An action to configure JwtBearerOptions.</param>
        /// <param name="tokenDecryptionCertificate">Token decryption certificate.</param>
        /// <param name="subscribeToJwtBearerMiddlewareDiagnosticsEvents">
        /// Set to true if you want to debug, or just understand the JwtBearer events.
        /// </param>
        /// <returns>The authentication builder to chain.</returns>
        public static AuthenticationBuilder AddProtectedWebApi(
        this AuthenticationBuilder builder,
        Action<JwtBearerOptions> configureJwtBearerOptions,
        Action<MicrosoftIdentityOptions> configureMicrosoftIdentityOptions,
        X509Certificate2 tokenDecryptionCertificate = null,
        string jwtBearerScheme = JwtBearerDefaults.AuthenticationScheme,
        bool subscribeToJwtBearerMiddlewareDiagnosticsEvents = false)
        {
            builder.Services.Configure(jwtBearerScheme, configureJwtBearerOptions);
            builder.Services.Configure<MicrosoftIdentityOptions>(configureMicrosoftIdentityOptions);

            builder.Services.AddHttpContextAccessor();
            builder.Services.AddSingleton<IJwtBearerMiddlewareDiagnostics, JwtBearerMiddlewareDiagnostics>();

            // Change the authentication configuration to accommodate the Microsoft identity platform endpoint (v2.0).
            builder.AddJwtBearer(jwtBearerScheme, options =>
            {
                // TODO:
                // Suspect. Why not get the IOption<MicrosoftIdentityOptions>?
                var microsoftIdentityOptions = new MicrosoftIdentityOptions();// configuration.GetSection(configSectionName).Get<MicrosoftIdentityOptions>();
                configureMicrosoftIdentityOptions(microsoftIdentityOptions);

                if (string.IsNullOrWhiteSpace(options.Authority))
                {
                    options.Authority = AuthorityHelpers.BuildAuthority(microsoftIdentityOptions);
                }

                // This is a Microsoft identity platform Web API
                options.Authority = AuthorityHelpers.EnsureAuthorityIsV2(options.Authority);

                // The valid audience could be given as Client Id or as Uri.
                // If it does not start with 'api://', this variant is added to the list of valid audiences.
                EnsureValidAudiencesContainsApiGuidIfGuidProvided(options, microsoftIdentityOptions);

                // If the developer registered an IssuerValidator, do not overwrite it
                if (options.TokenValidationParameters.IssuerValidator == null)
                {
                    // Instead of using the default validation (validating against a single tenant, as we do in line of business apps),
                    // we inject our own multi-tenant validation logic (which even accepts both v1.0 and v2.0 tokens)
                    options.TokenValidationParameters.IssuerValidator = AadIssuerValidator.GetIssuerValidator(options.Authority).Validate;
                }

                // If you provide a token decryption certificate, it will be used to decrypt the token
                if (tokenDecryptionCertificate != null)
                {
                    options.TokenValidationParameters.TokenDecryptionKey = new X509SecurityKey(tokenDecryptionCertificate);
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

        /// <summary>
        /// Ensure that if the audience is a GUID, api://{audience} is also added
        /// as a valid audience (this is the default App ID URL in the app registration
        /// portal).
        /// </summary>
        /// <param name="options"><see cref="JwtBearerOptions"/> for which to ensure that
        /// api://GUID is a valid audience.</param>
        internal static void EnsureValidAudiencesContainsApiGuidIfGuidProvided(JwtBearerOptions options, MicrosoftIdentityOptions msIdentityOptions)
        {
            options.TokenValidationParameters.ValidAudiences ??= new List<string>();
            var validAudiences = new List<string>(options.TokenValidationParameters.ValidAudiences);
            if (!string.IsNullOrWhiteSpace(options.Audience))
            {
                validAudiences.Add(options.Audience);
                if (!options.Audience.StartsWith("api://", StringComparison.OrdinalIgnoreCase)
                                                 && Guid.TryParse(options.Audience, out _))
                {
                    validAudiences.Add($"api://{options.Audience}");
                }
            }
            else
            {
                validAudiences.Add(msIdentityOptions.ClientId);
                validAudiences.Add($"api://{msIdentityOptions.ClientId}");
            }

            options.TokenValidationParameters.ValidAudiences = validAudiences;
        }
    }
}
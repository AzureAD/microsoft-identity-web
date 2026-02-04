// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#if NET10_0_OR_GREATER

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Web.Internal;
using Microsoft.Identity.Web.Resource;
using Microsoft.IdentityModel.Tokens;
using Microsoft.IdentityModel.Validators;

namespace Microsoft.Identity.Web.PostConfigureOptions
{
    /// <summary>
    /// Post-configures JwtBearerOptions for AOT-compatible path using MicrosoftIdentityApplicationOptions.
    /// Performs validation, configuration, and OBO token storage setup.
    /// </summary>
    internal sealed class MicrosoftIdentityJwtBearerOptionsPostConfigurator : IPostConfigureOptions<JwtBearerOptions>
    {
        private readonly IOptionsMonitor<MicrosoftIdentityApplicationOptions> _appOptionsMonitor;
        private readonly IServiceProvider _serviceProvider;

        public MicrosoftIdentityJwtBearerOptionsPostConfigurator(
            IOptionsMonitor<MicrosoftIdentityApplicationOptions> appOptionsMonitor,
            IServiceProvider serviceProvider)
        {
            _appOptionsMonitor = appOptionsMonitor;
            _serviceProvider = serviceProvider;
        }

        public void PostConfigure(string? name, JwtBearerOptions options)
        {
            var appOptions = _appOptionsMonitor.Get(name ?? string.Empty);

            // Skip if not configured via our AOT path (no ClientId means not configured)
            if (string.IsNullOrEmpty(appOptions.ClientId))
            {
                return;
            }

            // =========================================================
            // 1. VALIDATE (fail-fast with complete configuration)
            // =========================================================
            IdentityOptionsHelpers.ValidateRequiredOptions(appOptions);

            // =========================================================
            // 2. CONFIGURE (respect customer overrides)
            // =========================================================
            
            // Build and set authority if not already configured
            if (string.IsNullOrEmpty(options.Authority))
            {
                // Handle CIAM authorities
                if (!string.IsNullOrEmpty(appOptions.Authority))
                {
                    var authority = AuthorityHelpers.BuildCiamAuthorityIfNeeded(appOptions.Authority, out _);
                    options.Authority = AuthorityHelpers.EnsureAuthorityIsV2(authority ?? appOptions.Authority);
                }
                else
                {
                    options.Authority = AuthorityHelpers.EnsureAuthorityIsV2(IdentityOptionsHelpers.BuildAuthority(appOptions));
                }
            }

            // Configure audience validation if not already set
            if (options.TokenValidationParameters.AudienceValidator == null &&
                options.TokenValidationParameters.ValidAudience == null &&
                options.TokenValidationParameters.ValidAudiences == null)
            {
                IdentityOptionsHelpers.ConfigureAudienceValidation(
                    options.TokenValidationParameters, appOptions);
            }

            // Configure issuer validation if not already set
            if (options.TokenValidationParameters.ValidateIssuer && 
                options.TokenValidationParameters.IssuerValidator == null)
            {
                var microsoftIdentityIssuerValidatorFactory =
                    _serviceProvider.GetRequiredService<MicrosoftIdentityIssuerValidatorFactory>();

                options.TokenValidationParameters.IssuerValidator =
                    microsoftIdentityIssuerValidatorFactory.GetAadIssuerValidator(options.Authority).Validate;
            }

            // Configure token decryption if credentials provided
            if (appOptions.TokenDecryptionCredentials != null && appOptions.TokenDecryptionCredentials.Any())
            {
                // Extract user assigned identity client ID from credentials if present
                var managedIdentityCredential = appOptions.TokenDecryptionCredentials
                    .OfType<CredentialDescription>()
                    .FirstOrDefault(c => !string.IsNullOrEmpty(c.ManagedIdentityClientId));
                
                if (managedIdentityCredential != null)
                {
                    DefaultCertificateLoader.UserAssignedManagedIdentityClientId = managedIdentityCredential.ManagedIdentityClientId;
                }

                IEnumerable<X509Certificate2?> certificates = DefaultCertificateLoader.LoadAllCertificates(
                    appOptions.TokenDecryptionCredentials.OfType<CertificateDescription>());
                IEnumerable<X509SecurityKey> keys = certificates.Select(c => new X509SecurityKey(c));
                options.TokenValidationParameters.TokenDecryptionKeys = keys;
            }

            // Enable AAD signing key issuer validation
            options.TokenValidationParameters.EnableAadSigningKeyIssuerValidation();

            // Ensure events object exists
            if (options.Events == null)
            {
                options.Events = new JwtBearerEvents();
            }

            // Set up configuration manager on message received
            var existingOnMessageReceived = options.Events.OnMessageReceived;
            options.Events.OnMessageReceived = async context =>
            {
                context.Options.TokenValidationParameters.ConfigurationManager ??= 
                    options.ConfigurationManager as BaseConfigurationManager;
                
                if (existingOnMessageReceived != null)
                {
                    await existingOnMessageReceived(context).ConfigureAwait(false);
                }
            };

            // Add claims validation if not allowing ACL authorization
            if (!appOptions.AllowWebApiToBeAuthorizedByACL)
            {
                MicrosoftIdentityWebApiAuthenticationBuilderExtensions.ChainOnTokenValidatedEventForClaimsValidation(
                    options.Events, name ?? JwtBearerDefaults.AuthenticationScheme);
            }

            // =========================================================
            // 3. CHAIN OnTokenValidated (always - required for OBO)
            // =========================================================
            options.Events.OnTokenValidated = IdentityOptionsHelpers.ChainTokenStorageHandler(
                options.Events.OnTokenValidated);
        }
    }
}

#endif

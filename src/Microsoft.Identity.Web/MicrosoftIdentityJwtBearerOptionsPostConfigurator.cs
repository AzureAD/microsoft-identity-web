// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#if NET10_0_OR_GREATER

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Web.Resource;
using Microsoft.IdentityModel.Tokens;
using Microsoft.IdentityModel.Validators;

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// Post-configurator for JwtBearerOptions in AOT scenarios.
    /// Ensures our configuration (including OnTokenValidated for OBO) runs after customer configuration.
    /// </summary>
    internal sealed class MicrosoftIdentityJwtBearerOptionsPostConfigurator : IPostConfigureOptions<JwtBearerOptions>
    {
        private readonly IMergedOptionsStore _mergedOptionsStore;
        private readonly IServiceProvider _serviceProvider;

        public MicrosoftIdentityJwtBearerOptionsPostConfigurator(
            IMergedOptionsStore mergedOptionsStore,
            IServiceProvider serviceProvider)
        {
            _mergedOptionsStore = mergedOptionsStore;
            _serviceProvider = serviceProvider;
        }

        public void PostConfigure(
#if NET7_0_OR_GREATER
            string? name,
#else
            string name,
#endif
            JwtBearerOptions options)
        {
            string schemeName = name ?? string.Empty;
            MergedOptions mergedOptions = _mergedOptionsStore.Get(schemeName);

            // Set identity model logger
            MicrosoftIdentityBaseAuthenticationBuilder.SetIdentityModelLogger(_serviceProvider);

            // Validate the merged options
            MicrosoftIdentityOptionsValidation.Validate(mergedOptions);

            // Process OIDC compliant tenants - handle CIAM authority
            if (mergedOptions.Authority != null)
            {
                mergedOptions.Authority = AuthorityHelpers.GetAuthorityWithoutQueryIfNeeded(mergedOptions);
                mergedOptions.Authority = AuthorityHelpers.BuildCiamAuthorityIfNeeded(mergedOptions.Authority, out bool preserveAuthority);
                mergedOptions.PreserveAuthority = preserveAuthority;
            }

            // Build authority if not explicitly set
            if (string.IsNullOrWhiteSpace(mergedOptions.Authority))
            {
                mergedOptions.Authority = AuthorityHelpers.BuildAuthority(mergedOptions);
            }

            // Ensure authority is v2.0
            mergedOptions.Authority = AuthorityHelpers.EnsureAuthorityIsV2(mergedOptions.Authority);

            // Set authority on options if not already set by customer
            if (string.IsNullOrWhiteSpace(options.Authority))
            {
                options.Authority = mergedOptions.Authority;
            }

            // Configure audience validation if not already configured by customer
            if (options.TokenValidationParameters.AudienceValidator == null
                && options.TokenValidationParameters.ValidAudience == null
                && options.TokenValidationParameters.ValidAudiences == null)
            {
                RegisterValidAudience registerAudience = new RegisterValidAudience();
                registerAudience.RegisterAudienceValidation(
                    options.TokenValidationParameters,
                    mergedOptions);
            }

            // Configure issuer validation if not already configured by customer
            if (options.TokenValidationParameters.ValidateIssuer && options.TokenValidationParameters.IssuerValidator == null)
            {
                MicrosoftIdentityIssuerValidatorFactory microsoftIdentityIssuerValidatorFactory =
                    _serviceProvider.GetRequiredService<MicrosoftIdentityIssuerValidatorFactory>();

                options.TokenValidationParameters.IssuerValidator =
                    microsoftIdentityIssuerValidatorFactory.GetAadIssuerValidator(options.Authority).Validate;
            }

            // Configure token decryption if certificates are provided
            if (mergedOptions.TokenDecryptionCredentials != null)
            {
                DefaultCertificateLoader.UserAssignedManagedIdentityClientId = mergedOptions.UserAssignedManagedIdentityClientId;
                IEnumerable<X509Certificate2?> certificates = DefaultCertificateLoader.LoadAllCertificates(
                    mergedOptions.TokenDecryptionCredentials.OfType<CertificateDescription>());
                IEnumerable<X509SecurityKey> keys = certificates.Select(c => new X509SecurityKey(c));
                options.TokenValidationParameters.TokenDecryptionKeys = keys;
            }

            // Initialize events if not already initialized
            if (options.Events == null)
            {
                options.Events = new JwtBearerEvents();
            }

            // Enable AAD signing key issuer validation
            options.TokenValidationParameters.EnableAadSigningKeyIssuerValidation();

            // Chain OnMessageReceived to set ConfigurationManager
            var existingOnMessageReceived = options.Events.OnMessageReceived;
            options.Events.OnMessageReceived = async context =>
            {
                context.Options.TokenValidationParameters.ConfigurationManager ??= options.ConfigurationManager as BaseConfigurationManager;
                if (existingOnMessageReceived != null)
                {
                    await existingOnMessageReceived(context).ConfigureAwait(false);
                }
                else
                {
                    await Task.CompletedTask.ConfigureAwait(false);
                }
            };

            // Chain OnTokenValidated for OBO token storage and claims validation
            if (!mergedOptions.AllowWebApiToBeAuthorizedByACL)
            {
                ChainOnTokenValidatedForAot(options.Events, schemeName);
            }
        }

        /// <summary>
        /// Chains the OnTokenValidated event to store the token for OBO and validate claims.
        /// </summary>
        private static void ChainOnTokenValidatedForAot(JwtBearerEvents events, string jwtBearerScheme)
        {
            var existingTokenValidatedHandler = events.OnTokenValidated;

            events.OnTokenValidated = async context =>
            {
                // Validate that the token has either scope or role claims
                if (!context!.Principal!.Claims.Any(x => x.Type == ClaimConstants.Scope
                        || x.Type == ClaimConstants.Scp
                        || x.Type == ClaimConstants.Roles
                        || x.Type == ClaimConstants.Role))
                {
                    context.Fail(string.Format(
                        CultureInfo.InvariantCulture,
                        IDWebErrorMessage.NeitherScopeOrRolesClaimFoundInToken,
                        jwtBearerScheme));
                }

                // Store the token for OBO scenarios
                context.HttpContext.StoreTokenUsedToCallWebAPI(
                    context.SecurityToken is System.IdentityModel.Tokens.Jwt.JwtSecurityToken or Microsoft.IdentityModel.JsonWebTokens.JsonWebToken
                        ? context.SecurityToken
                        : null);

                // Call the existing handler if any
                if (existingTokenValidatedHandler != null)
                {
                    await existingTokenValidatedHandler(context).ConfigureAwait(false);
                }
            };
        }
    }
}

#endif

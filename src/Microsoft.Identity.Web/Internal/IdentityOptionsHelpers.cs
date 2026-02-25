// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Identity.Abstractions;
using Microsoft.IdentityModel.Tokens;

#if !NETSTANDARD2_0 && !NET462 && !NET472
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Web.Resource;
#endif

namespace Microsoft.Identity.Web.Internal
{
    /// <summary>
    /// Shared helper methods for identity options configuration and validation.
    /// Used by both traditional (MergedOptions) and AOT-compatible paths.
    /// </summary>
    internal static class IdentityOptionsHelpers
    {
        /// <summary>
        /// Builds the authority URL from the given application options.
        /// Handles AAD, B2C, and CIAM scenarios.
        /// </summary>
        /// <param name="options">The application options containing instance, tenant, and domain information.</param>
        /// <returns>The constructed authority URL.</returns>
        internal static string BuildAuthority(MicrosoftIdentityApplicationOptions options)
        {
            if (string.IsNullOrEmpty(options.Instance))
            {
                throw new ArgumentNullException(nameof(options.Instance));
            }

#if !NETSTANDARD2_0 && !NET462 && !NET472
            Uri baseUri = new Uri(options.Instance);
            var domain = options.Domain;
            var tenantId = options.TenantId;

            // B2C is detected by presence of SignUpSignInPolicyId
            bool isB2C = !string.IsNullOrWhiteSpace(options.SignUpSignInPolicyId);

            if (isB2C)
            {
                var userFlow = options.SignUpSignInPolicyId;
                return new Uri(baseUri, new PathString($"{baseUri.PathAndQuery}{domain}/{userFlow}/v2.0")).ToString();
            }

            return new Uri(baseUri, new PathString($"{baseUri.PathAndQuery}{tenantId}/v2.0")).ToString();
#else
            // For non-ASP.NET Core, use simple string concatenation
            // options.Instance is guaranteed to be non-null because we check it at the start of the method
            var instance = options.Instance!.TrimEnd('/');
            bool isB2C = !string.IsNullOrWhiteSpace(options.SignUpSignInPolicyId);

            if (isB2C)
            {
                return $"{instance}/{options.Domain}/{options.SignUpSignInPolicyId}/v2.0";
            }

            return $"{instance}/{options.TenantId}/v2.0";
#endif
        }


#if !NETSTANDARD2_0 && !NET462 && !NET472
        /// <summary>
        /// Configures issuer validation on the JWT bearer options.
        /// Sets up multi-tenant issuer validation logic that accepts both v1.0 and v2.0 tokens.
        /// If the developer has already registered an IssuerValidator, it will not be overwritten.
        /// </summary>
        /// <param name="options">The JWT bearer options containing token validation parameters and authority.</param>
        /// <param name="serviceProvider">The service provider to resolve the issuer validator factory.</param>
        internal static void ConfigureIssuerValidation(
            JwtBearerOptions options,
            IServiceProvider serviceProvider)
        {
            if (options.TokenValidationParameters.ValidateIssuer &&
                options.TokenValidationParameters.IssuerValidator == null)
            {
                var microsoftIdentityIssuerValidatorFactory =
                    serviceProvider.GetRequiredService<MicrosoftIdentityIssuerValidatorFactory>();

                options.TokenValidationParameters.IssuerValidator =
                    microsoftIdentityIssuerValidatorFactory.GetAadIssuerValidator(options.Authority!).Validate;
            }
        }

        /// <summary>
        /// Ensures the JwtBearerOptions.Events object exists and wires up the
        /// ConfigurationManager on the OnMessageReceived event.
        /// </summary>
        /// <param name="options">The JWT bearer options to configure.</param>
        internal static void InitializeJwtBearerEvents(JwtBearerOptions options)
        {
            if (options.Events == null)
            {
                options.Events = new JwtBearerEvents();
            }

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
        }

        /// <summary>
        /// Configures audience validation on the token validation parameters if not already configured.
        /// Sets up custom validator for handling v1.0/v2.0 and B2C tokens correctly.
        /// This is AOT-compatible as it directly sets up the validator without using reflection or MicrosoftIdentityOptions.
        /// </summary>
        /// <param name="validationParameters">The token validation parameters to configure.</param>
        /// <param name="clientId">The application (client) ID.</param>
        /// <param name="isB2C">Whether the application targets Azure AD B2C.</param>
        internal static void ConfigureAudienceValidation(
            TokenValidationParameters validationParameters,
            string? clientId,
            bool isB2C)
        {
            // Skip if audience validation is already configured by the caller
            if (validationParameters.AudienceValidator != null ||
                validationParameters.ValidAudience != null ||
                validationParameters.ValidAudiences != null)
            {
                return;
            }

            // Set up the audience validator directly without converting to MicrosoftIdentityOptions
            validationParameters.AudienceValidator = (audiences, securityToken, validationParams) =>
            {
                var claims = securityToken switch
                {
                    System.IdentityModel.Tokens.Jwt.JwtSecurityToken jwtSecurityToken => jwtSecurityToken.Claims,
                    Microsoft.IdentityModel.JsonWebTokens.JsonWebToken jwtWebToken => jwtWebToken.Claims,
                    _ => throw new SecurityTokenValidationException(IDWebErrorMessage.TokenIsNotJwtToken),
                };

                validationParams.AudienceValidator = null;

                // Case of a default App ID URI (the developer did not provide explicit valid audience(s))
                if (string.IsNullOrEmpty(validationParams.ValidAudience) &&
                    validationParams.ValidAudiences == null)
                {
                    // handle v2.0 access token or Azure AD B2C tokens (even if v1.0)
                    if (isB2C || claims.Any(c => c.Type == Constants.Version && c.Value == Constants.V2))
                    {
                        validationParams.ValidAudience = $"{clientId}";
                    }
                    // handle v1.0 access token
                    else if (claims.Any(c => c.Type == Constants.Version && c.Value == Constants.V1))
                    {
                        validationParams.ValidAudience = $"api://{clientId}";
                    }
                }

                Validators.ValidateAudience(audiences, securityToken, validationParams);
                return true;
            };
        }

        /// <summary>
        /// Chains a handler onto the OnTokenValidated event to store the token for OBO scenarios.
        /// </summary>
        /// <param name="existingHandler">The existing OnTokenValidated handler, if any.</param>
        /// <returns>A new handler that stores the token and then calls the existing handler.</returns>
        internal static Func<TokenValidatedContext, Task> ChainTokenStorageHandler(Func<TokenValidatedContext, Task>? existingHandler)
        {
            return async context =>
            {
                // Only pass through a token if it is of an expected type
                context.HttpContext.StoreTokenUsedToCallWebAPI(
                    context.SecurityToken is System.IdentityModel.Tokens.Jwt.JwtSecurityToken or
                    Microsoft.IdentityModel.JsonWebTokens.JsonWebToken ? context.SecurityToken : null);

                if (existingHandler != null)
                {
                    await existingHandler(context).ConfigureAwait(false);
                }
            };
        }
#endif
    }
}

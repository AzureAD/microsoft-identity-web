// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.Identity.Abstractions;
using Microsoft.IdentityModel.Tokens;

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
        }

        /// <summary>
        /// Validates that required options are present based on the configuration scenario.
        /// </summary>
        /// <param name="options">The application options to validate.</param>
        /// <exception cref="ArgumentNullException">Thrown when required options are missing.</exception>
        internal static void ValidateRequiredOptions(MicrosoftIdentityApplicationOptions options)
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

                // B2C is detected by presence of SignUpSignInPolicyId
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

        /// <summary>
        /// Configures audience validation on the token validation parameters.
        /// Sets up custom validator for handling v1.0/v2.0 and B2C tokens correctly.
        /// </summary>
        /// <param name="validationParameters">The token validation parameters to configure.</param>
        /// <param name="options">The application options containing client ID and B2C flag.</param>
        internal static void ConfigureAudienceValidation(
            TokenValidationParameters validationParameters, 
            MicrosoftIdentityApplicationOptions options)
        {
            var registerAudience = new Resource.RegisterValidAudience();
            registerAudience.RegisterAudienceValidation(validationParameters, ConvertToMicrosoftIdentityOptions(options));
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

        /// <summary>
        /// Converts MicrosoftIdentityApplicationOptions to MicrosoftIdentityOptions for internal use.
        /// This is needed for compatibility with existing helper classes.
        /// </summary>
        private static MicrosoftIdentityOptions ConvertToMicrosoftIdentityOptions(MicrosoftIdentityApplicationOptions appOptions)
        {
            var options = new MicrosoftIdentityOptions();
            options.ClientId = appOptions.ClientId!;
            options.Instance = appOptions.Instance!;
            options.TenantId = appOptions.TenantId;
            options.Domain = appOptions.Domain;
            options.Authority = appOptions.Authority;
            // Use reflection to set the read-only property, or use init-only syntax
            var userFlow = appOptions.SignUpSignInPolicyId;
            if (!string.IsNullOrEmpty(userFlow))
            {
                var property = typeof(MicrosoftIdentityOptions).GetProperty(nameof(MicrosoftIdentityOptions.DefaultUserFlow));
                if (property != null && property.CanWrite)
                {
                    property.SetValue(options, userFlow);
                }
            }
            return options;
        }
    }
}

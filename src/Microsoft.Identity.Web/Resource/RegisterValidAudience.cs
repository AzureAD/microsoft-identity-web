﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Microsoft.Identity.Web.Resource
{
    /// <summary>
    /// Generic class that registers the token audience from the provided Azure AD authority.
    /// </summary>
    internal class RegisterValidAudience
    {
        private string? ClientId { get; set; } = null;
        private bool IsB2C { get; set; }

        public void RegisterAudienceValidation(
            TokenValidationParameters validationParameters,
            MicrosoftIdentityOptions microsoftIdentityOptions)
        {
            _ = Throws.IfNull(validationParameters);
            _ = Throws.IfNull(microsoftIdentityOptions);

            ClientId = microsoftIdentityOptions.ClientId;
            IsB2C = microsoftIdentityOptions.IsB2C;

            validationParameters.AudienceValidator = ValidateAudience;
        }

        /// <summary>
        /// Default validation of the audience:
        /// - when registering an Azure AD web API in the app registration portal (and adding a scope)
        ///   the default App ID URI generated by the portal is api://{clientID}
        /// - However, the audience (aud) of the token acquired to access this web API is different depending
        ///   on the "accepted access token version" for the web API:
        ///   - if accepted token version is 1.0, the audience provided in the token
        ///     by the Microsoft identity platform (formerly Azure AD v2.0) endpoint is: api://{ClientID}
        ///   - if the accepted token version is 2.0, the audience provided by Azure AD v2.0 in the token
        ///     is {CliendID}
        ///  When getting an access token for an Azure AD B2C web API the audience in the token is
        ///  api://{ClientID}.
        ///
        /// When web API developers don't provide the "Audience" in the configuration, Microsoft.Identity.Web
        /// considers that this is the default App ID URI as explained above. When developer provides the
        /// "Audience" member, it's available in the TokenValidationParameter.ValidAudience.
        /// </summary>
        /// <param name="audiences">Audiences in the security token.</param>
        /// <param name="securityToken">Security token from which to validate the audiences.</param>
        /// <param name="validationParameters">Token validation parameters.</param>
        /// <returns>True if the token is valid; false, otherwise.</returns>
        internal /*for test only*/ bool ValidateAudience(
            IEnumerable<string> audiences,
            SecurityToken securityToken,
            TokenValidationParameters validationParameters)
        {
            var claims = securityToken switch
            {
                JwtSecurityToken jwtSecurityToken => jwtSecurityToken.Claims,
                JsonWebToken jwtWebToken => jwtWebToken.Claims,
                _ => throw new SecurityTokenValidationException(IDWebErrorMessage.TokenIsNotJwtToken),
            };

            validationParameters.AudienceValidator = null;

            // Case of a default App ID URI (the developer did not provide explicit valid audience(s)
            if (string.IsNullOrEmpty(validationParameters.ValidAudience) &&
                validationParameters.ValidAudiences == null)
            {
                // handle v2.0 access token or Azure AD B2C tokens (even if v1.0)
                if (IsB2C || claims.Any(c => c.Type == Constants.Version && c.Value == Constants.V2))
                {
                    validationParameters.ValidAudience = $"{ClientId}";
                }

                // handle v1.0 access token
                else if (claims.Any(c => c.Type == Constants.Version && c.Value == Constants.V1))
                {
                    validationParameters.ValidAudience = $"api://{ClientId}";
                }
            }

            Validators.ValidateAudience(audiences, securityToken, validationParameters);
            return true;
        }
    }
}
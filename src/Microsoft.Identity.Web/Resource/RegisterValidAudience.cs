﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using Microsoft.IdentityModel.Tokens;

namespace Microsoft.Identity.Web.Resource
{
    /// <summary>
    /// Generic class that registers the token audience from the provided Azure AD authority.
    /// </summary>
    internal class RegisterValidAudience
    {
        private string ClientId { get; set; }
        private bool IsB2C { get; set; } = false;
        private const string Version = "ver";
        private const string V1 = "1.0";
        private const string V2 = "2.0";

        public void RegisterAudienceValidation(
            TokenValidationParameters validationParameters,
            MicrosoftIdentityOptions microsoftIdentityOptions)
        {
            if (validationParameters == null)
            {
                throw new ArgumentNullException(nameof(validationParameters));
            }

            ClientId = microsoftIdentityOptions.ClientId;
            IsB2C = microsoftIdentityOptions.IsB2C;

            validationParameters.AudienceValidator = ValidateAudience;
        }

        /// <summary>
        /// Default validation of the audience:
        /// - when registering an Azure AD Web API in the app registration portal (and adding a scope)
        ///   the default App ID URI generated by the portal is api://{clientID}
        /// - However, the audience (aud) of the token acquired to access this Web API is different depending
        ///   on the "accepted access token version" for the Web API:
        ///   - if accepted token version is 1.0, the audience provided in the token
        ///     by the Microsoft identity platform (formerly Azure AD v2.0) endpoint is: api://{ClientID}
        ///   - if the accepted token version is 2.0, the audience provided by Azure AD v2.0 in the token
        ///     is {CliendID}
        ///  When getting an access token for an Azure AD B2C Web API the audience in the token is
        ///  api://{ClientID}.
        ///
        /// When Web API developers don't provide the "Audience" in the configuration, Microsoft.Identity.Web
        /// considers that this is the default App ID URI as explained abovce. When developer provide the
        /// "Audience" member, its available in the TokenValidationParameter.ValidAudience.
        /// </summary>
        /// <param name="audiences">audiences in the security token.</param>
        /// <param name="securityToken">Security token from which to validate the audiences.</param>
        /// <param name="validationParameters">Token validation parameters.</param>
        /// <returns>true is the token is valid, and false, otherwise.</returns>
        internal /*for test only*/ bool ValidateAudience(
            IEnumerable<string> audiences,
            SecurityToken securityToken,
            TokenValidationParameters validationParameters)
        {
            JwtSecurityToken token = securityToken as JwtSecurityToken;
            string validAudience;

            // Case of a default App ID URI (the developer did not provide explicit valid audience(s)
            if (string.IsNullOrEmpty(validationParameters.ValidAudience) &&
                validationParameters.ValidAudiences == null)
            {
                // handle v2.0 access token or Azure AD B2C tokens (even if v1.0)
                if (IsB2C || token.Claims.Any(c => c.Type == Version && c.Value == V2))
                {
                    validAudience = $"{ClientId}";
                    return audiences.Contains(validAudience);
                }

                // handle v1.0 access token
                if (token.Claims.Any(c => c.Type == Version && c.Value == V1))
                {
                    validAudience = $"api://{ClientId}";
                    return audiences.Contains(validAudience);
                }

                throw new SecurityTokenValidationException("Token does not contain a valid audience. ");
            }

            // Cases where developers explicitly provided the valid audiences
            else if (!string.IsNullOrEmpty(validationParameters.ValidAudience))
            {
                return audiences.Contains(validationParameters.ValidAudience);
            }
            else
            {
                return audiences.Intersect(validationParameters.ValidAudiences).Any();
            }
        }
    }
}

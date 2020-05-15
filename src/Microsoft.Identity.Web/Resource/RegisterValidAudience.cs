// Copyright (c) Microsoft Corporation. All rights reserved.
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

        internal /*for test only*/ bool ValidateAudience(
            IEnumerable<string> audiences,
            SecurityToken securityToken,
            TokenValidationParameters validationParameters)
        {
            JwtSecurityToken token = securityToken as JwtSecurityToken;
            string validAudience;

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

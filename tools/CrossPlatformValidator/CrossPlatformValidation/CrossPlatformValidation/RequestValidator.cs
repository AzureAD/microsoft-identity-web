// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#if JWT_TOKEN
using System.IdentityModel.Tokens.Jwt;
#else
using Microsoft.IdentityModel.JsonWebTokens;
#endif
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Microsoft.IdentityModel.Validators;

namespace CrossPlatformValidation
{
    /// <summary>
    /// 
    /// </summary>
    public class RequestValidator
    {
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        ConfigurationManager<OpenIdConnectConfiguration> configurationManager;
        TokenValidationParameters tokenValidationParameters;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
#if JWT_TOKEN
        JwtSecurityTokenHandler jwtSecurityTokenHandler = new();
#else
        JsonWebTokenHandler jsonWebTokenHandler = new();
#endif

        /// <summary>
        /// 
        /// </summary>
        /// <param name="authority"></param>
        /// <param name="audience"></param>
        public void Initialize(string authority, string audience)
        {
            configurationManager = new ConfigurationManager<OpenIdConnectConfiguration>(authority.TrimEnd('/') + "/v2.0/.well-known/openid-configuration", new OpenIdConnectConfigurationRetriever());
            tokenValidationParameters = new TokenValidationParameters();
            tokenValidationParameters.ConfigurationManager = configurationManager;
            tokenValidationParameters.ValidAudience = audience;
            tokenValidationParameters.IssuerValidator = AadIssuerValidator.GetAadIssuerValidator(authority).Validate;
            tokenValidationParameters.EnableAadSigningKeyIssuerValidation();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="authorizationHeader"></param>
        /// <returns></returns>
        public TokenValidationResult Validate(string authorizationHeader)
        {
#if NET472
            var token = authorizationHeader.Replace("Bearer ", string.Empty);
#else
            var token = authorizationHeader.Replace("Bearer ", string.Empty, StringComparison.OrdinalIgnoreCase);
#endif
#if JWT_TOKEN
            return jwtSecurityTokenHandler.ValidateTokenAsync(token, tokenValidationParameters).GetAwaiter().GetResult();
#else
            return jsonWebTokenHandler.ValidateTokenAsync(token, tokenValidationParameters).GetAwaiter().GetResult();
#endif
        }
    }
}

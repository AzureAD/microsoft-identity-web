// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.IdentityModel.JsonWebTokens;
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
        ConfigurationManager<OpenIdConnectConfiguration> configurationManager;
        TokenValidationParameters tokenValidationParameters;
        JsonWebTokenHandler jsonWebTokenHandler = new();

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
        public IDictionary<string, object> Validate(string authorizationHeader)
        {
            var token = authorizationHeader.Replace("Bearer ", string.Empty, StringComparison.OrdinalIgnoreCase);
            var result = jsonWebTokenHandler.ValidateTokenAsync(token, tokenValidationParameters).GetAwaiter().GetResult();
            return result.Claims;
        }
    }
}

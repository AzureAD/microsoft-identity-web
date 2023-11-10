// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Microsoft.IdentityModel.Validators;
using Microsoft.JavaScript.NodeApi;

namespace CrossPlatformValidationJS
{
    [JSExport]
    public class RequestValidator
    {
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        ConfigurationManager<OpenIdConnectConfiguration> configurationManager;
        TokenValidationParameters tokenValidationParameters;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
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
        public TokenValidationResult Validate(string authorizationHeader)
        {
            var token = authorizationHeader.Replace("Bearer ", string.Empty, StringComparison.OrdinalIgnoreCase);

            return jsonWebTokenHandler.ValidateTokenAsync(token, tokenValidationParameters).GetAwaiter().GetResult();
        }
    }
}

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Abstractions;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Microsoft.IdentityModel.Validators;

namespace Microsoft.Identity.Web.CrossPlatform
{
    /// <summary>
    /// 
    /// </summary>
    public class Validator
    {
        private MicrosoftIdentityApplicationOptions? _msIdentityApplicationOptions;
        static ConfigurationManager<OpenIdConnectConfiguration>? s_configurationManager;
        static TokenValidationParameters? s_tokenValidationParameters;
        static readonly JsonWebTokenHandler s_jsonWebTokenHandler = new();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="msIdentityApplicationOptions"></param>
        public Validator(MicrosoftIdentityApplicationOptions msIdentityApplicationOptions)
        {
            _msIdentityApplicationOptions = msIdentityApplicationOptions;
            s_configurationManager = new ConfigurationManager<OpenIdConnectConfiguration>(_msIdentityApplicationOptions.Authority!.TrimEnd('/') + "/.well-known/openid-configuration", new OpenIdConnectConfigurationRetriever());
            s_tokenValidationParameters = new()
            {
                ConfigurationManager = s_configurationManager,
                ValidAudience = _msIdentityApplicationOptions.Audience,
                ValidAudiences = _msIdentityApplicationOptions.Audiences,
                IssuerValidator = AadIssuerValidator.GetAadIssuerValidator(_msIdentityApplicationOptions.Authority).Validate
            };
            s_tokenValidationParameters.EnableAadSigningKeyIssuerValidation();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="validationInput"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public async Task<ValidationOutput> ValidateAsync(ValidationInput validationInput)
        {
            if (validationInput is null)
            {
                throw new ArgumentNullException(nameof(validationInput));
            }

            var token = validationInput.AuthorizationHeader?.Replace("Bearer ", string.Empty, StringComparison.OrdinalIgnoreCase);
            var result = await s_jsonWebTokenHandler.ValidateTokenAsync(token, s_tokenValidationParameters).ConfigureAwait(false);

            if (result.Exception is not null)
            {
                return new ValidationOutput
                {
                    HttpResponseStatusCode = 401,
                    ErrorDescription = result.Exception.Message,
                    WwwAuthenticate = "Bearer error=\"invalid_token\" error_description=\"The access token is not valid.\""
                };
            }

            return new ValidationOutput
            { 
                Claims = result.Claims,
                HttpResponseStatusCode = 200,
            };
        }
    }
}

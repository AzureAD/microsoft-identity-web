// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Microsoft.IdentityModel.Validators;
using System;

namespace Microsoft.JavaScript.NodeApi.Examples;

/// <summary>
/// Example Node API module that exports a simple "hello" method.
/// </summary>
[JSExport]
public static class Validator
{
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    static ConfigurationManager<OpenIdConnectConfiguration> configurationManager;
    static TokenValidationParameters tokenValidationParameters;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    static JsonWebTokenHandler jsonWebTokenHandler = new();

    /// <summary>
    /// 
    /// </summary>
    /// <param name="authority"></param>
    /// <param name="audience"></param>
    public static int Initialize(string authority, string audience)
    {
        configurationManager = new ConfigurationManager<OpenIdConnectConfiguration>(authority.TrimEnd('/') + "/v2.0/.well-known/openid-configuration", new OpenIdConnectConfigurationRetriever());
        tokenValidationParameters = new TokenValidationParameters();
        tokenValidationParameters.ConfigurationManager = configurationManager;
        tokenValidationParameters.ValidAudience = audience;
        tokenValidationParameters.IssuerValidator = AadIssuerValidator.GetAadIssuerValidator(authority).Validate;
        tokenValidationParameters.EnableAadSigningKeyIssuerValidation();
        return 0;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="authorizationHeader"></param>
    /// <returns></returns>
    public static string Validate(string authorizationHeader)
    {
        var token = authorizationHeader.Replace("Bearer ", string.Empty, StringComparison.OrdinalIgnoreCase);
        var result = jsonWebTokenHandler.ValidateTokenAsync(token, tokenValidationParameters).GetAwaiter().GetResult();
        return result.Exception?.Message;
    }
}

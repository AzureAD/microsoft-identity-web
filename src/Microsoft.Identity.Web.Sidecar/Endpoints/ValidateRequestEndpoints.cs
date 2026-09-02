// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Buffers.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Web.Resource;
using Microsoft.Identity.Web.Sidecar.Logging;
using Microsoft.Identity.Web.Sidecar.Models;
using Microsoft.Identity.Web.Sidecar.Pop;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Microsoft.Identity.Web.Sidecar.Endpoints;

public static class ValidateRequestEndpoints
{
    public static void AddValidateRequestEndpoints(this WebApplication app)
    {
        app.MapGet("/Validate", ValidateEndpoint).
            WithName("ValidateAuthorizationHeader").
            RequireAuthorization(PopConstants.ValidatePolicyName).
            ProducesProblem(StatusCodes.Status400BadRequest).
            ProducesProblem(StatusCodes.Status401Unauthorized);
    }

    private static Results<Ok<ValidateAuthorizationHeaderResult>, ProblemHttpResult> ValidateEndpoint(
        [FromServices] ILogger<Program> logger,
        HttpContext httpContext,
        [FromServices] IConfiguration configuration)
    {
        // The PoP handler has already validated the outer SHR signature and the embedded access token,
        // and rejected any non-app-only (delegated/user) token, before stashing the result here.
        //
        // This branch intentionally returns before the AzureAd:Scopes check below. That check
        // (VerifyUserHasAnyAcceptedScope) enforces delegated 'scp' scopes, which app-only tokens do not
        // carry — so unlike the Bearer path below, this path applies no scope gate. The app-only
        // restriction is a token-type admission check, not an authorization decision; authorizing the
        // returned app identity (e.g. by roles/appid) remains the caller's responsibility.
        if (httpContext.Items[PopConstants.ValidatedAccessTokenItemKey] is JsonWebToken popToken)
        {
            return BuildResult(logger, PopConstants.ProtocolName, popToken);
        }

        string scopeRequiredByApi = configuration["AzureAd:Scopes"] ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(scopeRequiredByApi))
        {
            httpContext.VerifyUserHasAnyAcceptedScope(scopeRequiredByApi);
        }

        var token = httpContext.GetTokenUsedToCallWebAPI() as JsonWebToken;

        if (token is null)
        {
            return TypedResults.Problem("No token found", statusCode: StatusCodes.Status400BadRequest);
        }

        return BuildResult(logger, "Bearer", token);
    }

    private static Results<Ok<ValidateAuthorizationHeaderResult>, ProblemHttpResult> BuildResult(
        ILogger logger,
        string protocol,
        JsonWebToken token)
    {
        var decodedBody = Base64Url.DecodeFromChars(token.EncodedPayload);

        JsonNode? jsonDoc;
        try
        {
            jsonDoc = JsonNode.Parse(decodedBody);
        }
        catch (JsonException ex)
        {
            logger.UnableToParseToken(ex);
            return TypedResults.Problem("Invalid JSON in token payload", statusCode: StatusCodes.Status400BadRequest);
        }

        if (jsonDoc is null)
        {
            logger.UnableToParseToken(null);
            return TypedResults.Problem("Failed to decode token claims", statusCode: StatusCodes.Status400BadRequest);
        }

        var result = new ValidateAuthorizationHeaderResult(
            Protocol: protocol,
            Token: token.EncodedToken,
            Claims: jsonDoc
        );

        return TypedResults.Ok(result);
    }
}

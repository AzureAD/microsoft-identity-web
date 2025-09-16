// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Buffers.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.Resource;
using Microsoft.Identity.Web.Sidecar.Models;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Microsoft.Identity.Web.Sidecar.Endpoints;

public static class ValidateRequestEndpoints
{
    public static void AddValidateRequestEndpoints(this WebApplication app)
    {
        app.MapGet("/Validate", ValidateEndpoint).
            WithName("Validate Authorization header").
            RequireAuthorization().
            WithOpenApi().
            ProducesProblem(401);
    }

    private static Results<Ok<ValidateAuthorizationHeaderResult>, ProblemHttpResult> ValidateEndpoint(HttpContext httpContext, IConfiguration configuration)
    {
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

        var decodedBody = Base64Url.DecodeFromChars(token.EncodedPayload);
        var jsonDoc = JsonSerializer.Deserialize<JsonNode>(decodedBody);

        if (jsonDoc is null)
        {
            return TypedResults.Problem("Failed to decode token claims", statusCode: StatusCodes.Status400BadRequest);
        }

        var result = new ValidateAuthorizationHeaderResult(
            Protocol: "Bearer",
            Token: token.EncodedToken,
            Claims: jsonDoc
        );

        return TypedResults.Ok(result);
    }
}

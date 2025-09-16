// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Net.Mime;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Web.Sidecar.Logging;
using Microsoft.Identity.Web.Sidecar.Models;

namespace Microsoft.Identity.Web.Sidecar.Endpoints;

public static class AuthorizationHeaderEndpoint
{
    public static void AddAuthorizationHeaderRequestEndpoints(this WebApplication app)
    {
        app.MapPost("/AuthorizationHeader/{apiName}", AuthorizationHeaderAsync).
            WithName("Authorization header").
            Accepts<DownstreamApiOptions>(true, MediaTypeNames.Application.Json).
            ProducesProblem(StatusCodes.Status400BadRequest).
            ProducesProblem(StatusCodes.Status401Unauthorized);
    }

    private static async Task<Results<Ok<AuthorizationHeaderResult>, ProblemHttpResult>> AuthorizationHeaderAsync(
        HttpContext httpContext,
        [FromRoute] string apiName,
        [AsParameters] AuthorizationHeaderRequest requestParameters,
        [FromServices] IAuthorizationHeaderProvider headerProvider,
        [FromServices] IOptionsMonitor<DownstreamApiOptions> optionsMonitor,
        [FromServices] ILogger<Program> logger)
    {
        DownstreamApiOptions? options = optionsMonitor.Get(apiName);

        if (options is null)
        {
            return TypedResults.Problem(
                detail: $"Not able to resolve '{apiName}'.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        if (requestParameters.OptionsOverride is not null)
        {
            options = DownstreamApiOptionsMerger.MergeOptions(options, requestParameters.OptionsOverride);
        }

        if (options.Scopes is null)
        {
            return TypedResults.Problem(
                detail: $"No scopes found for the API '{apiName}'. 'scopes' needs to be either a single value or a list of values.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        // To override the tenant use DownstreamAPI.AcquireTokenOptions.Tenant
        if (!string.IsNullOrEmpty(requestParameters.AgentIdentity) && !string.IsNullOrEmpty(requestParameters.AgentUsername))
        {
            options.WithAgentUserIdentity(requestParameters.AgentIdentity, requestParameters.AgentUsername);
        }
        else if (!string.IsNullOrEmpty(requestParameters.AgentIdentity))
        {
            options.WithAgentIdentity(requestParameters.AgentIdentity);
        }

        string authorizationHeader;

        try
        {
            authorizationHeader = await headerProvider.CreateAuthorizationHeaderAsync(
                options.Scopes,
                options,
                httpContext.User,
                httpContext.RequestAborted);
        }
        catch (Exception ex)
        {
            logger.AuthorizationHeaderAsyncError(ex);
            return TypedResults.Problem(
                detail: "An unexpected error occurred.",
                statusCode: StatusCodes.Status500InternalServerError);
        }

        return TypedResults.Ok(new AuthorizationHeaderResult(authorizationHeader));
    }
}

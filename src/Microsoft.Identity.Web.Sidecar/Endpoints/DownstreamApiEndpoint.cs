// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Net.Http;
using System.Net.Mime;
using System.Text;
using Azure;
using Azure.Core;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Web.Sidecar.Logging;
using Microsoft.Identity.Web.Sidecar.Models;

namespace Microsoft.Identity.Web.Sidecar.Endpoints;

public static class DownstreamApiEndpoint
{
    public static void AddDownstreamApiRequestEndpoints(this WebApplication app)
    {
        app.MapPost("/DownstreamApi/{apiName}", DownstreamApiAsync).
            WithName("Downstream Api").
            RequireAuthorization().
            Accepts<DownstreamApiOptions>(true, MediaTypeNames.Application.Json).
            ProducesProblem(StatusCodes.Status400BadRequest).
            ProducesProblem(StatusCodes.Status401Unauthorized);
    }

    private static async Task<Results<Ok<DownstreamApiResult>, ProblemHttpResult>> DownstreamApiAsync(
        HttpContext httpContext,
        [FromRoute] string apiName,
        [AsParameters] AuthorizationHeaderRequest requestParameters,
        [FromServices] IDownstreamApi downstreamApi,
        [FromServices] IOptionsMonitor<DownstreamApiOptions> optionsMonitor,
        [FromServices] ILogger<Program> logger,
        CancellationToken cancellationToken)
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
                detail: $"No scopes found for the API '{apiName}' or in optionsOverride. 'scopes' needs to be either a single value or a list of values.",
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

        HttpContent? content = null;

        if (httpContext.Request.HasJsonContentType())
        {
            using var reader = new StreamReader(httpContext.Request.Body, Encoding.UTF8);
            string body = await reader.ReadToEndAsync(cancellationToken);
            content = new StringContent(body, Encoding.UTF8, MediaTypeNames.Application.Json);
        }

        HttpResponseMessage downstreamResult;

        try
        {
            downstreamResult = await downstreamApi.CallApiAsync(
                options,
                httpContext.User,
                content,
                cancellationToken);
        }
        catch (MicrosoftIdentityWebChallengeUserException ex)
        {
            logger.AuthorizationHeaderAsyncError(ex);
            return TypedResults.Problem(
                detail: ex.InnerException?.Message ?? ex.Message,
                statusCode: StatusCodes.Status401Unauthorized);
        }
        catch (Exception ex)
        {
            logger.AuthorizationHeaderAsyncError(ex);
            return TypedResults.Problem(
                detail: "An unexpected error occurred.",
                statusCode: StatusCodes.Status500InternalServerError);
        }

        string? responseContent = null;

        if (downstreamResult.Content.Headers.ContentLength > 0)
        {
            responseContent = await downstreamResult.Content.ReadAsStringAsync(cancellationToken);
        }

        var result = new DownstreamApiResult(
            (int)downstreamResult.StatusCode,
            new Dictionary<string, IEnumerable<string>>(downstreamResult.Content.Headers),
            responseContent);

        return TypedResults.Ok(result);
    }
}

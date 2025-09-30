// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Net.Http.Headers;
using System.Net.Mime;
using System.Text;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Web.Sidecar.Logging;
using Microsoft.Identity.Web.Sidecar.Models;
using Microsoft.OpenApi.Models;

namespace Microsoft.Identity.Web.Sidecar.Endpoints;

public static class DownstreamApiEndpoint
{
    public static void AddDownstreamApiRequestEndpoints(this WebApplication app)
    {
        app.MapPost("/DownstreamApi/{apiName}", DownstreamApiAsync).
            WithName("DownstreamApi").
            RequireAuthorization().
            ProducesProblem(StatusCodes.Status400BadRequest).
            ProducesProblem(StatusCodes.Status401Unauthorized).
            WithSummary("Invoke a configured downstream API through the sidecar using the authenticated identity.").
            WithDescription(
                "Override downstream call options using dotted query parameters prefixed with 'optionsOverride.'. " +
                "Examples:\n" +
                "  ?optionsOverride.Scopes=User.Read\n" +
                "  ?optionsOverride.Scopes=User.Read&optionsOverride.Scopes=Mail.Read\n" +
                "  ?optionsOverride.AcquireTokenOptions.Tenant=GUID\n" +
                "  ?optionsOverride.RequestAppToken=true&optionsOverride.Scopes=https://graph.microsoft.com/.default").
            WithOpenApi(ConfigureOpenAPI);

        app.MapPost("/DownstreamApiUnauthenticated/{apiName}", DownstreamApiAsync).
            WithName("DownstreamApiUnauthenticated").
            AllowAnonymous().
            ProducesProblem(StatusCodes.Status400BadRequest).
            ProducesProblem(StatusCodes.Status401Unauthorized).
            WithSummary("Invoke a configured downstream API through the sidecar using the configured client credentials.").
            WithDescription(
                "Override downstream call options using dotted query parameters prefixed with 'optionsOverride.'. " +
                "Examples:\n" +
                "  ?optionsOverride.Scopes=User.Read\n" +
                "  ?optionsOverride.Scopes=User.Read&optionsOverride.Scopes=Mail.Read\n" +
                "  ?optionsOverride.AcquireTokenOptions.Tenant=GUID\n" +
                "  ?optionsOverride.RequestAppToken=true&optionsOverride.Scopes=https://graph.microsoft.com/.default").
            WithOpenApi(ConfigureOpenAPI);
    }

    private static OpenApiOperation ConfigureOpenAPI(OpenApiOperation operation)
    {
        // Only add once.
        var documented = operation.Extensions.ContainsKey("x-optionsOverride-documented");
        if (!documented)
        {
            OpenApiDescriptions.AddOptionsOverrideParameters(operation);
            operation.Extensions.Add("x-optionsOverride-documented", new OpenApi.Any.OpenApiBoolean(true));
        }
        return operation;
    }

    private static async Task<Results<Ok<DownstreamApiResult>, ProblemHttpResult>> DownstreamApiAsync(
        HttpContext httpContext,
        [FromRoute] string apiName,
        [AsParameters] DownstreamApiRequest requestParameters,
        BindableDownstreamApiOptions? optionsOverride,
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

        if (optionsOverride is not null)
        {
            options = DownstreamApiOptionsMerger.MergeOptions(options, optionsOverride);
        }

        if (options.Scopes is null || !options.Scopes.Any())
        {
            return TypedResults.Problem(
                detail: $"No scopes found for the API '{apiName}' or in optionsOverride. 'scopes' needs to be either a single value or a list of values.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        AgentOverrides.SetOverrides(options, requestParameters.AgentIdentity, requestParameters.AgentUsername);

        HttpContent? content = null;

        if (!string.IsNullOrWhiteSpace(httpContext.Request.ContentType) &&
            MediaTypeHeaderValue.TryParse(httpContext.Request.ContentType, out var mediaType))
        {
            using var reader = new StreamReader(httpContext.Request.Body, Encoding.UTF8);
            string body = await reader.ReadToEndAsync(cancellationToken);
            content = new StringContent(body, Encoding.UTF8, mediaType);
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

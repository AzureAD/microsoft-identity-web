// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Net.Mime;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Client;
using Microsoft.Identity.Web.Sidecar.Logging;
using Microsoft.Identity.Web.Sidecar.Models;
using Microsoft.OpenApi.Models;

namespace Microsoft.Identity.Web.Sidecar.Endpoints;

public static class AuthorizationHeaderEndpoint
{
    public static void AddAuthorizationHeaderRequestEndpoints(this WebApplication app)
    {
        app.MapGet("/AuthorizationHeader/{apiName}", AuthorizationHeaderAsync).
            WithName("AuthorizationHeader").
            RequireAuthorization().
            ProducesProblem(StatusCodes.Status400BadRequest).
            ProducesProblem(StatusCodes.Status401Unauthorized).
            WithSummary("Get an authorization header for a configured downstream API.").
            WithDescription(
                "This endpoint will use the identity of the authenticated request to acquire an authorization header." +
                "Use dotted query parameters prefixed with 'optionsOverride.' to override call settings. " +
                "Examples:\n" +
                "  ?optionsOverride.Scopes=User.Read&optionsOverride.Scopes=Mail.Read\n" +
                "  ?optionsOverride.RequestAppToken=true&optionsOverride.Scopes=https://graph.microsoft.com/.default\n" +
                "  ?optionsOverride.AcquireTokenOptions.Tenant=GUID\n" +
                "Repeat parameters like 'optionsOverride.Scopes' to add multiple scopes.").
            WithOpenApi(ConfigureOpenAPI);

        app.MapGet("/AuthorizationHeaderUnauthenticated/{apiName}", AuthorizationHeaderAsync).
            WithName("AuthorizationHeaderUnauthenticated").
            AllowAnonymous().
            ProducesProblem(StatusCodes.Status400BadRequest).
            ProducesProblem(StatusCodes.Status401Unauthorized).
            WithSummary("Get an authorization header for a configured downstream API using this configured client credentials.").
            WithDescription(
                "This endpoint will use the configured client credentials to acquire an authorization header." +
                "Use dotted query parameters prefixed with 'optionsOverride.' to override call settings. " +
                "Examples:\n" +
                "  ?optionsOverride.Scopes=User.Read&optionsOverride.Scopes=Mail.Read\n" +
                "  ?optionsOverride.RequestAppToken=true&optionsOverride.Scopes=https://graph.microsoft.com/.default\n" +
                "  ?optionsOverride.AcquireTokenOptions.Tenant=GUID\n" +
                "Repeat parameters like 'optionsOverride.Scopes' to add multiple scopes.").
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

    private static async Task<Results<Ok<AuthorizationHeaderResult>, ProblemHttpResult>> AuthorizationHeaderAsync(
        HttpContext httpContext,
        [FromRoute] string apiName,
        [AsParameters] AuthorizationHeaderRequest requestParameters,
        BindableDownstreamApiOptions? optionsOverride,
        [FromServices] IAuthorizationHeaderProvider headerProvider,
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

        if (options.Scopes is null)
        {
            return TypedResults.Problem(
                detail: $"No scopes found for the API '{apiName}' or in optionsOverride. 'scopes' needs to be either a single value or a list of values.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        AgentOverrides.SetOverrides(options, requestParameters.AgentIdentity, requestParameters.AgentUsername);

        string authorizationHeader;

        try
        {
            authorizationHeader = await headerProvider.CreateAuthorizationHeaderAsync(
                options.Scopes,
                options,
                httpContext.User,
                cancellationToken);
        }
        catch (MicrosoftIdentityWebChallengeUserException ex)
        {
            logger.AuthorizationHeaderAsyncError(ex);
            return TypedResults.Problem(
                detail: ex.InnerException?.Message ?? ex.Message,
                statusCode: StatusCodes.Status401Unauthorized);
        }
        catch(MsalServiceException ex)
        {
            logger.AuthorizationHeaderAsyncError(ex);
            return TypedResults.Problem(
                detail: ex.Message,
                statusCode: StatusCodes.Status401Unauthorized);
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

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.ComponentModel;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Client;
using Microsoft.Identity.Web.Sidecar.Configuration;
using Microsoft.Identity.Web.Sidecar.Logging;
using Microsoft.Identity.Web.Sidecar.Models;

namespace Microsoft.Identity.Web.Sidecar.Endpoints;

public static class AuthorizationHeaderEndpoint
{
    internal const string AuthenticatedRouteName = "AuthorizationHeader";
    internal const string UnauthenticatedRouteName = "AuthorizationHeaderUnauthenticated";

    public static void AddAuthorizationHeaderRequestEndpoints(this WebApplication app)
    {
        app.MapGet("/AuthorizationHeader/{apiName}",
                (HttpContext httpContext, [Description("The downstream API to acquire an authorization header for.")][FromRoute] string apiName,
                    [AsParameters] AuthorizationHeaderRequest requestParameters,
                    BindableDownstreamApiOptions optionsOverride,
                    [FromServices] IAuthorizationHeaderProvider headerProvider,
                    [FromServices] IOptionsMonitor<DownstreamApiOptions> optionsMonitor,
                    [FromServices] IOptions<SidecarOptions> sidecarOptions,
                    [FromServices] ILogger<Program> logger,
                    CancellationToken cancellationToken) =>
                AuthorizationHeaderAsync(
                    httpContext, apiName, requestParameters, optionsOverride, headerProvider, optionsMonitor,
                    sidecarOptions.Value.AllowOverrides.GetAuthorizationHeader, AuthenticatedRouteName, logger, cancellationToken)).
            WithName(AuthenticatedRouteName).
            RequireAuthorization().
            ProducesProblem(StatusCodes.Status400BadRequest).
            ProducesProblem(StatusCodes.Status401Unauthorized).
            WithSummary("Get an authorization header for a configured downstream API.").
            WithDescription(
                "This endpoint will use the identity of the authenticated request to acquire an authorization header." +
                "Use dotted query parameters prefixed with 'optionsOverride.' to override call settings with respect to the configuration. " +
                "Whether overrides are honoured is controlled by 'Sidecar:AllowOverrides:GetAuthorizationHeader' (default: true). " +
                "'optionsOverride.BaseUrl' is always ignored. " +
                "Examples:\n" +
                "  ?optionsOverride.Scopes=User.Read&optionsOverride.Scopes=Mail.Read\n" +
                "  ?optionsOverride.RequestAppToken=true&optionsOverride.Scopes=https://graph.microsoft.com/.default\n" +
                "  ?optionsOverride.AcquireTokenOptions.Tenant=GUID\n" +
                "Repeat parameters like 'optionsOverride.Scopes' to add multiple scopes.");

        app.MapGet("/AuthorizationHeaderUnauthenticated/{apiName}",
                (HttpContext httpContext, [Description("The downstream API to acquire an authorization header for.")][FromRoute] string apiName,
                    [AsParameters] AuthorizationHeaderRequest requestParameters,
                    BindableDownstreamApiOptions optionsOverride,
                    [FromServices] IAuthorizationHeaderProvider headerProvider,
                    [FromServices] IOptionsMonitor<DownstreamApiOptions> optionsMonitor,
                    [FromServices] IOptions<SidecarOptions> sidecarOptions,
                    [FromServices] ILogger<Program> logger,
                    CancellationToken cancellationToken) =>
                AuthorizationHeaderAsync(
                    httpContext, apiName, requestParameters, optionsOverride, headerProvider, optionsMonitor,
                    sidecarOptions.Value.AllowOverrides.GetAuthorizationHeaderUnauthenticated, UnauthenticatedRouteName, logger, cancellationToken)).
            WithName(UnauthenticatedRouteName).
            AllowAnonymous().
            ProducesProblem(StatusCodes.Status400BadRequest).
            ProducesProblem(StatusCodes.Status401Unauthorized).
            WithSummary("Get an authorization header for a configured downstream API using this configured client credentials.").
            WithDescription(
                "This endpoint will use the configured client credentials to acquire an authorization header." +
                "Use dotted query parameters prefixed with 'optionsOverride.' to override call settings with respect to the configuration. " +
                "Whether overrides are honoured is controlled by 'Sidecar:AllowOverrides:GetAuthorizationHeaderUnauthenticated' (default: false). " +
                "'optionsOverride.BaseUrl' is always ignored. " +
                "Examples:\n" +
                "  ?optionsOverride.Scopes=User.Read&optionsOverride.Scopes=Mail.Read\n" +
                "  ?optionsOverride.RequestAppToken=true&optionsOverride.Scopes=https://graph.microsoft.com/.default\n" +
                "  ?optionsOverride.AcquireTokenOptions.Tenant=GUID\n" +
                "Repeat parameters like 'optionsOverride.Scopes' to add multiple scopes.");
    }

    private static async Task<Results<Ok<Models.AuthorizationHeaderResult>, ProblemHttpResult>> AuthorizationHeaderAsync(
        HttpContext httpContext,
        string apiName,
        AuthorizationHeaderRequest requestParameters,
        BindableDownstreamApiOptions optionsOverride,
        IAuthorizationHeaderProvider headerProvider,
        IOptionsMonitor<DownstreamApiOptions> optionsMonitor,
        bool allowOverrides,
        string routeName,
        ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        DownstreamApiOptions? options = optionsMonitor.Get(apiName);

        if (options is null)
        {
            return TypedResults.Problem(
                detail: $"Not able to resolve '{apiName}'.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        if (optionsOverride.HasAny)
        {
            if (allowOverrides)
            {
                options = DownstreamApiOptionsMerger.MergeOptions(options, optionsOverride);
            }
            else
            {
                logger.OverridesIgnored(routeName);
            }
        }

        if (options.Scopes is null)
        {
            return TypedResults.Problem(
                detail: $"No scopes found for the API '{apiName}' or in optionsOverride. 'scopes' needs to be either a single value or a list of values.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        AgentOverrides.SetOverrides(options, requestParameters.AgentIdentity, requestParameters.AgentUsername, requestParameters.AgentUserId);

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

        return TypedResults.Ok(new Models.AuthorizationHeaderResult(authorizationHeader));
    }
}

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Web.Sidecar.Models;

namespace Microsoft.Identity.Web.Sidecar.Endpoints;

internal static class DownstreamApiRequestEndpoints
{
    public static void AddDownstreamApiRequestEndpoints(this WebApplication app)
    {
        app.MapPost("/AuthorizationHeader/{apiName}", AuthorizationHeaderAsync).
            WithName("Authorization header").
            RequireAuthorization().
            WithOpenApi().
            ProducesProblem(401);
    }

    private static async Task<Results<Ok<AuthorizationHeaderResult>, ProblemHttpResult>> AuthorizationHeaderAsync(
        HttpContext httpContext,
        [FromRoute] string apiName,
        [FromQuery] string? agentIdentity,
        [FromQuery] string? agentUsername,
        [FromQuery] string? tenant,
        [FromBody] DownstreamApiOptions? optionsOverride,
        [FromServices] IAuthorizationHeaderProvider headerProvider,
        [FromServices] IConfiguration configuration)
    {
        DownstreamApiOptions? options = configuration.GetSection($"DownstreamApi:{apiName}").Get<DownstreamApiOptions>();

        if (options is null)
        {
            return TypedResults.Problem(
                detail: $"Not able to resolve '{apiName}'.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        if (optionsOverride is not null)
        {
            MergeDownstreamApiOptionsOverrides(options, optionsOverride);
        }

        if (options.Scopes is null)
        {
            return TypedResults.Problem(
                detail: $"No scopes found for the API '{apiName}'. 'scopes' needs to be either a single ",
                statusCode: StatusCodes.Status400BadRequest);
        }

        if (!string.IsNullOrEmpty(agentIdentity) && !string.IsNullOrEmpty(agentUsername))
        {
            options.WithAgentUserIdentity(agentIdentity, agentUsername);
        }
        else if (!string.IsNullOrEmpty(agentIdentity))
        {
            options.WithAgentIdentity(agentIdentity);
        }

        if (!string.IsNullOrEmpty(tenant))
        {
            options.AcquireTokenOptions.Tenant = tenant;
        }

        var result = await headerProvider.CreateAuthorizationHeaderAsync(
            options.Scopes,
            options,
            httpContext.User,
            httpContext.RequestAborted);

        return TypedResults.Ok(new AuthorizationHeaderResult(result));
    }

    private static DownstreamApiOptions MergeDownstreamApiOptionsOverrides(DownstreamApiOptions left, DownstreamApiOptions right)
    {
        if (right is null)
        {
            return left;
        }

        var res = left.Clone();

        if (right.Scopes is not null && right.Scopes.Any())
        {
            res.Scopes = right.Scopes;
        }

        if (!string.IsNullOrEmpty(right.AcquireTokenOptions.Tenant))
        {
            res.AcquireTokenOptions.Tenant = right.AcquireTokenOptions.Tenant;
        }

        if (!string.IsNullOrEmpty(right.AcquireTokenOptions.Claims))
        {
            res.AcquireTokenOptions.Claims = right.AcquireTokenOptions.Claims;
        }

        if (!string.IsNullOrEmpty(right.AcquireTokenOptions.AuthenticationOptionsName))
        {
            res.AcquireTokenOptions.AuthenticationOptionsName = right.AcquireTokenOptions.AuthenticationOptionsName;
        }

        if (!string.IsNullOrEmpty(right.AcquireTokenOptions.FmiPath))
        {
            res.AcquireTokenOptions.FmiPath = right.AcquireTokenOptions.FmiPath;
        }

        if (!string.IsNullOrEmpty(right.RelativePath))
        {
            res.RelativePath = right.RelativePath;
        }

        res.AcquireTokenOptions.ForceRefresh = right.AcquireTokenOptions.ForceRefresh;

        if (right.AcquireTokenOptions.ExtraParameters is not null)
        {
            if (res.AcquireTokenOptions.ExtraParameters is null)
            {
                res.AcquireTokenOptions.ExtraParameters = new Dictionary<string, object>();
            }
            foreach (var extraParameter in right.AcquireTokenOptions.ExtraParameters)
            {
                if (!res.AcquireTokenOptions.ExtraParameters.ContainsKey(extraParameter.Key))
                {
                    res.AcquireTokenOptions.ExtraParameters.Add(extraParameter.Key, extraParameter.Value);
                }
            }
        }

        return res;
    }
}

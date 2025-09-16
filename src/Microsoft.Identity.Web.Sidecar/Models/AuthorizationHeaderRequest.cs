// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Abstractions;

namespace Microsoft.Identity.Web.Sidecar.Models;

/// <summary>
/// Represents the inputs to <see cref="AuthorizationHeaderEndpoint"/>
/// </summary>
/// <remarks>
/// Options with <see cref="FromBodyAttribute"/> also need to be added with
/// <see cref="OpenApiRouteHandlerBuilderExtensions.Accepts{TRequest}(RouteHandlerBuilder, bool, string, string[])"/>
/// </remarks>
public readonly struct AuthorizationHeaderRequest
{
    [FromQuery]
    public string? AgentIdentity { get; init; }

    [FromQuery]
    public string? AgentUsername { get; init; }

    [FromBody]
    public DownstreamApiOptions? OptionsOverride { get; init; }
}

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Abstractions;

namespace Microsoft.Identity.Web.Sidecar.Models;

/// <summary>
/// Represents the inputs to the downstream API endpoint.
/// </summary>
public readonly struct DownstreamApiRequest
{
    [FromQuery]
    public string? AgentIdentity { get; init; }

    [FromQuery]
    public string? AgentUsername { get; init; }
}

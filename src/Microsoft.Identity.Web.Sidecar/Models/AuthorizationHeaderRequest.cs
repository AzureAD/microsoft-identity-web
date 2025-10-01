// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;

namespace Microsoft.Identity.Web.Sidecar.Models;

/// <summary>
/// Represents the inputs to <see cref="AuthorizationHeaderEndpoint"/>
/// </summary>
/// <remarks>
public readonly struct AuthorizationHeaderRequest
{
    [FromQuery]
    [Description("The identity of the agent.")]
    public string? AgentIdentity { get; init; }

    [FromQuery]
    [Description("The username of the agent.")]
    public string? AgentUsername { get; init; }
}

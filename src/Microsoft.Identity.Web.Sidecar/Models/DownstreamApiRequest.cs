// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Abstractions;

namespace Microsoft.Identity.Web.Sidecar.Models;

/// <summary>
/// Represents the inputs to the downstream API endpoint.
/// </summary>
public readonly struct DownstreamApiRequest
{
    [FromQuery]
    [Description("The identity of the agent.")]
    public string? AgentIdentity { get; init; }

    [FromQuery]
    [Description("The username (UPN) of the agent.")]
    public string? AgentUsername { get; init; }

    [FromQuery]
    [Description("The ID of the agent (OID).")]
    [StringSyntax(StringSyntaxAttribute.GuidFormat)]
    public string? AgentUserId { get; init; }
}

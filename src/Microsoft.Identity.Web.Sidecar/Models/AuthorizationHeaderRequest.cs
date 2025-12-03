// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
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
    [Description("The username (UPN) of the user agent identity.")]
    public string? AgentUsername { get; init; }

    [FromQuery]
    [Description("The Object ID of the agent (OID).")]
    [StringSyntax(StringSyntaxAttribute.GuidFormat)]
    public string? AgentUserId { get; init; }
}

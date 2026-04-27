// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Identity.Web.Sidecar.Configuration;

/// <summary>
/// Top-level configuration for the sidecar host. Bound from the
/// <c>Sidecar</c> configuration section.
/// </summary>
public class SidecarOptions
{
    /// <summary>
    /// Per-route gating for caller-supplied <c>optionsOverride.*</c> query
    /// parameters. When the corresponding flag is <c>false</c>, any
    /// <c>optionsOverride.*</c> parameters supplied by the caller are
    /// ignored on that route and a warning is logged.
    /// </summary>
    public AllowOverridesOptions AllowOverrides { get; set; } = new();
}

/// <summary>
/// Per-route flags controlling whether the sidecar will honour
/// <c>optionsOverride.*</c> query parameters.
/// </summary>
public class AllowOverridesOptions
{
    /// <summary>
    /// Allow overrides on <c>GET /AuthorizationHeader/{apiName}</c>.
    /// Defaults to <c>true</c>.
    /// </summary>
    public bool GetAuthorizationHeader { get; set; } = true;

    /// <summary>
    /// Allow overrides on <c>GET /AuthorizationHeaderUnauthenticated/{apiName}</c>.
    /// Defaults to <c>false</c>.
    /// </summary>
    public bool GetAuthorizationHeaderUnauthenticated { get; set; }

    /// <summary>
    /// Allow overrides on <c>POST /DownstreamApi/{apiName}</c>.
    /// Defaults to <c>true</c>.
    /// </summary>
    public bool CallDownstreamApi { get; set; } = true;

    /// <summary>
    /// Allow overrides on <c>POST /DownstreamApiUnauthenticated/{apiName}</c>.
    /// Defaults to <c>false</c>.
    /// </summary>
    public bool CallDownstreamApiUnauthenticated { get; set; }
}

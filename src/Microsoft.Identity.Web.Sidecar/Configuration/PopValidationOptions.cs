// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.ObjectModel;
using System.ComponentModel;

namespace Microsoft.Identity.Web.Sidecar.Configuration;

/// <summary>
/// Tunable flags for inbound Signed HTTP Request (SHR) Proof-of-Possession validation, bound
/// from the <c>Sidecar:PopValidation</c> configuration subsection. Every property defaults to the
/// secure Microsoft.IdentityModel <c>SignedHttpRequestValidationParameters</c> default, so when the
/// subsection is absent PoP validation runs with method/URI/path/timestamp binding on, query
/// binding off, unsigned query parameters accepted, and a five-minute lifetime.
/// <para>
/// Only members exposed as simple configuration values are surfaced here. Header (<c>h</c>) and
/// body-hash (<c>b</c>) binding are not configurable: the sidecar receives only the request line
/// (method and URI), so header binding stays off and unsigned headers are accepted.
/// </para>
/// </summary>
public class PopValidationOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether the <c>ts</c> (timestamp) claim is validated - the
    /// replay/freshness guard. Default <c>true</c>; disabling it removes replay protection.
    /// </summary>
    [DefaultValue(true)]
    public bool ValidateTs { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether the <c>m</c> (HTTP method) claim is validated - request
    /// binding. Default <c>true</c>; disabling it lets a captured SHR be replayed against another method.
    /// </summary>
    [DefaultValue(true)]
    public bool ValidateM { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether the <c>u</c> (host + scheme) claim is validated - request
    /// binding. Default <c>true</c>; disabling it lets a captured SHR be replayed against another host.
    /// </summary>
    [DefaultValue(true)]
    public bool ValidateU { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether the <c>p</c> (path) claim is validated - request binding.
    /// Default <c>true</c>; disabling it lets a captured SHR be replayed against another path.
    /// </summary>
    [DefaultValue(true)]
    public bool ValidateP { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether the <c>q</c> (query parameters) claim is validated -
    /// request binding. Default <c>false</c>; enabling it hardens binding but the caller must sign
    /// <c>q</c> or every request fails.
    /// </summary>
    [DefaultValue(false)]
    public bool ValidateQ { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether query parameters not covered by the signature are
    /// accepted. Default <c>true</c>.
    /// </summary>
    [DefaultValue(true)]
    public bool AcceptUnsignedQueryParameters { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether the claims listed in <see cref="ClaimsToValidateWhenPresent"/>
    /// are validated when present (even if their individual flag is off). Default <c>false</c>; while
    /// <c>false</c> the <see cref="ClaimsToValidateWhenPresent"/> list is inert.
    /// </summary>
    [DefaultValue(false)]
    public bool ValidatePresentClaims { get; set; }

    /// <summary>
    /// Gets or sets the signed HTTP request lifetime - the <c>ts</c> clock-skew tolerance. Default
    /// 5 minutes.
    /// </summary>
    public TimeSpan SignedHttpRequestLifetime { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Gets or sets the claims validated only when present, gated by <see cref="ValidatePresentClaims"/>.
    /// Default <c>{ "m", "p" }</c>. Inert unless <see cref="ValidatePresentClaims"/> is <c>true</c>.
    /// </summary>
    public IList<string> ClaimsToValidateWhenPresent { get; set; } = new Collection<string> { "m", "p" };
}

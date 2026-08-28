// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.ObjectModel;
using System.ComponentModel;

namespace Microsoft.Identity.Web.Sidecar.Configuration;

/// <summary>
/// SPIKE (throwaway): operator-tunable flags for inbound Signed HTTP Request (SHR) Proof-of-Possession
/// validation, bound from the <c>Sidecar:PopValidation</c> configuration subsection. Every property
/// defaults to the strict MISE <c>SignedHttpRequestValidationOptions</c> default, so when the subsection
/// is absent the mapped <c>SignedHttpRequestValidationParameters</c> are the current behavior (m/u/p/ts
/// ON; q/h OFF; unsigned headers/query accepted; 5-minute lifetime; nonce unset).
/// <para>
/// Only the config-clean members Wilson exposes as simple values are surfaced here. Deliberately NOT
/// configurable (see the design doc): body-hash <c>b</c> (needs request-body buffering; MISE never
/// surfaces it), jku key resolution (introduces outbound egress; the <c>cnf</c> jwk-vs-jku open item),
/// and all delegate members (nonce/replay/signature/key-resolver) which are code, not configuration.
/// </para>
/// </summary>
public class PopValidationOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether the <c>ts</c> (timestamp) claim is validated. This is the
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
    /// request binding. Default <c>false</c> (matches MISE); enabling it hardens binding but the caller
    /// must sign <c>q</c> or every request fails.
    /// </summary>
    [DefaultValue(false)]
    public bool ValidateQ { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the <c>h</c> (headers) claim is validated - request
    /// binding. Default <c>false</c> (matches MISE); enabling it hardens binding but the caller must sign
    /// <c>h</c> or every request fails.
    /// </summary>
    [DefaultValue(false)]
    public bool ValidateH { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether headers not covered by the signature are accepted.
    /// Default <c>true</c> (matches MISE).
    /// </summary>
    [DefaultValue(true)]
    public bool AcceptUnsignedHeaders { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether query parameters not covered by the signature are
    /// accepted. Default <c>true</c> (matches MISE).
    /// </summary>
    [DefaultValue(true)]
    public bool AcceptUnsignedQueryParameters { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether the claims listed in <see cref="ClaimsToValidateWhenPresent"/>
    /// are validated when present (even if their individual flag is off). Default <c>false</c> (matches
    /// MISE); while <c>false</c> the <see cref="ClaimsToValidateWhenPresent"/> list is inert.
    /// </summary>
    [DefaultValue(false)]
    public bool ValidatePresentClaims { get; set; }

    /// <summary>
    /// Gets or sets the signed HTTP request lifetime - the <c>ts</c> clock-skew tolerance. Default
    /// 5 minutes (matches MISE).
    /// </summary>
    public TimeSpan SignedHttpRequestLifetime { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Gets or sets the claims validated only when present, gated by <see cref="ValidatePresentClaims"/>.
    /// Default <c>{ "m", "p" }</c> (matches MISE). Inert unless <see cref="ValidatePresentClaims"/> is
    /// <c>true</c>.
    /// </summary>
    public IList<string> ClaimsToValidateWhenPresent { get; set; } = new Collection<string> { "m", "p" };
}

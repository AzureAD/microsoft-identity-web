// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Abstractions;

namespace Microsoft.Identity.Web;

/// <summary>
/// The selected blueprint snapshot shared by an agent acquisition and its assertion provider.
/// </summary>
internal sealed class AgentAcquisitionContext
{
    internal AgentAcquisitionContext(
        string? agentClientId,
        MergedOptions blueprintOptions,
        ITokenAcquisition tokenAcquisition,
        string? authenticationScheme)
    {
        AgentClientId = Throws.IfNullOrWhitespace(agentClientId);
        BlueprintOptions = Throws.IfNull(blueprintOptions);
        BlueprintClientId = Throws.IfNullOrWhitespace(blueprintOptions.ClientId).ToLowerInvariant();
        BlueprintTokenAcquirer = new TokenAcquirer(
            tokenAcquisition, authenticationScheme, hasExplicitApplicationOptions: false, agentContext: this);
    }

    internal string AgentClientId { get; }

    internal string BlueprintClientId { get; }

    internal MergedOptions BlueprintOptions { get; }

    internal ITokenAcquirer BlueprintTokenAcquirer { get; }
}

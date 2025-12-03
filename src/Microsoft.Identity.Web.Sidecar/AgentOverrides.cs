// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;
using Microsoft.Identity.Abstractions;

namespace Microsoft.Identity.Web.Sidecar;

public class AgentOverrides
{
    /// <summary>
    /// Applies agent identity overrides to <see cref="DownstreamApiOptions"/>.
    /// Precedence:
    /// 1. If an agent identity and a username (UPN) are provided, use agent user identity (username wins over userId).
    /// 2. Else if an agent identity and a userId (OID) are provided, use agent user identity with the OID.
    /// 3. Else if only an agent identity is provided, use agent identity.
    /// To override the tenant, set options.AcquireTokenOptions.Tenant separately.
    /// </summary>
    /// <param name="options">Downstream API options to mutate.</param>
    /// <param name="agentIdentity">Agent identity (client/application ID) to act as.</param>
    /// <param name="agentUsername">Agent user identity UPN.</param>
    /// <param name="agentUserId">Agent user identity object id (GUID string).</param>
    public static void SetOverrides(
        DownstreamApiOptions options,
        string? agentIdentity,
        string? agentUsername,
        [StringSyntax(StringSyntaxAttribute.GuidFormat)]
        string? agentUserId)
    {
        if (options is null || string.IsNullOrWhiteSpace(agentIdentity))
        {
            return;
        }

        // Username (UPN) takes precedence if both UPN and OID are supplied.
        if (!string.IsNullOrWhiteSpace(agentUsername))
        {
            options.WithAgentUserIdentity(agentIdentity, agentUsername);
        }
        else if (!string.IsNullOrWhiteSpace(agentUserId) &&
            Guid.TryParse(agentUserId, out Guid userGuid))
        {
            options.WithAgentUserIdentity(agentIdentity, userGuid);
        }
        else
        {
            // Fallback to plain agent identity.
            options.WithAgentIdentity(agentIdentity);
        }
    }
}

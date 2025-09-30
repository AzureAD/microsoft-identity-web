// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Extensions.Options;
using Microsoft.Identity.Abstractions;

namespace Microsoft.Identity.Web.Sidecar;

public class AgentOverrides
{
    public static void SetOverrides(DownstreamApiOptions options, string? agentIdentity, string? agentUsername)
    {
        // To override the tenant use DownstreamAPI.AcquireTokenOptions.Tenant
        if (!string.IsNullOrEmpty(agentIdentity) && !string.IsNullOrEmpty(agentUsername))
        {
            options.WithAgentUserIdentity(agentIdentity, agentUsername);
        }
        else if (!string.IsNullOrEmpty(agentIdentity))
        {
            options.WithAgentIdentity(agentIdentity);
        }
    }
}

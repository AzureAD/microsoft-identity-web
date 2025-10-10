// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Abstractions;

namespace Microsoft.Identity.Web.Sidecar;

public static class DownstreamApiOptionsMerger
{
    public static DownstreamApiOptions MergeOptions(DownstreamApiOptions left, DownstreamApiOptions right)
    {
        var res = left.Clone();

        if (right is null)
        {
            return res;
        }

        if (right.Scopes is not null && right.Scopes.Any())
        {
            res.Scopes = right.Scopes;
        }

        if (!string.IsNullOrEmpty(right.AcquireTokenOptions.Tenant))
        {
            res.AcquireTokenOptions.Tenant = right.AcquireTokenOptions.Tenant;
        }

        if (!string.IsNullOrEmpty(right.AcquireTokenOptions.Claims))
        {
            res.AcquireTokenOptions.Claims = right.AcquireTokenOptions.Claims;
        }

        if (!string.IsNullOrEmpty(right.AcquireTokenOptions.AuthenticationOptionsName))
        {
            res.AcquireTokenOptions.AuthenticationOptionsName = right.AcquireTokenOptions.AuthenticationOptionsName;
        }

        if (!string.IsNullOrEmpty(right.AcquireTokenOptions.FmiPath))
        {
            res.AcquireTokenOptions.FmiPath = right.AcquireTokenOptions.FmiPath;
        }

        if (!string.IsNullOrEmpty(right.RelativePath))
        {
            res.RelativePath = right.RelativePath;
        }

        res.AcquireTokenOptions.ForceRefresh = right.AcquireTokenOptions.ForceRefresh;

        if (right.AcquireTokenOptions.ExtraParameters is not null)
        {
            if (res.AcquireTokenOptions.ExtraParameters is null)
            {
                res.AcquireTokenOptions.ExtraParameters = new Dictionary<string, object>();
            }
            foreach (var extraParameter in right.AcquireTokenOptions.ExtraParameters)
            {
                if (!res.AcquireTokenOptions.ExtraParameters.ContainsKey(extraParameter.Key))
                {
                    res.AcquireTokenOptions.ExtraParameters.Add(extraParameter.Key, extraParameter.Value);
                }
            }
        }

        return res;
    }
}

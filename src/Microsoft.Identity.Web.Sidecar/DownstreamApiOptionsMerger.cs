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

        // RequestAppToken determines whether to use client credentials (app token) or user delegation (OBO)
        if (right.RequestAppToken)
        {
            res.RequestAppToken = right.RequestAppToken;
        }

        if (!string.IsNullOrEmpty(right.BaseUrl))
        {
            res.BaseUrl = right.BaseUrl;
        }

        if (!string.IsNullOrEmpty(right.RelativePath))
        {
            res.RelativePath = right.RelativePath;
        }

        if (!string.IsNullOrEmpty(right.HttpMethod))
        {
            res.HttpMethod = right.HttpMethod;
        }

        if (!string.IsNullOrEmpty(right.ContentType))
        {
            res.ContentType = right.ContentType;
        }

        if (!string.IsNullOrEmpty(right.AcceptHeader))
        {
            res.AcceptHeader = right.AcceptHeader;
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

        if (!string.IsNullOrEmpty(right.AcquireTokenOptions.LongRunningWebApiSessionKey))
        {
            res.AcquireTokenOptions.LongRunningWebApiSessionKey = right.AcquireTokenOptions.LongRunningWebApiSessionKey;
        }

        if (!string.IsNullOrEmpty(right.AcquireTokenOptions.PopPublicKey))
        {
            res.AcquireTokenOptions.PopPublicKey = right.AcquireTokenOptions.PopPublicKey;
        }

        if (right.AcquireTokenOptions.CorrelationId != Guid.Empty)
        {
            res.AcquireTokenOptions.CorrelationId = right.AcquireTokenOptions.CorrelationId;
        }

        if (right.AcquireTokenOptions.ManagedIdentity is not null)
        {
            res.AcquireTokenOptions.ManagedIdentity = right.AcquireTokenOptions.ManagedIdentity;
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

        if (right.ExtraHeaderParameters is not null)
        {
            if (res.ExtraHeaderParameters is null)
            {
                res.ExtraHeaderParameters = new Dictionary<string, string>();
            }
            foreach (var extraHeader in right.ExtraHeaderParameters)
            {
                res.ExtraHeaderParameters[extraHeader.Key] = extraHeader.Value;
            }
        }

        if (right.ExtraQueryParameters is not null)
        {
            if (res.ExtraQueryParameters is null)
            {
                res.ExtraQueryParameters = new Dictionary<string, string>();
            }
            foreach (var extraQueryParam in right.ExtraQueryParameters)
            {
                res.ExtraQueryParameters[extraQueryParam.Key] = extraQueryParam.Value;
            }
        }

        return res;
    }
}

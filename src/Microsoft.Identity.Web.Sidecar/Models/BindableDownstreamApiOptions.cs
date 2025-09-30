// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Reflection;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Abstractions;

namespace Microsoft.Identity.Web.Sidecar.Models;

/// <summary>
/// Downstream options that can be bound from query (dotted keys).
/// Supports simple key-value format:
///   OptionsOverride=Scopes=User.Read,Mail.Read&OptionsOverride.AcquireTokenOptions=Tenant=foo.onmicrosoft.com&OptionsOverride=RelativePath=me
/// </summary>
public class BindableDownstreamApiOptions : DownstreamApiOptions, IEndpointParameterMetadataProvider
{
    public BindableDownstreamApiOptions()
    {
    }

    public static ValueTask<BindableDownstreamApiOptions?> BindAsync(HttpContext ctx, ParameterInfo parameter)
    {
        var paramName = parameter.Name ?? "optionsOverride";
        bool hasAny = ctx.Request.Query.Keys.Any(k =>
            k.StartsWith(paramName + ".", StringComparison.OrdinalIgnoreCase));

        if (!hasAny)
        {
            // No override supplied
            return ValueTask.FromResult<BindableDownstreamApiOptions?>(null);
        }

        var query = ctx.Request.Query;
        var result = new BindableDownstreamApiOptions();

        foreach (var key in query.Keys)
        {
            if (!key.StartsWith(paramName + ".", StringComparison.OrdinalIgnoreCase))
                continue;

            var path = key.Substring(paramName.Length + 1); // remove "optionsOverride."
            var values = query[key];

            if (path.Equals("Scopes", StringComparison.OrdinalIgnoreCase))
            {
                List<string> scopes = result.Scopes is null ? [] : [.. result.Scopes];
                foreach (var v in values)
                {
                    if (!string.IsNullOrWhiteSpace(v))
                        scopes.Add(v);
                }
                result.Scopes = scopes;
            }
            else if (path.Equals("RequestAppToken", StringComparison.OrdinalIgnoreCase))
            {
                if (bool.TryParse(values.LastOrDefault(), out var b))
                    result.RequestAppToken = b;
            }
            else if (path.StartsWith("AcquireTokenOptions.", StringComparison.OrdinalIgnoreCase))
            {
                var sub = path.Substring("AcquireTokenOptions.".Length);
                var last = values.LastOrDefault();
                if (string.IsNullOrEmpty(last)) continue;

                switch (sub.ToLowerInvariant())
                {
                    case "tenant":
                        result.AcquireTokenOptions.Tenant = last;
                        break;
                    case "forcerefresh":
                        if (bool.TryParse(last, out var fr))
                            result.AcquireTokenOptions.ForceRefresh = fr;
                        break;
                    case "authenticationoptionsname":
                        result.AcquireTokenOptions.AuthenticationOptionsName = last;
                        break;
                    case "claims":
                        result.AcquireTokenOptions.Claims = last;
                        break;
                    case "CorrelationId" when Guid.TryParse(last, out var corrId):
                        result.AcquireTokenOptions.CorrelationId = corrId;
                        break;
                    case "fmipath":
                        result.AcquireTokenOptions.FmiPath = last;
                        break;
                    case "longrunningwebapisessionkey":
                        result.AcquireTokenOptions.LongRunningWebApiSessionKey = last;
                        break;
                    case "poppublickey":
                        result.AcquireTokenOptions.PopPublicKey = last;
                        break;
                    case "managedidentity.userassignedclientid":
                        result.AcquireTokenOptions.ManagedIdentity.
                        result.AcquireTokenOptions.ManagedIdentity.UserAssignedClientId = last;
                        break;
                }
            }
            else if (path.Equals("BaseUrl", StringComparison.OrdinalIgnoreCase))
            {
                result.BaseUrl = values.LastOrDefault();
            }
            else if (path.Equals("RelativePath", StringComparison.OrdinalIgnoreCase))
            {
                result.RelativePath = values.LastOrDefault() ?? string.Empty;
            }
            else if (path.Equals("HttpMethod", StringComparison.OrdinalIgnoreCase))
            {
                result.HttpMethod = values.LastOrDefault() ?? string.Empty;
            }
            else if (path.Equals("ContentType", StringComparison.OrdinalIgnoreCase))
            {
                result.ContentType = values.LastOrDefault() ?? string.Empty;
            }
            else if (path.Equals("AcceptHeader", StringComparison.OrdinalIgnoreCase))
            {
                result.AcceptHeader = values.LastOrDefault() ?? string.Empty;
            }
        }

        return ValueTask.FromResult<BindableDownstreamApiOptions?>(result);
    }

    // Let ApiExplorer/OpenAPI know this should be treated as coming from query
    public static void PopulateMetadata(ParameterInfo parameter, EndpointBuilder builder)
    {
        builder.Metadata.Add(new FromQueryAttribute());
    }
}

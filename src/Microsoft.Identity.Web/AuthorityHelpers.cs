// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Web;
using Microsoft.AspNetCore.Http;

namespace Microsoft.Identity.Web
{
    internal static class AuthorityHelpers
    {
        internal static string BuildAuthority(MicrosoftIdentityOptions options)
        {
            Uri baseUri = new Uri(options.Instance);
            var domain = options.Domain;
            var tenantId = options.TenantId;

            if (options.IsB2C)
            {
                var userFlow = options.DefaultUserFlow;
                return new Uri(baseUri, new PathString($"{baseUri.PathAndQuery}{domain}/{userFlow}/v2.0")).ToString();
            }

            return new Uri(baseUri, new PathString($"{baseUri.PathAndQuery}{tenantId}/v2.0")).ToString();
        }

        internal static string EnsureAuthorityIsV2(string authority)
        {
            authority = authority.Trim().TrimEnd('/');
            if (!authority.EndsWith("v2.0", StringComparison.Ordinal))
            {
                authority += "/v2.0";
            }

            return authority;
        }

        internal static string? BuildCiamAuthorityIfNeeded(string authority, out bool preserveAuthority)
        {
            if (!string.IsNullOrEmpty(authority) && authority.Contains(Constants.CiamAuthoritySuffix, StringComparison.OrdinalIgnoreCase))
            {
                Uri baseUri = new Uri(authority);
                string host = baseUri.Host;
                if (host.EndsWith(Constants.CiamAuthoritySuffix, StringComparison.OrdinalIgnoreCase)
                    && baseUri.AbsolutePath == "/")
                {
                    preserveAuthority = false;
                    string tenantId = host.Substring(0, host.IndexOf(Constants.CiamAuthoritySuffix, StringComparison.OrdinalIgnoreCase)) + ".onmicrosoft.com";
                    return new Uri(baseUri, new PathString($"{baseUri.PathAndQuery}{tenantId}")).ToString();
                }
            }
            preserveAuthority = true;
            return authority;
        }
         
        internal static void AddAuthorityQueryToOptions(MicrosoftIdentityOptions options)
        {
            if (!string.IsNullOrEmpty(options.Authority))
            {
                int queryIndex = options.Authority.IndexOf('?', StringComparison.Ordinal);
                if (queryIndex > -1)
                {
                    options.ExtraQueryParameters ??= new Dictionary<string, string>();
                    var queryParams = HttpUtility.ParseQueryString(options.Authority[queryIndex..].TrimStart('?'));
                    for (int i = 0; i < queryParams.Count; i++)
                    {
                        var key = queryParams.GetKey(i);
                        var value = queryParams.Get(i);
                        if (key != null && key != null)
#pragma warning disable CS8601 // queryParams is not null. ParseQueryString returns a non-null NameValueCollection with non-null values.
                            options.ExtraQueryParameters[key] = value;
#pragma warning restore CS8601 // queryParams is not null. ParseQueryString returns a non-null NameValueCollection with non-null values.
                    }
                }
            }
        }
    }
}

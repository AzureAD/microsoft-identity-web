// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
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
            var queryParams = options.ExtraQueryParameters == null ? QueryString.Empty : QueryString.Create(options.ExtraQueryParameters);

            if (options.IsB2C)
            {
                var userFlow = options.DefaultUserFlow;
                return new Uri(baseUri, new PathString($"{baseUri.PathAndQuery}{domain}/{userFlow}/v2.0").Add(queryParams)).ToString();
            }

            return new Uri(baseUri, new PathString($"{baseUri.PathAndQuery}{tenantId}/v2.0").Add(queryParams)).ToString();
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
    }
}

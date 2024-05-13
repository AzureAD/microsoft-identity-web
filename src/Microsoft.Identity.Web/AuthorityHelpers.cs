// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
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
            QueryString queryParams = options.ExtraQueryParameters == null ? QueryString.Empty : QueryString.Create(options.ExtraQueryParameters as IEnumerable<KeyValuePair<string, string?>>);

            if (options.IsB2C)
            {
                var userFlow = options.DefaultUserFlow;
                return new Uri(baseUri, new PathString($"{baseUri.PathAndQuery}{domain}/{userFlow}/v2.0").Add(queryParams)).ToString();
            }

            return new Uri(baseUri, new PathString($"{baseUri.PathAndQuery}{tenantId}/v2.0").Add(queryParams)).ToString();
        }

        internal static string EnsureAuthorityIsV2(string authority)
        {
            int index = authority.LastIndexOf("?", StringComparison.Ordinal);
            var authorityWithoutQuery = index > 0 ? authority[..index] : authority;
            authorityWithoutQuery = authorityWithoutQuery.Trim().TrimEnd('/');

            if (!authorityWithoutQuery.EndsWith("v2.0", StringComparison.Ordinal))
                authorityWithoutQuery += "/v2.0";

            var query = index > 0 ? authority[index..] : string.Empty;
            return authorityWithoutQuery + query;
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

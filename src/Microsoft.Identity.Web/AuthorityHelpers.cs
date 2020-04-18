// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Http;
using System;
using System.Globalization;

namespace Microsoft.Identity.Web
{
    internal static class AuthorityHelpers
    {
        internal static bool IsV2Authority(string authority)
        {
            if (string.IsNullOrEmpty(authority))
            {
                return false;
            }

            return authority.EndsWith("/v2.0", StringComparison.InvariantCulture);
        }

        internal static string BuildAuthority(MicrosoftIdentityOptions options)
        {
            if (options == null)
            {
                return null;
            }

            // Cannot build authority without AAD Instance
            if (string.IsNullOrWhiteSpace(options.Instance))
            {
                return null;
            }

            var baseUri = new Uri(options.Instance);
            var pathBase = baseUri.PathAndQuery.TrimEnd('/');
            var domain = options.Domain;
            var tenantId = options.TenantId;

            // If there are user flows, then it must build a B2C authority
            if (!string.IsNullOrWhiteSpace(options.DefaultUserFlow))
            {
                // Cannot build B2C authority without domain
                if (string.IsNullOrWhiteSpace(domain))
                {
                    return null;
                }

                var userFlow = options.DefaultUserFlow;
                return new Uri(baseUri, new PathString($"{pathBase}/{domain}/{userFlow}/v2.0")).ToString();
            }
            else
            {
                // Cannot build AAD authority without tenant id
                if (string.IsNullOrWhiteSpace(tenantId))
                {
                    return null;
                }

                return new Uri(baseUri, new PathString($"{pathBase}/{tenantId}/v2.0")).ToString();
            }
        }

        internal static string EnsureAuthorityIsV2(string authority)
        {
            authority = authority.Trim().TrimEnd('/');
            if (!authority.EndsWith("v2.0"))
            {
                authority += "/v2.0";
            }

            return authority;
        }
    }
}

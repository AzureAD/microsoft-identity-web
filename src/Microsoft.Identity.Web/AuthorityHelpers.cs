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
            var baseUri = new Uri(options.Instance);
            var pathBase = baseUri.PathAndQuery.TrimEnd('/');
            var domain = options.Domain;
            var tenantId = options.TenantId;

            // If there are user flows, then it must build a B2C authority
            if (options.IsB2C)
            {
                var userFlow = options.DefaultUserFlow;
                return new Uri(baseUri, new PathString($"{pathBase}/{domain}/{userFlow}/v2.0")).ToString();
            }
            else
            {
                return new Uri(baseUri, new PathString($"{pathBase}/{tenantId}/v2.0")).ToString();
            }
        }

        internal static string EnsureAuthorityIsV2(string authority)
        {
            authority = authority.Trim().TrimEnd('/');
            if (!authority.EndsWith("v2.0", StringComparison.InvariantCulture))
            {
                authority += "/v2.0";
            }

            return authority;
        }
    }
}

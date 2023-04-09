// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Linq;
using Microsoft.Identity.Abstractions;

namespace Microsoft.Identity.Web
{
    internal class CiamAuthorityHelper
    {
        internal static void BuildCiamAuthorityIfNeeded(MicrosoftIdentityApplicationOptions options)
        {
            const string ciamAuthority = ".ciamlogin.com";
            string? authority = options.Authority;

            // Case where the instance is given with tenant.ciamlogin.com, and not tenant Id
            // We trim the v2.0
            if (authority != null)
            {
                if (authority.EndsWith("//v2.0", StringComparison.OrdinalIgnoreCase))
                {
                    authority = authority.Substring(0, authority.Length - 5);
                }
                else if (authority.EndsWith("/v2.0", StringComparison.OrdinalIgnoreCase))
                {
                    authority = authority.Substring(0, authority.Length - 4);
                }
            }

            if (authority != null
#if NET462 || NET472 || NETSTANDARD2_0
                && authority.Contains(ciamAuthority))
#else
                && authority.Contains(ciamAuthority, StringComparison.OrdinalIgnoreCase))
#endif
            {
                Uri baseUri = new Uri(authority);
                string host = baseUri.Host;
                if (host.EndsWith(ciamAuthority, StringComparison.OrdinalIgnoreCase)
                    && baseUri.AbsolutePath == "/")
                {
                    string tenantId = host.Substring(0, host.IndexOf(ciamAuthority, StringComparison.OrdinalIgnoreCase)) + ".onmicrosoft.com";
                    options.Authority = new Uri(baseUri, $"{baseUri.PathAndQuery}{tenantId}/v2.0").ToString();
                    options.Instance = new Uri(baseUri, $"{baseUri.PathAndQuery}").ToString();
                    options.TenantId = tenantId;
                }
            }
        }
    }
}

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Linq;
using Microsoft.Identity.Abstractions;

namespace Microsoft.Identity.Web
{

    // This class is temporary. Will be removed when the authority supports the right pattern.
    internal class CiamAuthorityHelper
    {
        internal static void BuildCiamAuthorityIfNeeded(MicrosoftIdentityApplicationOptions options)
        {
            string? authority = options.Authority;

            if (authority != null
#if NET462 || NET472 || NETSTANDARD2_0
                && authority.Contains(Constants.CiamAuthoritySuffix))
#else
                && authority.Contains(Constants.CiamAuthoritySuffix, StringComparison.OrdinalIgnoreCase))
#endif
            {
                if (authority.EndsWith("//v2.0", StringComparison.OrdinalIgnoreCase))
                {
                    authority = authority.Substring(0, authority.Length - 5);
                }
                else if (authority.EndsWith("/v2.0", StringComparison.OrdinalIgnoreCase))
                {
                    authority = authority.Substring(0, authority.Length - 4);
                }

                Uri baseUri = new Uri(authority);
                string host = baseUri.Host;
                if (host.EndsWith(Constants.CiamAuthoritySuffix, StringComparison.OrdinalIgnoreCase)
                    && baseUri.AbsolutePath == "/")
                {
                    string tenantId = host.Substring(0, host.IndexOf(Constants.CiamAuthoritySuffix, StringComparison.OrdinalIgnoreCase)) + ".onmicrosoft.com";
                    options.Authority = new Uri(baseUri, $"{baseUri.PathAndQuery}{tenantId}/v2.0").ToString();
                    options.Instance = new Uri(baseUri, $"{baseUri.PathAndQuery}").ToString();
                    options.TenantId = tenantId;
                }
            }
        }
    }
}

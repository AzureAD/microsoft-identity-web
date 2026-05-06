// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// Reserved header names that callers must not provide through
    /// <see cref="Microsoft.Identity.Abstractions.DownstreamApiOptions.ExtraHeaderParameters"/>.
    /// The library either sets these itself, or they have host-level meaning that
    /// should not be controlled by per-request configuration.
    /// </summary>
    internal static class ReservedHeaderNames
    {
        // Exact-match names (case-insensitive).
        private static readonly HashSet<string> s_exactNames = new(StringComparer.OrdinalIgnoreCase)
        {
            "Authorization",
            "Cookie",
            "Host",
            "X-Original-URL",
            "X-MS-CLIENT-PRINCIPAL",
            "X-MS-CLIENT-PRINCIPAL-ID",
            "X-MS-CLIENT-PRINCIPAL-NAME",
            "X-MS-CLIENT-PRINCIPAL-IDP",
        };

        // Prefix-match names (case-insensitive). Any header name starting with one of
        // these prefixes is treated as reserved.
        private static readonly string[] s_prefixes = new[]
        {
            "X-Forwarded-",
            "X-MS-TOKEN-AAD-",
        };

        /// <summary>
        /// Returns <see langword="true"/> when <paramref name="headerName"/> matches any
        /// reserved exact name or reserved prefix.
        /// </summary>
        public static bool IsReserved(string headerName)
        {
            if (string.IsNullOrEmpty(headerName))
            {
                return false;
            }

            if (s_exactNames.Contains(headerName))
            {
                return true;
            }

            for (int i = 0; i < s_prefixes.Length; i++)
            {
                if (headerName.StartsWith(s_prefixes[i], StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
    }
}

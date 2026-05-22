// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

namespace Microsoft.Identity.Web;

/// <summary>
/// Shared redirect-URI sanitization helpers for consistent local-URL validation
/// across login/logout endpoints and authorization attributes.
/// </summary>
internal static class RedirectUriHelper
{
    /// <summary>
    /// Returns <c>true</c> when <paramref name="url"/> is a strictly local path
    /// (starts with a single "/" that is not followed by another "/" or "\")
    /// and does not begin with a percent-encoded slash or backslash sequence.
    /// </summary>
    internal static bool IsLocalUrl(string? url)
    {
        if (string.IsNullOrEmpty(url))
        {
            return false;
        }

        if (HasPercentEncodedSlashPrefix(url!))
        {
            return false;
        }

        // "/foo" is local, but not "//foo" (protocol-relative) and not "/\foo" (slash-backslash).
        if (url![0] == '/')
        {
            return url.Length == 1 || (url[1] != '/' && url[1] != '\\');
        }

        return false;
    }

    /// <summary>
    /// Returns <c>true</c> when <paramref name="path"/> starts with a percent-encoded
    /// forward slash (<c>%2f</c>) or backslash (<c>%5c</c>).
    /// </summary>
    internal static bool HasPercentEncodedSlashPrefix(string path) =>
        path.StartsWith("/%2f", StringComparison.OrdinalIgnoreCase)
        || path.StartsWith("/%5c", StringComparison.OrdinalIgnoreCase);
}

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

        if (HasControlCharacter(url!))
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
    /// Returns <c>true</c> when <paramref name="value"/> contains an ASCII control
    /// character (C0 range <c>U+0000</c>–<c>U+001F</c> or DEL <c>U+007F</c>). Browsers
    /// strip characters such as tab, CR, and LF per the WHATWG URL spec, so a value like
    /// <c>"/\tevil.example"</c> resolves to a protocol-relative URL after stripping and
    /// must not be treated as local.
    /// </summary>
    internal static bool HasControlCharacter(string value)
    {
        for (int i = 0; i < value.Length; i++)
        {
            char c = value[i];
            if (c < '\u0020' || c == '\u007F')
            {
                return true;
            }
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

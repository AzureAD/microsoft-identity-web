// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// Extension methods.
    /// </summary>
    internal static class Extensions
    {
        /// <summary>Determines whether the specified string collection contains any.</summary>
        /// <param name="searchFor">The search for.</param>
        /// <param name="stringCollection">The string collection.</param>
        /// <returns>
        ///   <c>true</c> if the specified string collection contains any; otherwise, <c>false</c>.</returns>
        public static bool ContainsAny(this string searchFor, params string[] stringCollection)
        {
            foreach (string str in stringCollection)
            {
                if (searchFor.Contains(str, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
    }
}

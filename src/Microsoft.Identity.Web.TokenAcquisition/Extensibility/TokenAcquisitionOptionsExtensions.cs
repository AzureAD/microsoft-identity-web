// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;

namespace Microsoft.Identity.Web.Extensibility
{
    /// <summary>
    /// Extension methods for <see cref="TokenAcquisitionOptions"/> in extensibility scenarios.
    /// </summary>
    public static class TokenAcquisitionOptionsExtensions
    {
        /// <summary>
        /// Sets cache partition key-value pairs on the options. When set, the token cache
        /// lookup and storage will include these components, isolating cached tokens from
        /// entries that have different (or no) partition keys.
        /// </summary>
        /// <param name="options">The token acquisition options.</param>
        /// <param name="partitionKeys">The partition key-value pairs.</param>
        /// <returns>The options instance for chaining.</returns>
        public static TokenAcquisitionOptions WithCachePartitionKeys(
            this TokenAcquisitionOptions options,
            IDictionary<string, string> partitionKeys)
        {
            options.CachePartitionKeys = partitionKeys;
            return options;
        }
    }
}

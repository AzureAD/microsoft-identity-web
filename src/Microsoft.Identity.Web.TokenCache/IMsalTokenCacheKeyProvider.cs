// © Microsoft Corporation. All rights reserved.

using Microsoft.Identity.Client;

namespace Microsoft.Identity.Web.TokenCacheProviders
{
    /// <summary>
    /// Constructs a unique key for a token to be stored in cache using the authentication data provided by <see cref="TokenCacheNotificationArgs"/> object.
    /// </summary>
    public interface IMsalTokenCacheKeyProvider
    {
        /// <summary>
        /// Constructs and returns a unique cache key.
        /// </summary>
        /// <param name="tokenCacheNotificationArgs">Instance of <see cref="TokenCacheNotificationArgs"/> type.</param>
        /// <returns>A unique string to be used as the key.</returns>
        string GetKey(TokenCacheNotificationArgs tokenCacheNotificationArgs);
    }
}

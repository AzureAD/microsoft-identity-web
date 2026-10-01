// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading.Tasks;
using Microsoft.Identity.Client;

namespace Microsoft.Identity.Web.TokenCacheProviders
{
    /// <summary>
    /// A no-op <see cref="IMsalTokenCacheProvider"/> registered as a fallback when Microsoft.Identity.Web
    /// automatically redeems the authorization code for sign-in-only scenarios (a complex client credential
    /// is configured together with ResponseType=code, but EnableTokenAcquisitionToCallDownstreamApi() was
    /// never called). It does not hook MSAL's cache serialization callbacks: MSAL keeps the redeemed token
    /// in its own in-memory cache for the lifetime of the confidential client application instance, which is
    /// sufficient since no downstream API call is expected to read the cache back later.
    /// This type is never registered if the application configures its own token cache (e.g. via
    /// AddInMemoryTokenCaches(), AddDistributedTokenCaches(), etc.).
    /// </summary>
    internal sealed class NoopMsalTokenCacheProvider : IMsalTokenCacheProvider
    {
        public Task InitializeAsync(ITokenCache tokenCache)
        {
            Initialize(tokenCache);
            return Task.CompletedTask;
        }

        public void Initialize(ITokenCache tokenCache)
        {
            // Intentionally left blank: not hooking the cache serialization callbacks means
            // MSAL relies on its own default in-memory cache.
        }

        public Task ClearAsync(string homeAccountId)
        {
            return Task.CompletedTask;
        }
    }
}

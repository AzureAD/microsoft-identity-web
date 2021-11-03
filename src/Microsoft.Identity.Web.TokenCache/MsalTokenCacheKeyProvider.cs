using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Identity.Client;
using Microsoft.Identity.Web.TokenCacheProviders;

namespace Microsoft.Identity.Web.TokenCache
{
    internal class MsalTokenCacheKeyProvider : IMsalTokenCacheKeyProvider
    {
        public string GetKey(TokenCacheNotificationArgs tokenCacheNotificationArgs)
        {
            return tokenCacheNotificationArgs.SuggestedCacheKey;
        }
    }
}

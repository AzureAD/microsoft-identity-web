// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.IO;
using System.Security.Cryptography;
using Microsoft.Identity.Client;

namespace Microsoft.Identity.Web.Perf.Client
{
    static class TokenCacheHelper
    {
        /// <summary>
        /// Path to the token cache
        /// </summary>
        public static readonly string s_cacheFilePath = System.Reflection.Assembly.GetExecutingAssembly().Location + ".msalcache.bin3";

        private static readonly object s_fileLock = new object();

        public static void BeforeAccessNotification(TokenCacheNotificationArgs args)
        {
            lock (s_fileLock)
            {
                args.TokenCache.DeserializeMsalV3(File.Exists(s_cacheFilePath)
                        ? File.ReadAllBytes(s_cacheFilePath)
                        : null);
            }
        }

        public static void AfterAccessNotification(TokenCacheNotificationArgs args)
        {
            // if the access operation resulted in a cache update
            if (args.HasStateChanged)
            {
                lock (s_fileLock)
                {
                    // reflect changesgs in the persistent store
                    File.WriteAllBytes(s_cacheFilePath,
                                       args.TokenCache.SerializeMsalV3());
                }
            }
        }

        internal static void EnableSerialization(ITokenCache tokenCache)
        {
            tokenCache.SetBeforeAccess(BeforeAccessNotification);
            tokenCache.SetAfterAccess(AfterAccessNotification);
        }
    }
}

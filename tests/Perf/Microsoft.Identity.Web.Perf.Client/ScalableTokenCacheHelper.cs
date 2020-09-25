// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Identity.Client;

namespace Microsoft.Identity.Web.Perf.Client
{
    static class ScalableTokenCacheHelper
    {
        /// <summary>
        /// Path to the token cache
        /// </summary>
        public static readonly string s_cacheFileFolder = 
            Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location),
            "TokenCaches");
        public static readonly string s_cacheKeysFolder = s_cacheFileFolder + "Keys";

        static ScalableTokenCacheHelper()
        {
            if (!Directory.Exists(s_cacheFileFolder))
            {
                Directory.CreateDirectory(s_cacheFileFolder);
            }
            if (!Directory.Exists(s_cacheKeysFolder))
            {
                Directory.CreateDirectory(s_cacheKeysFolder);
            }
        }

        public static Dictionary<int, string> GetAccountIdsByUserNumber()
        {
            int start = "MIWTestUser".Length;
            Dictionary<int, string> accountIdByUserNumber = new Dictionary<int, string>();

            foreach(string filePath in Directory.EnumerateFiles(s_cacheKeysFolder))
            {
                string fileName = Path.GetFileName(filePath);
                string[] segments = fileName.Split('-');
                string userUpn = segments[0];
                string number = userUpn.Substring(start, userUpn.IndexOf('@')-start);

                accountIdByUserNumber.Add(int.Parse(number), string.Join("-", segments.Skip(1)));
            }
            return accountIdByUserNumber;
        }


        public static void BeforeAccessNotification(TokenCacheNotificationArgs args)
        {
            string cacheFilePath = GetCacheFilePath(args);
            args.TokenCache.DeserializeMsalV3(File.Exists(cacheFilePath)
                    ? File.ReadAllBytes(cacheFilePath)
                    : null);
        }

        private static string GetCacheFilePath(TokenCacheNotificationArgs args)
        {
            string suggestedKey = args.SuggestedCacheKey ?? args.Account.HomeAccountId.Identifier;
            if (suggestedKey == null)
            {
                return null;
            }
            return Path.Combine(s_cacheFileFolder, suggestedKey);
        }

        public static void AfterAccessNotification(TokenCacheNotificationArgs args)
        {
            // if the access operation resulted in a cache update
            if (args.HasStateChanged)
            {
                string cacheFilePath = GetCacheFilePath(args);

                // reflect changesgs in the persistent store
                File.WriteAllBytes(cacheFilePath,
                                       args.TokenCache.SerializeMsalV3());

                WriteKey(args);
            }
        }

        private static void WriteKey(TokenCacheNotificationArgs args)
        {
            if (args.Account != null)
            {
                string keyPath = Path.Combine(s_cacheKeysFolder, 
                    args.Account.Username + "-" + args.Account.HomeAccountId.Identifier);
                if (!File.Exists(keyPath))
                {
                    File.WriteAllText(keyPath, " ");
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

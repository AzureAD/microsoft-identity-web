// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Microsoft.Identity.Client;
using Newtonsoft.Json;

namespace Microsoft.Identity.Web.Perf.Client
{
    /// <summary>
    /// Token cache writing on disk one cache per account
    /// WARNING: this version is not encrypted
    /// </summary>
    static class TokenCacheHelper
    {
        /// <summary>
        /// Path to the token cache
        /// </summary>
        public static readonly string s_cacheFileFolder = 
            Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location),
            "TokenCaches");

        /// <summary>
        /// Path to the mapping between upn and home account identifier
        /// </summary>
        private static string s_cache_filename = s_cacheFileFolder + "\\cache.dat";
        private static string s_cache_filenameKeys = s_cacheFileFolder + "\\keys.dat";

        private static ConcurrentDictionary<string, byte[]> s_tokenCache = new ConcurrentDictionary<string, byte[]>();
        private static ConcurrentDictionary<string, string> s_tokenCacheKeys = new ConcurrentDictionary<string, string>();
        private static string s_emptyContent = " ";

        private static volatile bool s_isPersisting = false;
        private static object s_persistLock = new object();

        internal static void PersistCache()
        {
            if (s_isPersisting)
            {
                return;
            }

            lock (s_persistLock)
            {
                if (s_isPersisting)
                {
                    return;
                }

                s_isPersisting = true;
            }

            try
            {
                if (!Directory.Exists(s_cacheFileFolder))
                {
                    Directory.CreateDirectory(s_cacheFileFolder);
                }

                string content = JsonConvert.SerializeObject(s_tokenCache);
                File.WriteAllText(s_cache_filename, content);

                string contentKeys = JsonConvert.SerializeObject(s_tokenCacheKeys);
                File.WriteAllText(s_cache_filenameKeys, contentKeys);
            }
            finally
            {
                s_isPersisting = false;
            }
        }

        internal static void LoadCache()
        {
            if (!Directory.Exists(s_cacheFileFolder))
            {
                Directory.CreateDirectory(s_cacheFileFolder);
            }

            if (!File.Exists(s_cache_filename) || !File.Exists(s_cache_filenameKeys))
            {
                return;
            }

            string content = File.ReadAllText(s_cache_filename);
            s_tokenCache = JsonConvert.DeserializeObject<ConcurrentDictionary<string, byte[]>>(content);

            string contentKeys = File.ReadAllText(s_cache_filenameKeys);
            s_tokenCacheKeys = JsonConvert.DeserializeObject<ConcurrentDictionary<string, string>>(contentKeys);
        }

        /// <summary>
        /// Creating the folders for the token cache and its key, if needed
        /// </summary>
        static TokenCacheHelper()
        {
        }

        /// <summary>
        /// Gets the mapping between a user number and its own home identifier (tid.oid)
        /// </summary>
        /// <remarks>this is encoded in the file names of the cache key folder</remarks>
        /// <returns></returns>
        public static Dictionary<int, string> GetAccountIdsByUserNumber()
        {
            int start = "MIWTestUser".Length;
            Dictionary<int, string> accountIdByUserNumber = new Dictionary<int, string>();

            foreach(string filePath in s_tokenCacheKeys.Keys)
            {
                string fileName = Path.GetFileName(filePath);
                string[] segments = fileName.Split('-');
                string userUpn = segments[0];
                string number = userUpn.Substring(start, userUpn.IndexOf('@', System.StringComparison.OrdinalIgnoreCase) -start);

                accountIdByUserNumber.Add(int.Parse(number, CultureInfo.InvariantCulture), string.Join("-", segments.Skip(1)));
            }
            return accountIdByUserNumber;
        }


        public static void BeforeAccessNotification(TokenCacheNotificationArgs args)
        {
            string cacheFilePath = GetCacheFilePath(args);
            args.TokenCache.DeserializeMsalV3(GetCacheContent(cacheFilePath));
        }

        private static byte[] GetCacheContent(string cacheFilePath)
        {
            s_tokenCache.TryGetValue(cacheFilePath, out byte[] value);
            return value;
        }

        private static void SetCacheContent(string cacheFilePath, byte[] content)
        {
            if (s_tokenCache.ContainsKey(cacheFilePath))
            {
                if (s_tokenCache[cacheFilePath] != content)
                {
                    s_tokenCache[cacheFilePath] = content;
                }
            }
            else
            {
                s_tokenCache.TryAdd(cacheFilePath, content);
            }
        }

        private static string GetCacheFilePath(TokenCacheNotificationArgs args)
        {
            // TODO
            // Here there is a bug in MSAL that sometimes we have the SuggestedCacheKey which is the
            // home account identifier, but we don't have the Account ?? (in AcquireTokenForUsernamePassword)
            // whereas we have passed-in an account
            string suggestedKey = args.SuggestedCacheKey ?? args.Account.HomeAccountId.Identifier;
            if (suggestedKey == null)
            {
                return null;
            }

            return suggestedKey;
        }

        public static void AfterAccessNotification(TokenCacheNotificationArgs args)
        {
            // if the access operation resulted in a cache update
            if (args.HasStateChanged)
            {
                string cacheFilePath = GetCacheFilePath(args);

                // reflect changesgs in the persistent store
                SetCacheContent(cacheFilePath, args.TokenCache.SerializeMsalV3());

                WriteKey(args);
            }
        }

        /// <summary>
        /// Writes (if not already there) a file which names is the concatenation of the
        /// upn and the home account identifier. This is useful to map a user number to
        /// its home account id
        /// </summary>
        /// <param name="args"></param>
        private static void WriteKey(TokenCacheNotificationArgs args)
        {
            if (args.Account != null)
            {
                string keyPath = args.Account.Username + "-" + args.Account.HomeAccountId.Identifier;

                if (!s_tokenCacheKeys.ContainsKey(keyPath))
                {
                    s_tokenCacheKeys.TryAdd(keyPath, s_emptyContent);
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

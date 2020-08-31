// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Client;

namespace IntegrationTest.ClientBuilder
{
    /// <summary>
    /// MSAL token cache handler.
    /// </summary>
    internal class MsalTokenCacheHandler
    {
        /// <summary>
        /// The token cache key prefix.
        /// </summary>
        private const string TokenCacheKeyPrefix = "MSALCache";

        /// <summary>
        /// App token cache prefix.
        /// </summary>
        private const string AppTokenCachePrefix = "App";

        /// <summary>
        /// User token cache prefix.
        /// </summary>
        private const string UserTokenCachePrefix = "User";

        private readonly IDistributedCache _distributedCache;
        private readonly ILogger<MsalTokenCacheHandler> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="MsalTokenCacheHandler"/> class.
        /// </summary>
        /// <param name="distributedCache">Distributed cache instance.</param>
        /// <param name="dataProtectionProvider">Data protection provider.</param>
        /// <param name="payloadCompressor">Payload compressor.</param>
        /// <param name="telemetryClient">Telemetry client.</param>
        /// <param name="requestContextAccessor">Request context accessor.</param>
        /// <param name="logger">Logger.</param>
        public MsalTokenCacheHandler(
            IDistributedCache distributedCache,
            ILogger<MsalTokenCacheHandler> logger)
        {
            _distributedCache = distributedCache ?? throw new ArgumentNullException(nameof(distributedCache));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Registers the callbacks for MSAL to call into for storing and retrieving data from distributed cache.
        /// </summary>
        /// <param name="tokenCache">MSAL token cache.</param>
        public void RegisterWithMsalClient(ITokenCache tokenCache)
        {
            tokenCache.SetBeforeAccessAsync(BeforeAccessNotificationAsync);
            tokenCache.SetAfterAccessAsync(AfterAccessNotificationAsync);
        }

        /// <summary>
        /// Notification raised before MSAL accesses the cache.
        /// </summary>
        /// <param name="args">The arguments.</param>
        /// <remarks>This is your chance to update the in-memory copy from the cache, if the in-memory version is stale.</remarks>
        private async Task BeforeAccessNotificationAsync(TokenCacheNotificationArgs args)
        {
            string cacheKey = GetCacheKey(args);

            byte[] cacheValue = default;
            try
            {
                cacheValue = await _distributedCache.GetAsync(cacheKey).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Hit while getting data from distributed cache, cacheKey: {0}", cacheKey);
                return;
            }

            args.TokenCache.DeserializeMsalV3(cacheValue, shouldClearExistingCache: true);
        }

        /// <summary>
        /// Notification raised after ADAL accessed the cache.
        /// </summary>
        /// <param name="args">The arguments.</param>
        /// <remarks> If the HasStateChanged flag is set, MSAL changed the content of the cache. </remarks>
        private async Task AfterAccessNotificationAsync(TokenCacheNotificationArgs args)
        {
            string cacheKey = GetCacheKey(args);

            if (args.HasStateChanged)
            {
                if (!args.HasTokens)
                {
                    await _distributedCache.RemoveAsync(cacheKey).ConfigureAwait(false);
                }

                // if state changed store the new data into distributed cache.
                byte[] serializedBytes = args.TokenCache.SerializeMsalV3();

                try
                {
                    await _distributedCache
                        .SetAsync(cacheKey, serializedBytes, new DistributedCacheEntryOptions { SlidingExpiration = TimeSpan.FromDays(15) })
                        .ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Hit while storing data in distributed cache, cacheKey: {0}", cacheKey);
                }
                finally
                {
                }
            }
        }

        /// <summary>
        /// Gets the cache for a token MSAL token cache notification.
        /// </summary>
        /// <param name="args">MSAL token cache notification arguments.</param>
        /// <returns>Cache key to store or retrieve token cache against.</returns>
        private string GetCacheKey(TokenCacheNotificationArgs args)
        {
            if (args.IsApplicationCache)
            {
                return $"{TokenCacheKeyPrefix}-{AppTokenCachePrefix}-{args.SuggestedCacheKey}";
            }
            else
            {
                if (string.IsNullOrEmpty(args.Account?.HomeAccountId?.Identifier))
                {
                    return $"{TokenCacheKeyPrefix}-{UserTokenCachePrefix}-{args.SuggestedCacheKey}";
                }

                return $"{TokenCacheKeyPrefix}-{UserTokenCachePrefix}-{args.Account.HomeAccountId.Identifier}";
            }
        }
    }
}

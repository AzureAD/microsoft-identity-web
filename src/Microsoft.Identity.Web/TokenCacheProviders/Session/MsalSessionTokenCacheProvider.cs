// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.Identity.Web.TokenCacheProviders.Session
{
    /// <summary>
    /// An implementation of token cache for Confidential clients backed by an Http session.
    /// </summary>
    /// For this session cache to work effectively the aspnetcore session has to be configured properly.
    /// The latest guidance is provided at https://docs.microsoft.com/aspnet/core/fundamentals/app-state
    ///
    /// // In the method - public void ConfigureServices(IServiceCollection services) in startup.cs, add the following
    /// services.AddSession(option =>
    /// {
    /// option.Cookie.IsEssential = true;
    /// });
    ///
    /// In the method - public void Configure(IApplicationBuilder app, IHostingEnvironment env) in startup.cs, add the following
    ///
    /// app.UseSession(); // Before UseMvc()
    ///
    /// <seealso cref="https://aka.ms/msal-net-token-cache-serialization"/>
    public class MsalSessionTokenCacheProvider : MsalAbstractTokenCacheProvider, IMsalTokenCacheProvider
    {
        public MsalSessionTokenCacheProvider(
            IOptions<MicrosoftIdentityOptions> microsoftIdentityOptions,
            IHttpContextAccessor httpContextAccessor,
            ILogger<MsalSessionTokenCacheProvider> logger)
            : base(microsoftIdentityOptions, httpContextAccessor)
        {
            _logger = logger;
        }

        private HttpContext CurrentHttpContext => _httpContextAccessor.HttpContext;

        private ILogger _logger;
        private static readonly ReaderWriterLockSlim s_sessionLock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);

        protected override async Task<byte[]> ReadCacheBytesAsync(string cacheKey)
        {
            await CurrentHttpContext.Session.LoadAsync().ConfigureAwait(false);

            s_sessionLock.EnterReadLock();
            try
            {
                if (CurrentHttpContext.Session.TryGetValue(cacheKey, out byte[] blob))
                {
                    _logger.LogInformation($"Deserializing session {CurrentHttpContext.Session.Id}, cacheId {cacheKey}");
                }
                else
                {
                    _logger.LogInformation($"CacheId {cacheKey} not found in session {CurrentHttpContext.Session.Id}");
                }

                return blob;
            }
            finally
            {
                s_sessionLock.ExitReadLock();
            }
        }

        protected override async Task WriteCacheBytesAsync(string cacheKey, byte[] bytes)
        {
            s_sessionLock.EnterWriteLock();
            try
            {
                _logger.LogInformation($"Serializing session {CurrentHttpContext.Session.Id}, cacheId {cacheKey}");

                // Reflect changes in the persistent store
                CurrentHttpContext.Session.Set(cacheKey, bytes);
                await CurrentHttpContext.Session.CommitAsync().ConfigureAwait(false);
            }
            finally
            {
                s_sessionLock.ExitWriteLock();
            }
        }

        protected override async Task RemoveKeyAsync(string cacheKey)
        {
            s_sessionLock.EnterWriteLock();
            try
            {
                _logger.LogInformation($"Clearing session {CurrentHttpContext.Session.Id}, cacheId {cacheKey}");

                // Reflect changes in the persistent store
                CurrentHttpContext.Session.Remove(cacheKey);
                await CurrentHttpContext.Session.CommitAsync().ConfigureAwait(false);
            }
            finally
            {
                s_sessionLock.ExitWriteLock();
            }
        }
    }
}

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT-License.

using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Microsoft.Identity.Web.TokenCacheProviders.Session
{
    /// <summary>
    /// An implementation of token cache for confidential clients backed by an HTTP session.
    /// </summary>
    /// <remarks>
    /// For this session cache to work effectively, the ASP.NET Core session has to be configured properly.
    /// The latest guidance is provided at https://docs.microsoft.com/aspnet/core/fundamentals/app-state
    ///
    /// In the method <c>public void ConfigureServices(IServiceCollection services)</c> in Startup.cs, add the following:
    /// <code>
    /// services.AddSession(option =>
    /// {
    ///     option.Cookie.IsEssential = true;
    /// });
    /// </code>
    /// In the method <c>public void Configure(IApplicationBuilder app, IHostingEnvironment env)</c> in Startup.cs, add the following:
    /// <code>
    /// app.UseSession(); // Before UseMvc()
    /// </code>
    /// </remarks>
    /// <seealso>https://aka.ms/msal-net-token-cache-serialization</seealso>
    public class MsalSessionTokenCacheProvider : MsalAbstractTokenCacheProvider
    {
        private readonly ILogger _logger;
        private readonly ISession _session;

        /// <summary>
        /// MSAL Token cache provider constructor.
        /// </summary>
        /// <param name="session">Session for the current user.</param>
        /// <param name="logger">Logger.</param>
        public MsalSessionTokenCacheProvider(
            ISession session,
            ILogger<MsalSessionTokenCacheProvider> logger)
        {
            _session = session;
            _logger = logger;
        }

        /// <summary>
        /// Read a blob representing the token cache from its key.
        /// </summary>
        /// <param name="cacheKey">Key representing the token cache
        /// (account or app).</param>
        /// <returns>Read blob.</returns>
        protected override async Task<byte[]> ReadCacheBytesAsync(string cacheKey)
        {
            await _session.LoadAsync().ConfigureAwait(false);

            _sessionLock.EnterReadLock();
            try
            {
                if (_session.TryGetValue(cacheKey, out byte[] blob))
                {
                    _logger.LogInformation(string.Format(CultureInfo.InvariantCulture, LogMessages.DeserializingSessionCache, _session.Id, cacheKey));
                }
                else
                {
                    _logger.LogInformation(string.Format(CultureInfo.InvariantCulture, LogMessages.SessionCacheKeyNotFound, cacheKey, _session.Id));
                }

                return blob;
            }
            finally
            {
                _sessionLock.ExitReadLock();
            }
        }

        /// <summary>
        /// Writes the token cache identified by its key to the serialization mechanism.
        /// </summary>
        /// <param name="cacheKey">Key for the cache (account ID or app ID).</param>
        /// <param name="bytes">Blob to write to the cache.</param>
        /// <returns>A <see cref="Task"/> that completes when a write operation has completed.</returns>
        protected override async Task WriteCacheBytesAsync(string cacheKey, byte[] bytes)
        {
            _sessionLock.EnterWriteLock();
            try
            {
                _logger.LogInformation(string.Format(CultureInfo.InvariantCulture, LogMessages.SerializingSessionCache, _session.Id, cacheKey));

                // Reflect changes in the persistent store
                _session.Set(cacheKey, bytes);
                await Task.CompletedTask.ConfigureAwait(false);
            }
            finally
            {
                _sessionLock.ExitWriteLock();
            }
        }

        /// <summary>
        /// Removes a cache described by its key.
        /// </summary>
        /// <param name="cacheKey">Key of the token cache (user account or app ID).</param>
        /// <returns>A <see cref="Task"/> that completes when key removal has completed.</returns>
        protected override async Task RemoveKeyAsync(string cacheKey)
        {
            _sessionLock.EnterWriteLock();
            try
            {
                _logger.LogInformation(string.Format(CultureInfo.InvariantCulture, LogMessages.ClearingSessionCache, _session.Id, cacheKey));

                // Reflect changes in the persistent store
                _session.Remove(cacheKey);
                await Task.CompletedTask.ConfigureAwait(false);
            }
            finally
            {
                _sessionLock.ExitWriteLock();
            }
        }

        private readonly ReaderWriterLockSlim _sessionLock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);
    }
}

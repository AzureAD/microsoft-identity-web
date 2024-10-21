// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
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
    /// The latest guidance is provided at https://learn.microsoft.com/aspnet/core/fundamentals/app-state.
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
    public partial class MsalSessionTokenCacheProvider : MsalAbstractTokenCacheProvider, IDisposable
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
            : base(null, logger)
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
        protected override async Task<byte[]?> ReadCacheBytesAsync(string cacheKey)
        {
            return await ReadCacheBytesAsync(cacheKey, new CacheSerializerHints()).ConfigureAwait(false);
        }

        /// <summary>
        /// Read a blob representing the token cache from its key.
        /// </summary>
        /// <param name="cacheKey">Key representing the token cache
        /// (account or app).</param>
        /// <param name="cacheSerializerHints">Hints for the cache serialization implementation optimization.</param>
        /// <returns>Read blob.</returns>
        protected override async Task<byte[]?> ReadCacheBytesAsync(string cacheKey, CacheSerializerHints cacheSerializerHints)
        {
#pragma warning disable CA1062 // Validate arguments of public methods
            await _session.LoadAsync(cacheSerializerHints.CancellationToken).ConfigureAwait(false);
#pragma warning restore CA1062 // Validate arguments of public methods

            _sessionLock.EnterReadLock();
            try
            {
                if (_session.TryGetValue(cacheKey, out byte[]? blob))
                {
                    Logger.SessionCache(_logger, "Read", _session.Id, cacheKey, null);
                }
                else
                {
                    Logger.SessionCacheKeyNotFound(_logger, cacheKey, _session.Id, null);
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
                Logger.SessionCache(_logger, "Write", _session.Id, cacheKey, null);

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
                Logger.SessionCache(_logger, "Remove", _session.Id, cacheKey, null);

                // Reflect changes in the persistent store
                _session.Remove(cacheKey);
                await Task.CompletedTask.ConfigureAwait(false);
            }
            finally
            {
                _sessionLock.ExitWriteLock();
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            _sessionLock.Dispose();
        }

        private readonly ReaderWriterLockSlim _sessionLock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);
    }
}

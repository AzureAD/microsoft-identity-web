// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Owin.Security.Jwt;

namespace Microsoft.Identity.Web
{
    // This class is necessary because the OAuthBearer Middleware does not leverage
    // the OpenID Connect metadata endpoint exposed by the STS by default.
    internal class OpenIdConnectCachingSecurityTokenProvider : IIssuerSecurityKeyProvider
    {
        private readonly TimeProvider _timeProvider;
        public ConfigurationManager<OpenIdConnectConfiguration> _configManager;
        private string? _issuer;
        private ICollection<SecurityKey>? _keys;
        private DateTimeOffset _syncAfter;

        private readonly ReaderWriterLockSlim _syncLock = new();

        private const int IdleState = 0;
        private const int RunningState = 1;
        private int _state = IdleState;

        public OpenIdConnectCachingSecurityTokenProvider(string metadataEndpoint)
            : this(new ConfigurationManager<OpenIdConnectConfiguration>(metadataEndpoint, new OpenIdConnectConfigurationRetriever()), TimeProvider.System) { }

        public OpenIdConnectCachingSecurityTokenProvider(ConfigurationManager<OpenIdConnectConfiguration> configManager, TimeProvider timeProvider)
        {
            _timeProvider = timeProvider;
            _configManager = configManager;

            RetrieveValues();
        }

        /// <summary>
        /// Gets the issuer the credentials are for.
        /// </summary>
        /// <value>
        /// The issuer the credentials are for.
        /// </value>
        public string? Issuer => RetrieveValues().Issuer;

        /// <summary>
        /// Gets all known security keys.
        /// </summary>
        /// <value>
        /// All known security keys.
        /// </value>
        public IEnumerable<SecurityKey>? SecurityKeys => RetrieveValues().Keys;

        private (string? Issuer, ICollection<SecurityKey>? Keys) RetrieveValues()
        {
            _syncLock.EnterReadLock();
            string? issuer = _issuer;
            ICollection<SecurityKey>? keys = _keys;
            DateTimeOffset syncAfter = _syncAfter;
            _syncLock.ExitReadLock();

            // Check if it's time to refresh the stored issuer and keys
            if (syncAfter < _timeProvider.GetUtcNow())
            {
                // Acquire lock to retrieve new metadata
                if (Interlocked.CompareExchange(ref _state, RunningState, IdleState) == IdleState)
                {
                    try
                    {
#pragma warning disable VSTHRD002 // Avoid problematic synchronous waits
                        OpenIdConnectConfiguration config = Task.Run(_configManager.GetConfigurationAsync).Result;
#pragma warning restore VSTHRD002 // Avoid problematic synchronous waits
                        // Acquire write lock to update stored values
                        _syncLock.EnterWriteLock();
                        issuer = _issuer = config.Issuer;
                        keys = _keys = config.SigningKeys;
                        // Metadata will refresh after AutomaticRefreshInterval + jitter, so refresh 1 hour after to account for jitter
                        _syncAfter = _timeProvider.GetUtcNow() + _configManager.AutomaticRefreshInterval + TimeSpan.FromHours(1);
                        _syncLock.ExitWriteLock();
                    }
                    finally
                    {
                        Interlocked.Exchange(ref _state, IdleState);
                    }
                }
            }

            return (issuer, keys);
        }
    }
}

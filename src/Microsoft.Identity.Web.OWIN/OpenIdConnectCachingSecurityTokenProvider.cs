// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Owin.Security.Jwt;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Identity.Web
{
    // This class is necessary because the OAuthBearer Middleware does not leverage
    // the OpenID Connect metadata endpoint exposed by the STS by default.
    internal class OpenIdConnectCachingSecurityTokenProvider : IIssuerSecurityKeyProvider
    {
        public ConfigurationManager<OpenIdConnectConfiguration> _configManager;
        private string? _issuer;
        private IEnumerable<SecurityKey>? _keys;
        private readonly string _metadataEndpoint;

        private readonly ReaderWriterLockSlim _synclock = new ReaderWriterLockSlim();

        public OpenIdConnectCachingSecurityTokenProvider(string metadataEndpoint)
        {
            _metadataEndpoint = metadataEndpoint;
            _configManager = new ConfigurationManager<OpenIdConnectConfiguration>(metadataEndpoint, new OpenIdConnectConfigurationRetriever());

            _ = RetrieveMetadataAsync();
        }

        /// <summary>
        /// Gets the issuer the credentials are for.
        /// </summary>
        /// <value>
        /// The issuer the credentials are for.
        /// </value>
        public string? Issuer
        {
            get
            {
                _ = RetrieveMetadataAsync();
                _synclock.EnterReadLock();
                try
                {
                    return _issuer;
                }
                finally
                {
                    _synclock.ExitReadLock();
                }
            }
        }

        /// <summary>
        /// Gets all known security keys.
        /// </summary>
        /// <value>
        /// All known security keys.
        /// </value>
        public IEnumerable<SecurityKey>? SecurityKeys
        {
            get
            {
                _ = RetrieveMetadataAsync();
                _synclock.EnterReadLock();
                try
                {
                    return _keys;
                }
                finally
                {
                    _synclock.ExitReadLock();
                }
            }
        }

        private async Task RetrieveMetadataAsync()
        {
            _synclock.EnterWriteLock();
            try
            {
                OpenIdConnectConfiguration config = await Task.Run(_configManager.GetConfigurationAsync);
                _issuer = config.Issuer;
                _keys = config.SigningKeys;
            }
            finally
            {
                _synclock.ExitWriteLock();
            }
        }
    }
}

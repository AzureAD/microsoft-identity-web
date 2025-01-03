// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

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
        private int _lock = 0;

        public ConfigurationManager<OpenIdConnectConfiguration> _configManager;
        private string? _issuer;
        private IEnumerable<SecurityKey>? _keys;

        public OpenIdConnectCachingSecurityTokenProvider(string metadataEndpoint)
        {
            _configManager = new ConfigurationManager<OpenIdConnectConfiguration>(metadataEndpoint, new OpenIdConnectConfigurationRetriever());

            RetrieveMetadata();
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
                RetrieveMetadata();
                return _issuer;
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
                RetrieveMetadata();
                return _keys;
            }
        }

        private void RetrieveMetadata()
        {
            // Try to acquire the lock
            //
            // Interlocked.Exchange returns the original value of _lock before it was swapped.
            // If it's 0, it means it went from 0 to 1, so we did acquire the lock.
            // If it's 1, then the lock was already acquired by another thread.
            //
            // See the example in the Exchange(Int32, Int32) overload: https://learn.microsoft.com/en-us/dotnet/api/system.threading.interlocked.exchange?view=netframework-4.7.2
            if (Interlocked.Exchange(ref _lock, 1) == 0)
            {
#pragma warning disable VSTHRD002 // Avoid problematic synchronous waits
                OpenIdConnectConfiguration config = Task.Run(_configManager.GetConfigurationAsync).Result;
#pragma warning restore VSTHRD002 // Avoid problematic synchronous waits
                _issuer = config.Issuer;
                _keys = config.SigningKeys;

                // Release the lock
                Interlocked.Exchange(ref _lock, 0);
            }
        }
    }
}

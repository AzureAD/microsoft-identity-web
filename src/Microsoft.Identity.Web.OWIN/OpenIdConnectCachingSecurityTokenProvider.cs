// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
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
        public readonly ConfigurationManager<OpenIdConnectConfiguration> _configManager;
        private readonly string _metadataEndpoint;
        OpenIdConnectConfiguration? _oidcConfig;

        public OpenIdConnectCachingSecurityTokenProvider(string metadataEndpoint)
        {
            _metadataEndpoint = metadataEndpoint;
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
                return _oidcConfig!.Issuer;
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
                return _oidcConfig!.SigningKeys;
            }
        }

        private void RetrieveMetadata()
        {
            // ConfigurationManager will return the same cached config unless enough time has passed,
            // then the return value will be a new object.
#pragma warning disable VSTHRD002 // Avoid problematic synchronous waits
            _oidcConfig = _configManager.GetConfigurationAsync().Result;
#pragma warning restore VSTHRD002 // Avoid problematic synchronous waits
        }
    }
}

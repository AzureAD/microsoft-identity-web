// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Client;
using Microsoft.Identity.Web;

namespace Microsoft.Identity.Web.OidcFic
{
    internal class OidcIdpSignedAssertionProvider : ClientAssertionProviderBase
    {
        private ITokenAcquirer? _tokenAcquirer = null;
        private readonly ITokenAcquirerFactory _tokenAcquirerFactory;
        private readonly MicrosoftIdentityApplicationOptions _options;
        private readonly string? _tokenExchangeUrl;
        private readonly ILogger? _logger;

        public bool RequiresSignedAssertionFmiPath { get; internal set; }

        public OidcIdpSignedAssertionProvider(ITokenAcquirerFactory tokenAcquirerFactory, MicrosoftIdentityApplicationOptions options, string? tokenExchangeUrl, ILogger? logger)
        {
            _tokenAcquirerFactory = tokenAcquirerFactory;
            _options = options;
            _tokenExchangeUrl = tokenExchangeUrl;
            _logger = logger;
        }

        protected override async Task<ClientAssertion> GetClientAssertionAsync(AssertionRequestOptions? assertionRequestOptions)
        {
            _tokenAcquirer ??= _tokenAcquirerFactory.GetTokenAcquirer(_options);

            string tokenExchangeUrl = _tokenExchangeUrl ?? "api://AzureADTokenExchange";
            AcquireTokenOptions? acquireTokenOptions = null;

            // During the construction of the CCA, IdWeb tried to understand which Credential description to use and to skip, and thefore
            // attempts to load the credentials with assertionRequestOptions = null (whereas it's not null when MSAL calls GetSignedAssertionAsync).
            // If assertionRequestOptions = null and RequiresSignedAssertionFmiPath is true
            // we postpone getting the signed assertion until the first call, when ClientAssertionFmiPath will be provided.
            if (RequiresSignedAssertionFmiPath && assertionRequestOptions == null)
            {
                if (_logger != null)
                {
                    _logger.PostponingToFirstCall();
                }

                // By using Now, we are certain to be called immediately again
                return new ClientAssertion(null!, DateTimeOffset.Now);
            }

            if (assertionRequestOptions != null && !string.IsNullOrEmpty(assertionRequestOptions.ClientAssertionFmiPath))
            {
                // Extract tenant from TokenEndpoint if available and if it's from the same cloud instance.
                // This enables tenant override propagation while preserving cross-cloud scenarios.
                // TokenEndpoint format: https://login.microsoftonline.com/{tenant}/oauth2/v2.0/token
                string? tenant = ExtractTenantFromTokenEndpointIfSameInstance(
                    assertionRequestOptions.TokenEndpoint,
                    _options.Instance);

                acquireTokenOptions = new AcquireTokenOptions()
                {
                    FmiPath = assertionRequestOptions.ClientAssertionFmiPath,
                    Tenant = tenant
                };
            }

            if (_logger != null)
            {
                _logger.AcquiringToken(tokenExchangeUrl, acquireTokenOptions?.FmiPath);
            }
            string effectiveTokenExchangeUrl = (tokenExchangeUrl.EndsWith("/.default", StringComparison.OrdinalIgnoreCase)
                ? tokenExchangeUrl : tokenExchangeUrl + "/.default");
            AcquireTokenResult result = await _tokenAcquirer.GetTokenForAppAsync(effectiveTokenExchangeUrl, acquireTokenOptions);
            if (_logger != null)
            {
                _logger.AcquiredToken(acquireTokenOptions?.FmiPath);
            }
            ClientAssertion clientAssertion;
            if (result != null)
            {
                clientAssertion = new ClientAssertion(result.AccessToken!, result.ExpiresOn);
            }
            else
            {
                clientAssertion = null!;
            }
            return clientAssertion;
        }

        /// <summary>
        /// Extracts the tenant from a token endpoint URL if the endpoint is from the same cloud instance.
        /// This enables tenant override propagation while preserving cross-cloud scenarios.
        /// </summary>
        /// <param name="tokenEndpoint">Token endpoint URL in the format https://login.microsoftonline.com/{tenant}/oauth2/v2.0/token</param>
        /// <param name="configuredInstance">The configured instance URL (e.g., https://login.microsoftonline.com/)</param>
        /// <returns>The tenant ID if the endpoint is from the same instance, otherwise null.</returns>
        private static string? ExtractTenantFromTokenEndpointIfSameInstance(string? tokenEndpoint, string? configuredInstance)
        {
            if (string.IsNullOrEmpty(tokenEndpoint) || string.IsNullOrEmpty(configuredInstance))
            {
                return null;
            }

            try
            {
                var endpointUri = new Uri(tokenEndpoint!);
                var instanceUri = new Uri(configuredInstance!.TrimEnd('/') + "/");

                // Only extract tenant if the host matches (same cloud instance)
                if (!string.Equals(endpointUri.Host, instanceUri.Host, StringComparison.OrdinalIgnoreCase))
                {
                    return null;
                }

                // TokenEndpoint format: https://login.microsoftonline.com/{tenant}/oauth2/v2.0/token
                // Extract {tenant} from the path.
                var pathSegments = endpointUri.AbsolutePath.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);

                // The tenant is typically the first segment after the host
                // e.g., /tenantId/oauth2/v2.0/token -> segments[0] = "tenantId"
                if (pathSegments.Length > 0)
                {
                    return pathSegments[0];
                }
            }
            catch (UriFormatException)
            {
                // Invalid URI, return null
            }

            return null;
        }
    }
}

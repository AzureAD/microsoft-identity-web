// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading;
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
        private const string DefaultTokenExchangeUrl = "api://AzureADTokenExchange";
        private const string DotDefaultSuffix = "/.default";

        // Signals the inner token acquisition to bind the assertion to an mTLS PoP certificate.
        // Kept in sync with the internal constant used by the token-acquisition pipeline.
        private const string TokenBindingParameterName = "IsTokenBinding";

        private const string MissingAssertionMessage =
            "[MsIdWeb] OidcIdpSignedAssertionProvider: the inner token acquisition returned no access token; " +
            "a bound OIDC federated assertion could not be produced.";

        private const string MissingBindingCertificateMessage =
            "[MsIdWeb] OidcIdpSignedAssertionProvider: the inner token acquisition did not return a binding certificate. " +
            "Ensure the inner application is configured with a credential able to produce an mTLS binding certificate " +
            "(for example SignedAssertionFromManagedIdentity or a certificate).";

        private ITokenAcquirer? _tokenAcquirer = null;
        private readonly ITokenAcquirerFactory _tokenAcquirerFactory;
        private readonly MicrosoftIdentityApplicationOptions _options;
        private readonly string? _tokenExchangeUrl;
        private readonly ILogger? _logger;

        public bool RequiresSignedAssertionFmiPath { get; internal set; }

        /// <summary>
        /// This provider can produce a binding certificate alongside its signed assertion: the
        /// certificate is returned dynamically by the inner token acquisition (see
        /// <see cref="GetSignedAssertionWithBindingAsync"/>), not from any statically configured
        /// certificate. If the inner application cannot mint a binding certificate, the bound
        /// acquisition fails clearly at runtime.
        /// </summary>
        public override bool SupportsTokenBinding => true;

        public OidcIdpSignedAssertionProvider(ITokenAcquirerFactory tokenAcquirerFactory, MicrosoftIdentityApplicationOptions options, string? tokenExchangeUrl, ILogger? logger)
        {
            _tokenAcquirerFactory = tokenAcquirerFactory;
            _options = options;
            _tokenExchangeUrl = tokenExchangeUrl;
            _logger = logger;
        }

        protected override async Task<ClientAssertion> GetClientAssertionAsync(AssertionRequestOptions? assertionRequestOptions)
        {
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

            AcquireTokenResult result = await AcquireOidcAssertionResultAsync(
                assertionRequestOptions,
                requestTokenBinding: false,
                cancellationToken: default).ConfigureAwait(false);

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
        /// Acquires a signed assertion together with its binding certificate for confidential
        /// clients configured for mTLS Proof-of-Possession (either <c>ProtocolScheme = "MTLS_POP"</c>
        /// on the final request, or <c>UseBoundCredential = true</c> on the outer OIDC credential).
        /// Both the assertion and the certificate come from the same inner token-acquisition result,
        /// so the certificate automatically flows with the assertion; no separate binding
        /// certificate is configured.
        /// </summary>
        /// <param name="assertionRequestOptions">Input options populated by MSAL.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The inner access token paired with the exact binding certificate returned by the inner acquisition.</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the inner acquisition does not return an access token or a binding certificate.
        /// </exception>
        public override async Task<ClientSignedAssertion?> GetSignedAssertionWithBindingAsync(
            AssertionRequestOptions? assertionRequestOptions,
            CancellationToken cancellationToken = default)
        {
            AcquireTokenResult result = await AcquireOidcAssertionResultAsync(
                assertionRequestOptions,
                requestTokenBinding: true,
                cancellationToken: cancellationToken).ConfigureAwait(false);

            if (result == null || string.IsNullOrEmpty(result.AccessToken))
            {
                throw new InvalidOperationException(MissingAssertionMessage);
            }

            if (result.BindingCertificate == null)
            {
                throw new InvalidOperationException(MissingBindingCertificateMessage);
            }

            // Propagate the exact certificate object returned by the inner token acquisition so the
            // outer confidential client pins its request to the same certificate.
            return new ClientSignedAssertion
            {
                Assertion = result.AccessToken,
                TokenBindingCertificate = result.BindingCertificate,
            };
        }

        /// <summary>
        /// Performs the inner OIDC token-exchange acquisition shared by the Bearer
        /// (<see cref="GetClientAssertionAsync"/>) and bound
        /// (<see cref="GetSignedAssertionWithBindingAsync"/>) paths, preserving the complete
        /// <see cref="AcquireTokenResult"/> (including its binding certificate) instead of reducing
        /// it to a string assertion. The inner <see cref="ITokenAcquirer"/> owns token caching.
        /// </summary>
        /// <param name="assertionRequestOptions">Input options populated by MSAL.</param>
        /// <param name="requestTokenBinding">When <c>true</c>, requests the inner acquisition to bind
        /// the assertion to an mTLS PoP certificate.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        private async Task<AcquireTokenResult> AcquireOidcAssertionResultAsync(
            AssertionRequestOptions? assertionRequestOptions,
            bool requestTokenBinding,
            CancellationToken cancellationToken)
        {
            _tokenAcquirer ??= _tokenAcquirerFactory.GetTokenAcquirer(_options);

            string tokenExchangeUrl = _tokenExchangeUrl ?? DefaultTokenExchangeUrl;
            string effectiveTokenExchangeUrl = tokenExchangeUrl.EndsWith(DotDefaultSuffix, StringComparison.OrdinalIgnoreCase)
                ? tokenExchangeUrl
                : tokenExchangeUrl + DotDefaultSuffix;

            string? fmiPath = assertionRequestOptions?.ClientAssertionFmiPath;

            // Propagate the outer tenant to the inner exchange only for FMI-path requests. For non-FMI
            // requests the inner application must use its own configured tenant — propagating the outer
            // tenant would break same-cloud, cross-tenant OIDC FIC (inner app A in tenant A being asked
            // to acquire its assertion from the outer's tenant B). This preserves the original behavior,
            // which only overrode the tenant when an FMI path was present. Within the FMI-path case,
            // ResolveInnerTenant keeps the same-cloud extraction plus an authoritative TenantId fallback
            // for endpoints the token-endpoint parser cannot read (e.g. the mTLS auth endpoint).
            string? tenant = !string.IsNullOrEmpty(fmiPath)
                ? ResolveInnerTenant(assertionRequestOptions)
                : null;

            Guid correlationId = assertionRequestOptions?.CorrelationId ?? Guid.Empty;

            AcquireTokenOptions? acquireTokenOptions = null;
            if (!string.IsNullOrEmpty(fmiPath)
                || !string.IsNullOrEmpty(tenant)
                || requestTokenBinding
                || correlationId != Guid.Empty)
            {
                acquireTokenOptions = new AcquireTokenOptions();

                if (!string.IsNullOrEmpty(fmiPath))
                {
                    acquireTokenOptions.FmiPath = fmiPath;
                }

                if (!string.IsNullOrEmpty(tenant))
                {
                    acquireTokenOptions.Tenant = tenant;
                }

                if (correlationId != Guid.Empty)
                {
                    acquireTokenOptions.CorrelationId = correlationId;
                }

                if (requestTokenBinding)
                {
                    acquireTokenOptions.ExtraParameters ??= new Dictionary<string, object>();
                    acquireTokenOptions.ExtraParameters[TokenBindingParameterName] = true;
                }
            }

            if (_logger != null)
            {
                _logger.AcquiringToken(tokenExchangeUrl, acquireTokenOptions?.FmiPath);
            }

            CancellationToken effectiveCancellationToken = cancellationToken != default
                ? cancellationToken
                : assertionRequestOptions?.CancellationToken ?? CancellationToken.None;

            AcquireTokenResult result = await _tokenAcquirer.GetTokenForAppAsync(
                effectiveTokenExchangeUrl,
                acquireTokenOptions,
                effectiveCancellationToken).ConfigureAwait(false);

            if (_logger != null)
            {
                _logger.AcquiredToken(acquireTokenOptions?.FmiPath);
            }

            return result;
        }

        /// <summary>
        /// Resolves the outer tenant for an FMI-path inner token exchange, or <c>null</c> when no
        /// override should be applied. The caller invokes this only when <c>ClientAssertionFmiPath</c>
        /// is present. The override is honored only when the outer request is on the same cloud
        /// instance as this application, preserving cross-cloud isolation.
        /// </summary>
        private string? ResolveInnerTenant(AssertionRequestOptions? assertionRequestOptions)
        {
            if (assertionRequestOptions == null)
            {
                return null;
            }

            // Preferred: the tenant embedded in a canonical oauth2 token endpoint (validated same-cloud).
            // Preserves the original extraction behavior for endpoints of the form
            // https://login.microsoftonline.com/{tenant}/oauth2/v2.0/token.
            string? tenant = ExtractTenantFromTokenEndpointIfSameInstance(
                assertionRequestOptions.TokenEndpoint,
                _options.Instance);
            if (!string.IsNullOrEmpty(tenant))
            {
                return tenant;
            }

            // Fallback: the authoritative AssertionRequestOptions.TenantId, honored only when the outer
            // request is on the same cloud instance. This covers endpoints the token-endpoint parser
            // cannot read (for example the mTLS auth endpoint, or a canonical authority URL without the
            // oauth2 path) while still preserving cross-cloud isolation.
            if (!string.IsNullOrEmpty(assertionRequestOptions.TenantId)
                && IsSameCloudInstance(
                    assertionRequestOptions.Authority,
                    assertionRequestOptions.TokenEndpoint,
                    _options.Instance))
            {
                return assertionRequestOptions.TenantId;
            }

            return null;
        }

        /// <summary>
        /// Determines whether the outer request (identified by its authority and/or token endpoint) is
        /// on the same cloud instance (host) as the configured instance.
        /// </summary>
        internal static bool IsSameCloudInstance(string? authority, string? tokenEndpoint, string? configuredInstance)
        {
            string? instanceHost = TryGetHost(configuredInstance);
            if (instanceHost == null)
            {
                return false;
            }

            return string.Equals(TryGetHost(authority), instanceHost, StringComparison.OrdinalIgnoreCase)
                || string.Equals(TryGetHost(tokenEndpoint), instanceHost, StringComparison.OrdinalIgnoreCase);
        }

        private static string? TryGetHost(string? url)
        {
            if (string.IsNullOrEmpty(url))
            {
                return null;
            }

            try
            {
                return new Uri(url!).Host;
            }
            catch (UriFormatException)
            {
                return null;
            }
        }

        /// <summary>
        /// Extracts the tenant from a token endpoint URL if the endpoint is from the same cloud instance.
        /// This enables tenant override propagation while preserving cross-cloud scenarios.
        /// </summary>
        /// <param name="tokenEndpoint">Token endpoint URL in the format https://login.microsoftonline.com/{tenant}/oauth2/v2.0/token</param>
        /// <param name="configuredInstance">The configured instance URL (e.g., https://login.microsoftonline.com/)</param>
        /// <returns>The tenant ID if the endpoint is from the same instance, otherwise null.</returns>
        internal static string? ExtractTenantFromTokenEndpointIfSameInstance(string? tokenEndpoint, string? configuredInstance)
        {
            if (string.IsNullOrEmpty(tokenEndpoint) || string.IsNullOrEmpty(configuredInstance))
            {
                return null;
            }

            try
            {
                var endpointUri = new Uri(tokenEndpoint!);
                
                // Safely construct instance URI by trimming trailing slash
                var normalizedInstance = configuredInstance!.TrimEnd('/');
                var instanceUri = new Uri(normalizedInstance);

                // Only extract tenant if the host matches (same cloud instance)
                if (!string.Equals(endpointUri.Host, instanceUri.Host, StringComparison.OrdinalIgnoreCase))
                {
                    return null;
                }

                // TokenEndpoint format: https://login.microsoftonline.com/{tenant}/oauth2/v2.0/token
                // Validate the path follows the expected pattern before extracting tenant.
                var pathSegments = endpointUri.AbsolutePath.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);

                // Expected pattern: [tenantId, oauth2, v2.0, token] or similar
                // We need at least the tenant segment and some oauth2 path segments
                if (pathSegments.Length >= 2)
                {
                    // Verify this looks like a token endpoint (contains "oauth2" somewhere after tenant)
                    for (int i = 1; i < pathSegments.Length; i++)
                    {
                        if (string.Equals(pathSegments[i], "oauth2", StringComparison.OrdinalIgnoreCase))
                        {
                            // Found oauth2 segment, the first segment is likely the tenant
                            return pathSegments[0];
                        }
                    }
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

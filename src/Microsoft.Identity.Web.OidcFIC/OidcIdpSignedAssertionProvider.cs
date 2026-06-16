// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Client;

namespace Microsoft.Identity.Web.OidcFic
{
    internal class OidcIdpSignedAssertionProvider : ClientAssertionProviderBase
    {
        private ITokenAcquirer? _tokenAcquirer = null;
        private readonly ITokenAcquirerFactory _tokenAcquirerFactory;
        private readonly MicrosoftIdentityApplicationOptions _options;
        private readonly string? _tokenExchangeUrl;
        private readonly ILogger? _logger;

        // The bound credential is captured by reference (not the cert directly) so that in-place updates
        // to credential.Certificate — e.g. a future ResetCredentials hook that re-reads KeyVault on rotation —
        // are picked up automatically without the provider holding a stale cert reference.
        private readonly CredentialDescription? _boundCredentialDescription;

        public bool RequiresSignedAssertionFmiPath { get; internal set; }

        public OidcIdpSignedAssertionProvider(ITokenAcquirerFactory tokenAcquirerFactory, MicrosoftIdentityApplicationOptions options, string? tokenExchangeUrl, ILogger? logger)
            : this(tokenAcquirerFactory, options, tokenExchangeUrl, logger, boundCredentialDescription: null)
        {
        }

        public OidcIdpSignedAssertionProvider(
            ITokenAcquirerFactory tokenAcquirerFactory,
            MicrosoftIdentityApplicationOptions options,
            string? tokenExchangeUrl,
            ILogger? logger,
            CredentialDescription? boundCredentialDescription)
        {
            _tokenAcquirerFactory = tokenAcquirerFactory;
            _options = options;
            _tokenExchangeUrl = tokenExchangeUrl;
            _logger = logger;
            _boundCredentialDescription = boundCredentialDescription;
        }

        /// <summary>
        /// Returns <c>true</c> when a binding certificate is currently available on the configured bound
        /// inner-leg <see cref="CredentialDescription"/> (its <see cref="CredentialDescription.Certificate"/>
        /// is non-null). The cert is read live, so if the underlying credential is later reset/cleared this
        /// property flips back to <c>false</c> and the dispatch in
        /// <c>ConfidentialClientApplicationBuilderExtension</c> will throw a clear
        /// <c>MissingTokenBindingCertificate</c> rather than silently use a stale cert.
        /// </summary>
        public override bool SupportsTokenBinding => _boundCredentialDescription?.Certificate is not null;

        /// <summary>
        /// Returns the OIDC-issued federated assertion paired with the configured binding certificate so
        /// MSAL can issue an mTLS PoP token from Entra.
        /// </summary>
        /// <remarks>
        /// The dispatch in <c>ConfidentialClientApplicationBuilderExtension</c> only calls this method when
        /// <see cref="SupportsTokenBinding"/> already returned <c>true</c>, and the returned
        /// <see cref="ClientSignedAssertion"/> is fed straight into MSAL with a null-forgiving operator —
        /// so returning <c>null</c> here would surface as a <see cref="NullReferenceException"/> downstream
        /// rather than a graceful fallback. The null-check below is therefore a defensive belt-and-braces
        /// for the race where the cert is cleared between the <see cref="SupportsTokenBinding"/> check and this call.
        /// <para>
        /// IMPORTANT: when the outer Entra leg requests mTLS PoP, the inner OIDC-IdP exchange must ALSO be
        /// requested as mTLS PoP. ESTS rejects "bearer inner JWT + mtls_pop outer" with
        /// <c>AADSTS392199 ("mtls_pop token type is not supported for bearer token in Entra ID federated
        /// identity flow")</c>. We propagate this via <c>AcquireTokenOptions.ExtraParameters["IsTokenBinding"] = true</c>
        /// which is the same magic-string IdWeb's <c>TokenAcquisition</c> / <c>DefaultAuthorizationHeaderProvider</c>
        /// already use internally to flip the inner CCA onto MSAL's <c>WithMtlsProofOfPossession()</c> path.
        /// </para>
        /// </remarks>
        public override async Task<ClientSignedAssertion?> GetSignedAssertionWithBindingAsync(
            AssertionRequestOptions? assertionRequestOptions,
            CancellationToken cancellationToken = default)
        {
            // Read once per call so rotation/reset is visible and we don't race against ourselves
            // between the cert read and the assertion request.
            X509Certificate2? bindingCertificate = _boundCredentialDescription?.Certificate;
            if (bindingCertificate is null)
            {
                // SupportsTokenBinding returned true at dispatch time (otherwise this method would not
                // have been called), but the cert was cleared between then and now. The dispatch in
                // ConfidentialClientApplicationBuilderExtension feeds the returned ClientSignedAssertion
                // into MSAL with a null-forgiving operator, so a null return would surface as an opaque
                // NullReferenceException downstream. Fail loudly here instead.
                throw new InvalidOperationException(
                    "OIDC FIC mTLS token binding was requested, but the configured binding certificate " +
                    "is no longer available. Ensure the bound credential is still loaded and has not been " +
                    "reset or cleared between the SupportsTokenBinding check and this call.");
            }

            ClientAssertion assertion = await AcquireOidcAssertionAsync(
                assertionRequestOptions,
                useMtlsPopForInnerExchange: true,
                cancellationToken).ConfigureAwait(false);

            // GetClientAssertionAsync / AcquireOidcAssertionAsync may return:
            //   * a sentinel ClientAssertion(null!, Now) for the RequiresSignedAssertionFmiPath postpone case
            //     (only when assertionRequestOptions == null — unreachable from MSAL's dispatch but still
            //     possible from internal callers that pre-warm the provider), or
            //   * null! when the inner-leg ITokenAcquirer.GetTokenForAppAsync returned no result.
            // Neither shape is a valid MSAL assertion for the MTLS_POP path, so fail loudly here.
            if (assertion is null || string.IsNullOrEmpty(assertion.SignedAssertion))
            {
                throw new InvalidOperationException(
                    "OIDC FIC signed assertion was not available for mTLS token binding. " +
                    "Ensure the external OIDC IdP credential is correctly configured and the IdP returned a token.");
            }

            return new ClientSignedAssertion
            {
                Assertion = assertion.SignedAssertion,
                TokenBindingCertificate = bindingCertificate,
            };
        }

        protected override Task<ClientAssertion> GetClientAssertionAsync(AssertionRequestOptions? assertionRequestOptions)
            => AcquireOidcAssertionAsync(
                assertionRequestOptions,
                useMtlsPopForInnerExchange: false,
                assertionRequestOptions?.CancellationToken ?? CancellationToken.None);

        // Magic-string mirror of TokenAcquisition.TokenBindingParameterName /
        // DefaultAuthorizationHeaderProvider.TokenBindingParameterName. Both are private constants in their
        // declaring classes, so we duplicate the value here. KEEP IN SYNC if the canonical value ever changes.
        private const string InnerExchangeTokenBindingParameterName = "IsTokenBinding";

        /// <summary>
        /// Shared inner-leg implementation called by both the bearer
        /// (<see cref="GetClientAssertionAsync"/>) and bound (<see cref="GetSignedAssertionWithBindingAsync"/>)
        /// paths. Centralizing here lets the bound path thread the explicit
        /// <see cref="CancellationToken"/> argument all the way down to
        /// <see cref="ITokenAcquirer.GetTokenForAppAsync(string, AcquireTokenOptions?, CancellationToken)"/>
        /// and flip the inner CCA into mTLS PoP mode when the outer leg is PoP.
        /// </summary>
        private async Task<ClientAssertion> AcquireOidcAssertionAsync(
            AssertionRequestOptions? assertionRequestOptions,
            bool useMtlsPopForInnerExchange,
            CancellationToken cancellationToken)
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

            if (assertionRequestOptions != null)
            {
                // Reviewer feedback: tenant override propagation must not be tied to ClientAssertionFmiPath.
                // MSAL can hand us a TokenEndpoint with a tenant override without an FMI path
                // (e.g. WithTenant() on the outer builder). We extract the tenant whenever the endpoint
                // is from the same cloud instance — the cross-cloud guard in
                // ExtractTenantFromTokenEndpointIfSameInstance still preserves cross-cloud scenarios.
                string? fmiPath = assertionRequestOptions.ClientAssertionFmiPath;
                string? tenant = ExtractTenantFromTokenEndpointIfSameInstance(
                    assertionRequestOptions.TokenEndpoint,
                    _options.Instance);

                if (!string.IsNullOrEmpty(fmiPath) || !string.IsNullOrEmpty(tenant))
                {
                    acquireTokenOptions = new AcquireTokenOptions()
                    {
                        FmiPath = fmiPath,
                        Tenant = tenant
                    };
                }
            }

            // When the outer Entra leg requests mTLS PoP, propagate that requirement to the inner-leg
            // ITokenAcquirer so IdWeb routes the inner CCA through MSAL's WithMtlsProofOfPossession().
            // Otherwise ESTS rejects with AADSTS392199 ("mtls_pop token type is not supported for bearer
            // token in Entra ID federated identity flow").
            if (useMtlsPopForInnerExchange)
            {
                acquireTokenOptions ??= new AcquireTokenOptions();
                acquireTokenOptions.ExtraParameters ??= new System.Collections.Generic.Dictionary<string, object>();
                acquireTokenOptions.ExtraParameters[InnerExchangeTokenBindingParameterName] = true;
            }

            if (_logger != null)
            {
                _logger.AcquiringToken(tokenExchangeUrl, acquireTokenOptions?.FmiPath);
            }
            string effectiveTokenExchangeUrl = (tokenExchangeUrl.EndsWith("/.default", StringComparison.OrdinalIgnoreCase)
                ? tokenExchangeUrl : tokenExchangeUrl + "/.default");
            AcquireTokenResult result = await _tokenAcquirer.GetTokenForAppAsync(effectiveTokenExchangeUrl, acquireTokenOptions, cancellationToken).ConfigureAwait(false);
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

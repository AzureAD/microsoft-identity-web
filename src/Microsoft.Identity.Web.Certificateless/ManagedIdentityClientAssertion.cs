// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.AppConfig;
using Microsoft.Identity.Client.Extensibility;
using Microsoft.Identity.Client.Instance.Discovery;
#if NETCOREAPP
using Microsoft.Identity.Client.KeyAttestation;
#endif
using Microsoft.Identity.Web.Certificateless;
using Microsoft.Identity.Web.TestOnly;
using Microsoft.IdentityModel.LoggingExtensions;

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// See https://aka.ms/ms-id-web/certificateless.
    /// </summary>
    public class ManagedIdentityClientAssertion : ClientAssertionProviderBase
    {
        private IManagedIdentityApplication _managedIdentityApplication;
        private readonly string? _explicitTokenExchangeUrl;
        private readonly string _defaultTokenExchangeUrl;
        private readonly ILogger? _logger;

        /// <summary>
        /// See https://aka.ms/ms-id-web/certificateless.
        /// </summary>
        /// <param name="managedIdentityClientId">Optional ClientId of the Managed Identity</param>
        public ManagedIdentityClientAssertion(string? managedIdentityClientId) :
            this(managedIdentityClientId, tokenExchangeUrl: null, logger: null)
        {

        }

        /// <summary>
        /// See https://aka.ms/ms-id-web/certificateless.
        /// </summary>
        /// <param name="managedIdentityClientId">Optional ClientId of the Managed Identity</param>
        /// <param name="tokenExchangeUrl">Optional audience of the token to be requested from Managed Identity. When omitted, the audience is auto-resolved from the calling confidential client's authority host (public cloud resolves to "api://AzureADTokenExchange"; sovereign/national clouds resolve to their own value), falling back to the public-cloud audience.</param>
        public ManagedIdentityClientAssertion(string? managedIdentityClientId, string? tokenExchangeUrl) :
            this(managedIdentityClientId, tokenExchangeUrl, null)
        {
        }

        /// <summary>
        /// See https://aka.ms/ms-id-web/certificateless.
        /// </summary>
        /// <param name="managedIdentityClientId">Optional ClientId of the Managed Identity</param>
        /// <param name="tokenExchangeUrl">Optional audience of the token to be requested from Managed Identity. When omitted, the audience is auto-resolved from the calling confidential client's authority host (public cloud resolves to "api://AzureADTokenExchange"; sovereign/national clouds resolve to their own value), falling back to the public-cloud audience.</param>
        /// <param name="logger">A logger</param>
        public ManagedIdentityClientAssertion(
            string? managedIdentityClientId,
            string? tokenExchangeUrl,
            ILogger? logger)
            : this(
                managedIdentityClientId,
                tokenExchangeUrl,
                logger,
                ManagedIdentityClientAssertionTestHook.HttpClientFactoryForTests)
        {
        }


        /// <summary>
        /// Same as <see cref="ManagedIdentityClientAssertion(string?, string?, ILogger?)"/>,
        /// but allows injecting a custom MSAL HttpClient factory (used by tests).
        /// </summary>
        /// <param name="managedIdentityClientId">Optional ClientId of the Managed Identity</param>
        /// <param name="tokenExchangeUrl">Optional audience of the token to be requested from Managed Identity. When omitted, the audience is auto-resolved from the calling confidential client's authority host (public cloud resolves to "api://AzureADTokenExchange"; sovereign/national clouds resolve to their own value), falling back to the public-cloud audience.</param>
        /// <param name="logger">A logger.</param>
        /// <param name="testHttpClientFactory">Optional MSAL HttpClient factory.</param>
        internal ManagedIdentityClientAssertion(
            string? managedIdentityClientId,
            string? tokenExchangeUrl,
            ILogger? logger,
            IMsalHttpClientFactory? testHttpClientFactory)
        {
            // Resolution precedence for the token-exchange audience (see ResolveTokenExchangeUrl):
            //   1. An explicit tokenExchangeUrl passed here always wins.
            //   2. Otherwise, resolve per-request from the calling confidential client's authority host
            //      (so a sovereign/national cloud request auto-selects its own audience).
            //   3. Otherwise, fall back to the public-cloud audience computed here.
            // The default is taken from MSAL's cloud metadata for the public cloud (single source of
            // truth), falling back to the documented constant if the table has no public entry.
            _explicitTokenExchangeUrl = tokenExchangeUrl;
            _defaultTokenExchangeUrl =
                (KnownCloudMetadata.Default.GetByAuthorityHost(CertificatelessConstants.PublicCloudInstanceHost) is { } publicValues
                    && publicValues.TryGetValue(CloudMetadataKeyNames.FederatedCredentialAudience, out string? publicAudience)
                    && !string.IsNullOrEmpty(publicAudience))
                        ? publicAudience!
                        : CertificatelessConstants.DefaultTokenExchangeUrl;
            _logger = logger;

            var id = ManagedIdentityId.SystemAssigned;
            if (!string.IsNullOrEmpty(managedIdentityClientId))
            {
                id = ManagedIdentityId.WithUserAssignedClientId(managedIdentityClientId);
            }

            var builder = ManagedIdentityApplicationBuilder.Create(id);

            if (testHttpClientFactory != null)
            {
                builder = builder.WithHttpClientFactory(testHttpClientFactory);
            }

            if (_logger != null)
            {
                builder = builder.WithLogging(new IdentityLoggerAdapter(_logger), enablePiiLogging: false);
                _logger.LogInformation($"ManagedIdentityClientAssertion with tokenExchangeUrl={_explicitTokenExchangeUrl ?? _defaultTokenExchangeUrl}");
            }

            _managedIdentityApplication = builder
                .Build();
        }

        /// <summary>
        /// Prototype of certificate-less authentication using a signed assertion
        /// acquired with managed identity (certificateless).
        /// </summary>
        /// <returns>The signed assertion.</returns>
        protected override async Task<ClientAssertion> GetClientAssertionAsync(
            AssertionRequestOptions? assertionRequestOptions)
        {
            var result = await AcquireManagedIdentityTokenAsync(
                    assertionRequestOptions,
                    bindToCertificate: false,
                    cancellationToken: default)
                .ConfigureAwait(false);

            return new ClientAssertion(result.AccessToken, result.ExpiresOn);
        }

        /// <summary>
        /// Returns <c>true</c>: managed identity provides a binding certificate alongside the
        /// federated assertion via MSAL's IMDS V2 mTLS PoP flow.
        /// </summary>
        public override bool SupportsTokenBinding => true;

        /// <summary>
        /// Acquires a managed identity token bound to a binding certificate via mTLS PoP,
        /// returning both the assertion and the binding certificate so MSAL can pin the outer
        /// confidential client request to the same certificate (FIC + mTLS PoP, two-leg flow).
        /// </summary>
        /// <remarks>
        /// Used when the consuming confidential client has token binding enabled (e.g.,
        /// <c>AuthorizationHeaderProviderOptions.ProtocolScheme = "MTLS_POP"</c>). Requires
        /// MSAL.NET key-attestation support and an Azure VM / Arc-hosted managed identity
        /// capable of returning a <see cref="AuthenticationResult.BindingCertificate"/>.
        /// </remarks>
        public override async Task<ClientSignedAssertion?> GetSignedAssertionWithBindingAsync(
            AssertionRequestOptions? assertionRequestOptions,
            CancellationToken cancellationToken = default)
        {
            var result = await AcquireManagedIdentityTokenAsync(
                    assertionRequestOptions,
                    bindToCertificate: true,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            // MSAL guarantees BindingCertificate is non-null when WithMtlsProofOfPossession()
            // succeeds; failure to bind surfaces as an MsalServiceException from ExecuteAsync.
            return new ClientSignedAssertion
            {
                Assertion = result.AccessToken,
                TokenBindingCertificate = result.BindingCertificate!,
            };
        }

        /// <summary>
        /// Builds and executes the underlying managed-identity token request shared by both the
        /// bearer (<see cref="GetClientAssertionAsync"/>) and mTLS PoP
        /// (<see cref="GetSignedAssertionWithBindingAsync"/>) code paths.
        /// </summary>
        private async Task<AuthenticationResult> AcquireManagedIdentityTokenAsync(
            AssertionRequestOptions? assertionRequestOptions,
            bool bindToCertificate,
            CancellationToken cancellationToken)
        {
            var miBuilder = _managedIdentityApplication
                .AcquireTokenForManagedIdentity(ResolveTokenExchangeUrl(assertionRequestOptions));

            if (bindToCertificate)
            {
                miBuilder = miBuilder.WithMtlsProofOfPossession();
#if NETCOREAPP
                // Key attestation is only available on modern .NET; on .NET Framework/netstandard the
                // KeyAttestation dependency is intentionally absent (issue #3894).
                miBuilder = miBuilder.WithAttestationSupport();
#endif
            }

            // Propagate claims into the MI token request.
            // This also forces MSAL to bypass the MI token cache when claims are present.
            if (!string.IsNullOrEmpty(assertionRequestOptions?.Claims))
            {
                miBuilder.WithClaims(assertionRequestOptions!.Claims);
            }

            // Carry the outer request's OTel tags enricher onto this MI FIC leg too, like the OIDC leg does.
            if (assertionRequestOptions?.OtelTagsEnricher != null)
            {
                miBuilder.WithOtelTagsEnricher(assertionRequestOptions.OtelTagsEnricher);
            }

            CancellationToken effectiveCancellationToken = cancellationToken != default
                ? cancellationToken
                : assertionRequestOptions?.CancellationToken ?? CancellationToken.None;

            return await miBuilder
                .ExecuteAsync(effectiveCancellationToken)
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Resolves the token-exchange audience for a given assertion request.
        /// Precedence: an explicit tokenExchangeUrl supplied to the constructor always wins; otherwise the
        /// audience is auto-resolved from the calling confidential client's authority host (so a
        /// sovereign/national cloud request selects its own audience); otherwise the public-cloud default
        /// is used. Because the resolution keys off the request authority, a single instance shared across
        /// clouds still emits the correct per-cloud audience.
        /// </summary>
        private string ResolveTokenExchangeUrl(AssertionRequestOptions? assertionRequestOptions)
        {
            if (!string.IsNullOrEmpty(_explicitTokenExchangeUrl))
            {
                return _explicitTokenExchangeUrl!;
            }

            string? host = TryGetHost(assertionRequestOptions?.Authority);
            if (!string.IsNullOrEmpty(host) &&
                KnownCloudMetadata.Default.GetByAuthorityHost(host) is { } values &&
                values.TryGetValue(CloudMetadataKeyNames.FederatedCredentialAudience, out string? audience) &&
                !string.IsNullOrEmpty(audience))
            {
                return audience!;
            }

            return _defaultTokenExchangeUrl;
        }

        /// <summary>
        /// Extracts the host from an authority string. Accepts either a full authority URI
        /// (e.g. "https://login.microsoftonline.us/tenant") or a bare host, returning the input
        /// unchanged when it is not a parseable absolute URI.
        /// </summary>
        private static string? TryGetHost(string? authority)
        {
            if (string.IsNullOrEmpty(authority))
            {
                return null;
            }

            return Uri.TryCreate(authority, UriKind.Absolute, out Uri? uri)
                ? uri.Host
                : authority;
        }

    }
}

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
        private readonly string _tokenExchangeUrl;
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
        /// <param name="tokenExchangeUrl">Optional audience of the token to be requested from Managed Identity. Default value is "api://AzureADTokenExchange". 
        /// This value is different on clouds other than Azure Public</param>
        public ManagedIdentityClientAssertion(string? managedIdentityClientId, string? tokenExchangeUrl) :
            this(managedIdentityClientId, tokenExchangeUrl, null)
        {
        }

        /// <summary>
        /// See https://aka.ms/ms-id-web/certificateless.
        /// </summary>
        /// <param name="managedIdentityClientId">Optional ClientId of the Managed Identity</param>
        /// <param name="tokenExchangeUrl">Optional audience of the token to be requested from Managed Identity. Default value is "api://AzureADTokenExchange". 
        /// This value is different on clouds other than Azure Public</param>
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
        /// <param name="tokenExchangeUrl">Optional audience of the token to be requested from Managed Identity. Default value is "api://AzureADTokenExchange". 
        /// This value is different on clouds other than Azure Public</param>
        /// <param name="logger">A logger.</param>
        /// <param name="testHttpClientFactory">Optional MSAL HttpClient factory.</param>
        internal ManagedIdentityClientAssertion(
            string? managedIdentityClientId,
            string? tokenExchangeUrl,
            ILogger? logger,
            IMsalHttpClientFactory? testHttpClientFactory)
        {
            _tokenExchangeUrl = tokenExchangeUrl ?? CertificatelessConstants.DefaultTokenExchangeUrl;
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
                _logger.LogInformation($"ManagedIdentityClientAssertion with tokenExchangeUrl={_tokenExchangeUrl}");
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
                .AcquireTokenForManagedIdentity(_tokenExchangeUrl);

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

            CancellationToken effectiveCancellationToken = cancellationToken != default
                ? cancellationToken
                : assertionRequestOptions?.CancellationToken ?? CancellationToken.None;

            return await miBuilder
                .ExecuteAsync(effectiveCancellationToken)
                .ConfigureAwait(false);
        }

    }
}

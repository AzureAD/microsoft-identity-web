// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Client;
using Microsoft.Identity.Web.OidcFic;
using NSubstitute;
using Xunit;

namespace Microsoft.Identity.Web.Test
{
    /// <summary>
    /// Unit tests for the binding-certificate extension on <see cref="OidcIdpSignedAssertionProvider"/>
    /// added for issue #3851 (OIDC-FIC mTLS Proof-of-Possession).
    /// </summary>
    public class OidcIdpBindingCertificateProviderTests
    {
        [Fact]
        public void SupportsTokenBinding_WithoutBoundCredential_ReturnsFalse()
        {
            var provider = BuildProvider(boundCredentialDescription: null);

            Assert.False(provider.SupportsTokenBinding);
        }

        [Fact]
        public void SupportsTokenBinding_WithBoundCredentialCarryingCertificate_ReturnsTrue()
        {
            using X509Certificate2 cert = CreateSelfSignedTestCertificate();
            var bound = new CredentialDescription
            {
                SourceType = CredentialSource.Certificate,
                UseBoundCredential = true,
                Certificate = cert,
            };

            var provider = BuildProvider(boundCredentialDescription: bound);

            Assert.True(provider.SupportsTokenBinding);
        }

        [Fact]
        public void SupportsTokenBinding_BoundCredentialCertificateClearedAfterConstruction_ReturnsFalse()
        {
            // Per-call read of credential.Certificate means a downstream reset/clear is reflected immediately —
            // no stale cert lingering on the cached provider after rotation/clearance.
            using X509Certificate2 cert = CreateSelfSignedTestCertificate();
            var bound = new CredentialDescription
            {
                SourceType = CredentialSource.Certificate,
                UseBoundCredential = true,
                Certificate = cert,
            };

            var provider = BuildProvider(boundCredentialDescription: bound);
            Assert.True(provider.SupportsTokenBinding);

            bound.Certificate = null;

            Assert.False(provider.SupportsTokenBinding);
        }

        [Fact]
        public async Task GetSignedAssertionWithBindingAsync_WithoutBoundCredential_ThrowsClearError()
        {
            // Dispatch in ConfidentialClientApplicationBuilderExtension only invokes us when
            // SupportsTokenBinding is true, and it feeds the result into MSAL with a null-forgiving
            // operator — so we must throw a clear error instead of returning null when the cert
            // disappears mid-flight (e.g. race between the SupportsTokenBinding check and this call).
            var provider = BuildProvider(boundCredentialDescription: null);

            InvalidOperationException ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => provider.GetSignedAssertionWithBindingAsync(assertionRequestOptions: null));

            Assert.Contains("mTLS token binding", ex.Message, StringComparison.Ordinal);
            Assert.Contains("binding certificate", ex.Message, StringComparison.Ordinal);
        }

        [Fact]
        public async Task GetSignedAssertionWithBindingAsync_WithBoundCredential_PairsAssertionWithLiveCertificate()
        {
            using X509Certificate2 cert = CreateSelfSignedTestCertificate();
            const string fakeOidcAssertion = "fake-oidc-assertion";
            var bound = new CredentialDescription
            {
                SourceType = CredentialSource.Certificate,
                UseBoundCredential = true,
                Certificate = cert,
            };

            var tokenAcquirer = Substitute.For<ITokenAcquirer>();
            tokenAcquirer
                .GetTokenForAppAsync(Arg.Any<string>(), Arg.Any<AcquireTokenOptions?>(), Arg.Any<CancellationToken>())
                .Returns(NewAcquireTokenResult(fakeOidcAssertion));

            var options = new MicrosoftIdentityApplicationOptions { Instance = "https://login.microsoftonline.com/" };
            var factory = Substitute.For<ITokenAcquirerFactory>();
            factory.GetTokenAcquirer(options).Returns(tokenAcquirer);

            var provider = new OidcIdpSignedAssertionProvider(factory, options, tokenExchangeUrl: null, logger: null, boundCredentialDescription: bound);

            ClientSignedAssertion? result = await provider
                .GetSignedAssertionWithBindingAsync(assertionRequestOptions: null);

            Assert.NotNull(result);
            Assert.Equal(fakeOidcAssertion, result!.Assertion);
            Assert.Same(cert, result.TokenBindingCertificate);
        }

        [Fact]
        public async Task GetSignedAssertionWithBindingAsync_BoundCredentialRotatedBetweenCalls_PicksUpNewCertificate()
        {
            // Simulates KeyVault rotation: customer (or some future reset hook) swaps credential.Certificate
            // in place; the cached provider must reflect the new cert without being reconstructed.
            using X509Certificate2 originalCert = CreateSelfSignedTestCertificate("CN=Original");
            using X509Certificate2 rotatedCert = CreateSelfSignedTestCertificate("CN=Rotated");
            var bound = new CredentialDescription
            {
                SourceType = CredentialSource.Certificate,
                UseBoundCredential = true,
                Certificate = originalCert,
            };

            var tokenAcquirer = Substitute.For<ITokenAcquirer>();
            tokenAcquirer
                .GetTokenForAppAsync(Arg.Any<string>(), Arg.Any<AcquireTokenOptions?>(), Arg.Any<CancellationToken>())
                .Returns(NewAcquireTokenResult("any-assertion"));

            var options = new MicrosoftIdentityApplicationOptions { Instance = "https://login.microsoftonline.com/" };
            var factory = Substitute.For<ITokenAcquirerFactory>();
            factory.GetTokenAcquirer(options).Returns(tokenAcquirer);

            var provider = new OidcIdpSignedAssertionProvider(factory, options, tokenExchangeUrl: null, logger: null, boundCredentialDescription: bound);

            ClientSignedAssertion? first = await provider.GetSignedAssertionWithBindingAsync(assertionRequestOptions: null);
            Assert.Same(originalCert, first!.TokenBindingCertificate);

            bound.Certificate = rotatedCert;

            ClientSignedAssertion? second = await provider.GetSignedAssertionWithBindingAsync(assertionRequestOptions: null);
            Assert.Same(rotatedCert, second!.TokenBindingCertificate);
        }

        [Fact]
        public async Task GetSignedAssertionWithBindingAsync_TokenAcquirerReturnsNullResult_ThrowsClearError()
        {
            // The base implementation returns a null! ClientAssertion when GetTokenForAppAsync yields no result.
            // The binding path must surface this as a clear InvalidOperationException rather than an NRE / a
            // malformed ClientSignedAssertion that MSAL would later reject with a cryptic InvalidClientAssertion.
            using X509Certificate2 cert = CreateSelfSignedTestCertificate();
            var bound = new CredentialDescription
            {
                SourceType = CredentialSource.Certificate,
                UseBoundCredential = true,
                Certificate = cert,
            };

            var tokenAcquirer = Substitute.For<ITokenAcquirer>();
            tokenAcquirer
                .GetTokenForAppAsync(Arg.Any<string>(), Arg.Any<AcquireTokenOptions?>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<AcquireTokenResult>(null!));

            var options = new MicrosoftIdentityApplicationOptions { Instance = "https://login.microsoftonline.com/" };
            var factory = Substitute.For<ITokenAcquirerFactory>();
            factory.GetTokenAcquirer(options).Returns(tokenAcquirer);

            var provider = new OidcIdpSignedAssertionProvider(factory, options, tokenExchangeUrl: null, logger: null, boundCredentialDescription: bound);

            InvalidOperationException ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => provider.GetSignedAssertionWithBindingAsync(assertionRequestOptions: null));
            Assert.Contains("OIDC FIC signed assertion was not available", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task GetSignedAssertionWithBindingAsync_PassesCancellationTokenToTokenAcquirer()
        {
            using X509Certificate2 cert = CreateSelfSignedTestCertificate();
            var bound = new CredentialDescription
            {
                SourceType = CredentialSource.Certificate,
                UseBoundCredential = true,
                Certificate = cert,
            };

            var tokenAcquirer = Substitute.For<ITokenAcquirer>();
            tokenAcquirer
                .GetTokenForAppAsync(Arg.Any<string>(), Arg.Any<AcquireTokenOptions?>(), Arg.Any<CancellationToken>())
                .Returns(NewAcquireTokenResult("any-assertion"));

            var options = new MicrosoftIdentityApplicationOptions { Instance = "https://login.microsoftonline.com/" };
            var factory = Substitute.For<ITokenAcquirerFactory>();
            factory.GetTokenAcquirer(options).Returns(tokenAcquirer);

            var provider = new OidcIdpSignedAssertionProvider(factory, options, tokenExchangeUrl: null, logger: null, boundCredentialDescription: bound);

            using var cts = new CancellationTokenSource();
            CancellationToken expectedToken = cts.Token;

            await provider.GetSignedAssertionWithBindingAsync(assertionRequestOptions: null, cancellationToken: expectedToken);

            await tokenAcquirer
                .Received(1)
                .GetTokenForAppAsync(Arg.Any<string>(), Arg.Any<AcquireTokenOptions?>(), expectedToken);
        }

        [Fact]
        public void Constructor_DefaultsBoundCredentialToNullWhenLegacyOverloadUsed()
        {
            // Existing internal callers (and any future ones) that only pass four args must not opt-in
            // to token binding by accident.
            var provider = new OidcIdpSignedAssertionProvider(
                Substitute.For<ITokenAcquirerFactory>(),
                new MicrosoftIdentityApplicationOptions(),
                tokenExchangeUrl: null,
                logger: null);

            Assert.False(provider.SupportsTokenBinding);
        }

        private static OidcIdpSignedAssertionProvider BuildProvider(CredentialDescription? boundCredentialDescription)
        {
            return new OidcIdpSignedAssertionProvider(
                Substitute.For<ITokenAcquirerFactory>(),
                new MicrosoftIdentityApplicationOptions(),
                tokenExchangeUrl: null,
                logger: null,
                boundCredentialDescription: boundCredentialDescription);
        }

        private static AcquireTokenResult NewAcquireTokenResult(string accessToken)
            => new AcquireTokenResult(
                accessToken: accessToken,
                expiresOn: DateTimeOffset.UtcNow.AddHours(1),
                tenantId: "fake-tenant",
                idToken: string.Empty,
                scopes: Array.Empty<string>(),
                correlationId: Guid.NewGuid(),
                tokenType: "Bearer");

        private static X509Certificate2 CreateSelfSignedTestCertificate(string subject = "CN=OidcFicBindingCertTest")
        {
            using var rsa = RSA.Create(2048);
            var request = new CertificateRequest(
                new X500DistinguishedName(subject),
                rsa,
                HashAlgorithmName.SHA256,
                RSASignaturePadding.Pkcs1);

            return request.CreateSelfSigned(
                DateTimeOffset.UtcNow.AddMinutes(-5),
                DateTimeOffset.UtcNow.AddHours(1));
        }
    }
}

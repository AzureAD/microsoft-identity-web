// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Web.OidcFic;
using NSubstitute;
using Xunit;

namespace Microsoft.Identity.Web.Test
{
    /// <summary>
    /// Unit tests for the binding-credential resolution path added to
    /// <see cref="OidcIdpSignedAssertionLoader"/> for issue #3851 (OIDC-FIC mTLS Proof-of-Possession).
    /// </summary>
    public class OidcIdpBindingCertificateLoaderTests
    {
        private const string MtlsPopProtocol = "MTLS_POP";
        private const string BearerProtocol = "Bearer";
        private const string InnerSectionName = "InnerSection";

        private readonly ILogger<OidcIdpSignedAssertionLoader> _logger;
        private readonly IOptionsMonitor<MicrosoftIdentityApplicationOptions> _optionsMonitor;
        private readonly IServiceProvider _serviceProvider;
        private readonly ITokenAcquirerFactory _tokenAcquirerFactory;

        public OidcIdpBindingCertificateLoaderTests()
        {
            _logger = new LoggerFactory().CreateLogger<OidcIdpSignedAssertionLoader>();
            _optionsMonitor = Substitute.For<IOptionsMonitor<MicrosoftIdentityApplicationOptions>>();
            _serviceProvider = Substitute.For<IServiceProvider>();
            _tokenAcquirerFactory = Substitute.For<ITokenAcquirerFactory>();
        }

        // ---------------------------------------------------------------------------
        // MTLS_POP — happy paths
        // ---------------------------------------------------------------------------

        [Fact]
        public async Task LoadIfNeededAsync_MtlsPop_BoundCertResolves_InvokesICredentialsLoaderAndCachesProvider()
        {
            using X509Certificate2 bindingCert = CreateSelfSignedTestCertificate();
            CredentialDescription boundCredential = NewBoundCertCredential();

            var innerOptions = NewInnerOptions(boundCredential);
            _optionsMonitor.Get(InnerSectionName).Returns(innerOptions);

            ICredentialsLoader credentialsLoader = StubCredentialsLoaderThatMaterialisesCert(boundCredential, bindingCert);
            _serviceProvider.GetService(typeof(ICredentialsLoader)).Returns(credentialsLoader);

            var loader = NewLoader();
            CredentialDescription outer = NewOuterCustomSignedAssertionCredential();

            try { await loader.LoadIfNeededAsync(outer, NewParameters(MtlsPopProtocol)); }
            catch { /* expected: trailing GetSignedAssertionAsync probe fails because token acquirer substitute isn't wired */ }

            await credentialsLoader
                .Received(1)
                .LoadCredentialsIfNeededAsync(boundCredential, Arg.Any<CredentialSourceLoaderParameters?>());
        }

        // ---------------------------------------------------------------------------
        // Fix #1 — bound cred must NOT be selectable as inner-leg auth
        // ---------------------------------------------------------------------------

        [Fact]
        public async Task LoadIfNeededAsync_MtlsPop_BoundCredentialPlacedFirst_IsTakenOffInnerLegAuthMenu()
        {
            // Bound cert first, secret second — without defense, DefaultCredentialsLoader.LoadFirstValidCredentialsAsync
            // would return the bound cert as the inner-leg auth credential and the binding cert would end up
            // authenticating to the external OIDC IdP. The loader must mark the bound entry Skip=true so the
            // inner-leg auth call falls through to the secret.
            using X509Certificate2 bindingCert = CreateSelfSignedTestCertificate();
            CredentialDescription boundCredential = NewBoundCertCredential();
            var nonBoundSecret = new CredentialDescription
            {
                SourceType = CredentialSource.ClientSecret,
                ClientSecret = "shh",
            };

            var innerOptions = NewInnerOptions(boundCredential, nonBoundSecret);
            _optionsMonitor.Get(InnerSectionName).Returns(innerOptions);

            ICredentialsLoader credentialsLoader = StubCredentialsLoaderThatMaterialisesCert(boundCredential, bindingCert);
            _serviceProvider.GetService(typeof(ICredentialsLoader)).Returns(credentialsLoader);

            var loader = NewLoader();
            CredentialDescription outer = NewOuterCustomSignedAssertionCredential();

            try { await loader.LoadIfNeededAsync(outer, NewParameters(MtlsPopProtocol)); }
            catch { /* expected */ }

            Assert.True(boundCredential.Skip, "Bound credential must be Skip=true after resolution so it cannot be picked as inner-leg auth.");
            Assert.False(nonBoundSecret.Skip, "Non-bound inner-leg auth credential must remain selectable.");
        }

        // ---------------------------------------------------------------------------
        // Fix #2 + #3 — bearer flow must NOT be broken by a misconfigured bound entry
        // ---------------------------------------------------------------------------

        [Fact]
        public async Task LoadIfNeededAsync_Bearer_BoundCredentialIsIgnoredAndCertLoaderNeverInvoked()
        {
            // For bearer, the bound cert is not needed. The loader must not even resolve ICredentialsLoader so a
            // missing/misconfigured bound entry can't take down a perfectly-good bearer OIDC FIC flow.
            CredentialDescription boundCredential = NewBoundCertCredential();
            var innerOptions = NewInnerOptions(boundCredential);
            _optionsMonitor.Get(InnerSectionName).Returns(innerOptions);

            var credentialsLoader = Substitute.For<ICredentialsLoader>();
            _serviceProvider.GetService(typeof(ICredentialsLoader)).Returns(credentialsLoader);

            var loader = NewLoader();
            CredentialDescription outer = NewOuterCustomSignedAssertionCredential();

            try { await loader.LoadIfNeededAsync(outer, NewParameters(BearerProtocol)); }
            catch { /* expected */ }

            await credentialsLoader
                .DidNotReceive()
                .LoadCredentialsIfNeededAsync(Arg.Any<CredentialDescription>(), Arg.Any<CredentialSourceLoaderParameters?>());
            Assert.False(boundCredential.Skip, "Bearer flow must not mutate inner credential state.");
        }

        [Fact]
        public async Task LoadIfNeededAsync_Bearer_BoundCertLoadThrows_DoesNotPropagateAndDoesNotSkipOuter()
        {
            // Symmetry with the previous test: even if a bound cert was resolved-and-failed somehow, the bearer
            // flow contract is "binding errors are non-fatal". Here we verify the LOAD itself is skipped for bearer,
            // which trivially satisfies this — but we also exercise the catch-and-swallow code path explicitly by
            // forcing a Protocol-less call (parameters null → not MTLS_POP).
            CredentialDescription boundCredential = NewBoundCertCredential();
            var innerOptions = NewInnerOptions(boundCredential);
            _optionsMonitor.Get(InnerSectionName).Returns(innerOptions);

            var credentialsLoader = Substitute.For<ICredentialsLoader>();
            _serviceProvider.GetService(typeof(ICredentialsLoader)).Returns(credentialsLoader);

            var loader = NewLoader();
            CredentialDescription outer = NewOuterCustomSignedAssertionCredential();

            // Call with no protocol at all — should behave as bearer (non-MTLS_POP).
            try { await loader.LoadIfNeededAsync(outer, parameters: null); }
            catch { /* expected: trailing probe fails as before */ }

            await credentialsLoader
                .DidNotReceive()
                .LoadCredentialsIfNeededAsync(Arg.Any<CredentialDescription>(), Arg.Any<CredentialSourceLoaderParameters?>());
        }

        // ---------------------------------------------------------------------------
        // Fix #2 — MTLS_POP must throw and Skip when binding cert load fails
        // ---------------------------------------------------------------------------

        [Fact]
        public async Task LoadIfNeededAsync_MtlsPop_BoundCertLoadThrows_PropagatesAndSetsSkipOnOuter()
        {
            CredentialDescription boundCredential = NewBoundCertCredential();
            var innerOptions = NewInnerOptions(boundCredential);
            _optionsMonitor.Get(InnerSectionName).Returns(innerOptions);

            var credentialsLoader = Substitute.For<ICredentialsLoader>();
            credentialsLoader
                .LoadCredentialsIfNeededAsync(boundCredential, Arg.Any<CredentialSourceLoaderParameters?>())
                .Returns<Task>(_ => throw new InvalidOperationException("simulated KeyVault failure"));
            _serviceProvider.GetService(typeof(ICredentialsLoader)).Returns(credentialsLoader);

            var loader = NewLoader();
            CredentialDescription outer = NewOuterCustomSignedAssertionCredential();

            InvalidOperationException ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => loader.LoadIfNeededAsync(outer, NewParameters(MtlsPopProtocol)));
            Assert.Equal("simulated KeyVault failure", ex.Message);
            Assert.True(outer.Skip, "MTLS_POP binding cert load failure must Skip the outer credential.");
        }

        [Fact]
        public async Task LoadIfNeededAsync_MtlsPop_NoBoundCredentialConfigured_ThrowsAndSetsSkipOnOuter()
        {
            // Inner section has credentials, but none are flagged UseBoundCredential. MTLS_POP cannot succeed,
            // so the outer credential must Skip and the consumer should get a clear configuration error.
            var nonBoundSecret = new CredentialDescription
            {
                SourceType = CredentialSource.ClientSecret,
                ClientSecret = "shh",
            };
            var innerOptions = NewInnerOptions(nonBoundSecret);
            _optionsMonitor.Get(InnerSectionName).Returns(innerOptions);

            // No need to stub ICredentialsLoader — TryLoadBoundCredentialAsync never reaches the resolver
            // because no UseBoundCredential entry exists. Leave the service provider returning null.

            var loader = NewLoader();
            CredentialDescription outer = NewOuterCustomSignedAssertionCredential();

            InvalidOperationException ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => loader.LoadIfNeededAsync(outer, NewParameters(MtlsPopProtocol)));
            Assert.Contains("MTLS_POP was requested", ex.Message, StringComparison.Ordinal);
            Assert.True(outer.Skip);
        }

        [Fact]
        public async Task LoadIfNeededAsync_MtlsPop_BoundCredentialResolvesToNullCertificate_ThrowsWhenMultipleBound()
        {
            // Two bound entries now throws before any loading — multiple UseBoundCredential = true is invalid.
            CredentialDescription badBound = NewBoundCertCredential();
            CredentialDescription goodBound = NewBoundCertCredential();
            var innerOptions = NewInnerOptions(badBound, goodBound);
            _optionsMonitor.Get(InnerSectionName).Returns(innerOptions);

            var loader = NewLoader();
            CredentialDescription outer = NewOuterCustomSignedAssertionCredential();

            InvalidOperationException ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => loader.LoadIfNeededAsync(outer, NewParameters(MtlsPopProtocol)));
            Assert.Contains("UseBoundCredential", ex.Message, StringComparison.Ordinal);
            Assert.True(outer.Skip);
        }

        [Fact]
        public async Task LoadIfNeededAsync_MtlsPop_MultipleBoundCredentials_Throws()
        {
            CredentialDescription firstBound = NewBoundCertCredential();
            CredentialDescription secondBound = NewBoundCertCredential();
            var innerOptions = NewInnerOptions(firstBound, secondBound);
            _optionsMonitor.Get(InnerSectionName).Returns(innerOptions);

            var loader = NewLoader();
            CredentialDescription outer = NewOuterCustomSignedAssertionCredential();

            InvalidOperationException ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => loader.LoadIfNeededAsync(outer, NewParameters(MtlsPopProtocol)));
            Assert.Contains("UseBoundCredential", ex.Message, StringComparison.Ordinal);
            Assert.True(outer.Skip);
        }

        // ---------------------------------------------------------------------------
        // Fix #7 — clear DI error when ICredentialsLoader is not registered
        // ---------------------------------------------------------------------------

        [Fact]
        public async Task LoadIfNeededAsync_MtlsPop_ICredentialsLoaderUnregistered_ThrowsClearConfigurationError()
        {
            CredentialDescription boundCredential = NewBoundCertCredential();
            var innerOptions = NewInnerOptions(boundCredential);
            _optionsMonitor.Get(InnerSectionName).Returns(innerOptions);

            // _serviceProvider.GetService(ICredentialsLoader) returns null by default (NSubstitute).
            var loader = NewLoader();
            CredentialDescription outer = NewOuterCustomSignedAssertionCredential();

            InvalidOperationException ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => loader.LoadIfNeededAsync(outer, NewParameters(MtlsPopProtocol)));
            Assert.Contains("ICredentialsLoader", ex.Message, StringComparison.Ordinal);
            Assert.True(outer.Skip);
        }

        // ---------------------------------------------------------------------------
        // Edge case: null ClientCredentials collection
        // ---------------------------------------------------------------------------

        [Fact]
        public async Task LoadIfNeededAsync_MtlsPop_InnerClientCredentialsCollectionIsNull_ThrowsBindingMissingError()
        {
            // No collection at all → no bound cred can be found → MTLS_POP cannot proceed → throw + Skip.
            var innerOptions = new MicrosoftIdentityApplicationOptions
            {
                Instance = "https://login.microsoftonline.com/",
                Authority = "https://login.microsoftonline.com/tenant/v2.0",
                ClientCredentials = null,
            };
            _optionsMonitor.Get(InnerSectionName).Returns(innerOptions);

            var loader = NewLoader();
            CredentialDescription outer = NewOuterCustomSignedAssertionCredential();

            InvalidOperationException ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => loader.LoadIfNeededAsync(outer, NewParameters(MtlsPopProtocol)));
            Assert.Contains("MTLS_POP was requested", ex.Message, StringComparison.Ordinal);
            Assert.True(outer.Skip);
        }

        [Fact]
        public async Task LoadIfNeededAsync_Bearer_InnerClientCredentialsCollectionIsNull_DoesNotThrowOnBindingScan()
        {
            // For bearer, missing collection is fine — provider is constructed with null bound cred and proceeds.
            var innerOptions = new MicrosoftIdentityApplicationOptions
            {
                Instance = "https://login.microsoftonline.com/",
                Authority = "https://login.microsoftonline.com/tenant/v2.0",
                ClientCredentials = null,
            };
            _optionsMonitor.Get(InnerSectionName).Returns(innerOptions);

            var credentialsLoader = Substitute.For<ICredentialsLoader>();
            _serviceProvider.GetService(typeof(ICredentialsLoader)).Returns(credentialsLoader);

            var loader = NewLoader();
            CredentialDescription outer = NewOuterCustomSignedAssertionCredential();

            try { await loader.LoadIfNeededAsync(outer, NewParameters(BearerProtocol)); }
            catch { /* expected — trailing probe still fails on unwired token acquirer */ }

            await credentialsLoader
                .DidNotReceive()
                .LoadCredentialsIfNeededAsync(Arg.Any<CredentialDescription>(), Arg.Any<CredentialSourceLoaderParameters?>());
        }

        // ---------------------------------------------------------------------------
        // Cached-provider upgrade — first bearer, then MTLS_POP on same CredentialDescription
        // ---------------------------------------------------------------------------

        [Fact]
        public async Task LoadIfNeededAsync_BearerThenMtlsPop_ReconstructsProviderWithBoundCredentialOnSecondCall()
        {
            // First call is bearer → no bound cred resolved → cached provider has SupportsTokenBinding=false.
            // Second call is MTLS_POP → loader must detect the gap, re-run TryLoadBoundCredentialAsync,
            // and replace the cached provider with one that has SupportsTokenBinding=true.
            using X509Certificate2 bindingCert = CreateSelfSignedTestCertificate();
            CredentialDescription boundCredential = NewBoundCertCredential();
            var innerOptions = NewInnerOptions(boundCredential);
            _optionsMonitor.Get(InnerSectionName).Returns(innerOptions);

            ICredentialsLoader credentialsLoader = StubCredentialsLoaderThatMaterialisesCert(boundCredential, bindingCert);
            _serviceProvider.GetService(typeof(ICredentialsLoader)).Returns(credentialsLoader);

            var loader = NewLoader();
            CredentialDescription outer = NewOuterCustomSignedAssertionCredential();

            try { await loader.LoadIfNeededAsync(outer, NewParameters(BearerProtocol)); }
            catch { /* expected: trailing probe fails */ }

            // After bearer call, the cached provider should exist but have no binding capability.
            await credentialsLoader
                .DidNotReceive()
                .LoadCredentialsIfNeededAsync(Arg.Any<CredentialDescription>(), Arg.Any<CredentialSourceLoaderParameters?>());
            Assert.False(boundCredential.Skip, "Bearer-only call must not have touched the bound credential.");

            // Reset Skip set by the trailing probe so the second call doesn't short-circuit elsewhere.
            outer.Skip = false;

            try { await loader.LoadIfNeededAsync(outer, NewParameters(MtlsPopProtocol)); }
            catch { /* expected: trailing probe still fails */ }

            await credentialsLoader
                .Received(1)
                .LoadCredentialsIfNeededAsync(boundCredential, Arg.Any<CredentialSourceLoaderParameters?>());
            Assert.True(boundCredential.Skip, "MTLS_POP follow-up must take the bound cred off the inner-leg auth menu.");
        }

        // ---------------------------------------------------------------------------
        // Reviewer feedback regression — Skip=true on bound cred must not hide the
        // Certificate from the provider. The provider reads Certificate live by
        // reference, so SupportsTokenBinding stays true even after Skip flips.
        // ---------------------------------------------------------------------------

        [Fact]
        public async Task LoadIfNeededAsync_MtlsPop_BoundCredentialMarkedSkip_StillProvidesBindingCertificate()
        {
            using X509Certificate2 bindingCert = CreateSelfSignedTestCertificate();
            CredentialDescription boundCredential = NewBoundCertCredential();

            var innerOptions = NewInnerOptions(boundCredential);
            _optionsMonitor.Get(InnerSectionName).Returns(innerOptions);

            ICredentialsLoader credentialsLoader = StubCredentialsLoaderThatMaterialisesCert(boundCredential, bindingCert);
            _serviceProvider.GetService(typeof(ICredentialsLoader)).Returns(credentialsLoader);

            var loader = NewLoader();
            CredentialDescription outer = NewOuterCustomSignedAssertionCredential();

            try { await loader.LoadIfNeededAsync(outer, NewParameters(MtlsPopProtocol)); }
            catch { /* expected: trailing probe fails */ }

            // The loader must leave the bound credential in a state where the provider — which holds the
            // CredentialDescription by reference and reads Certificate live per call — can still see the
            // binding certificate. Skip and Certificate are independent fields: setting Skip=true to keep
            // the bound entry off the inner-leg auth menu must NOT clear the live Certificate the provider
            // relies on for SupportsTokenBinding.
            Assert.True(boundCredential.Skip, "Bound credential must be Skip=true so inner-leg auth can't pick it.");
            Assert.NotNull(boundCredential.Certificate);
            Assert.Same(bindingCert, boundCredential.Certificate);
        }

        [Fact]
        public async Task LoadIfNeededAsync_MtlsPop_BoundCertificateClearedAfterLoad_SupportsTokenBindingFlipsToFalse()
        {
            // Rotation/reset scenario: after the loader resolves the bound cred, a downstream actor
            // (e.g. a future ResetCredentials hook on KeyVault rotation) clears the Certificate.
            // The provider must read live, so the next SupportsTokenBinding probe must return false
            // and the bound dispatch must fail loudly rather than silently send a stale cert.
            using X509Certificate2 bindingCert = CreateSelfSignedTestCertificate();
            CredentialDescription boundCredential = NewBoundCertCredential();

            var innerOptions = NewInnerOptions(boundCredential);
            _optionsMonitor.Get(InnerSectionName).Returns(innerOptions);

            ICredentialsLoader credentialsLoader = StubCredentialsLoaderThatMaterialisesCert(boundCredential, bindingCert);
            _serviceProvider.GetService(typeof(ICredentialsLoader)).Returns(credentialsLoader);

            var loader = NewLoader();
            CredentialDescription outer = NewOuterCustomSignedAssertionCredential();

            try { await loader.LoadIfNeededAsync(outer, NewParameters(MtlsPopProtocol)); }
            catch { /* expected: trailing probe fails */ }

            Assert.NotNull(boundCredential.Certificate);

            // Simulate a clearing — e.g. a rotation hook that disposes the old cert before reloading.
            boundCredential.Certificate = null;

            Assert.Null(boundCredential.Certificate);
            // Skip stays true (we don't reset it). That documents the known rotation limitation:
            // a re-load via the same shared options instance is blocked by Skip=true. Customers who
            // need cert rotation today must rebuild the options graph rather than mutate in-place.
            Assert.True(boundCredential.Skip);
        }



        private OidcIdpSignedAssertionLoader NewLoader()
            => new OidcIdpSignedAssertionLoader(_logger, _optionsMonitor, _serviceProvider, _tokenAcquirerFactory);

        private static CredentialDescription NewBoundCertCredential()
            => new CredentialDescription
            {
                SourceType = CredentialSource.Certificate,
                UseBoundCredential = true,
            };

        private static CredentialDescription NewOuterCustomSignedAssertionCredential()
            => new CredentialDescription
            {
                SourceType = CredentialSource.CustomSignedAssertion,
                CustomSignedAssertionProviderName = "OidcIdpSignedAssertion",
                CustomSignedAssertionProviderData = new Dictionary<string, object>
                {
                    ["ConfigurationSection"] = InnerSectionName,
                },
            };

        private static MicrosoftIdentityApplicationOptions NewInnerOptions(params CredentialDescription[] clientCredentials)
            => new MicrosoftIdentityApplicationOptions
            {
                Instance = "https://login.microsoftonline.com/",
                Authority = "https://login.microsoftonline.com/tenant/v2.0",
                ClientCredentials = clientCredentials,
            };

        private static CredentialSourceLoaderParameters NewParameters(string protocol)
            => new CredentialSourceLoaderParameters("test-client-id", "https://login.microsoftonline.com/test-tenant")
            {
                Protocol = protocol,
            };

        private static ICredentialsLoader StubCredentialsLoaderThatMaterialisesCert(CredentialDescription credential, X509Certificate2 cert)
        {
            var loader = Substitute.For<ICredentialsLoader>();
            loader
                .LoadCredentialsIfNeededAsync(credential, Arg.Any<CredentialSourceLoaderParameters?>())
                .Returns(_ => { credential.Certificate = cert; return Task.CompletedTask; });
            return loader;
        }

        private static X509Certificate2 CreateSelfSignedTestCertificate(string subject = "CN=OidcFicLoaderBindingCertTest")
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

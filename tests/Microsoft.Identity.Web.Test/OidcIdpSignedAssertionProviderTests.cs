// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
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
    public class OidcIdpSignedAssertionProviderTests
    {
        [Theory]
        [InlineData("https://login.microsoftonline.com/my-tenant-id/oauth2/v2.0/token", "https://login.microsoftonline.com/", "my-tenant-id")]
        [InlineData("https://login.microsoftonline.com/contoso.onmicrosoft.com/oauth2/v2.0/token", "https://login.microsoftonline.com", "contoso.onmicrosoft.com")]
        [InlineData("https://login.microsoftonline.com/12345678-1234-1234-1234-123456789abc/oauth2/v2.0/token", "https://login.microsoftonline.com/", "12345678-1234-1234-1234-123456789abc")]
        public void ExtractTenantFromTokenEndpointIfSameInstance_SameInstance_ReturnsTenant(
            string tokenEndpoint,
            string configuredInstance,
            string expectedTenant)
        {
            // Act
            var result = OidcIdpSignedAssertionProvider.ExtractTenantFromTokenEndpointIfSameInstance(
                tokenEndpoint,
                configuredInstance);

            // Assert
            Assert.Equal(expectedTenant, result);
        }

        [Theory]
        [InlineData("https://login.microsoftonline.us/my-tenant-id/oauth2/v2.0/token", "https://login.microsoftonline.com/", null)]
        [InlineData("https://login.microsoftonline.com/my-tenant-id/oauth2/v2.0/token", "https://login.microsoftonline.us/", null)]
        [InlineData("https://login.chinacloudapi.cn/my-tenant-id/oauth2/v2.0/token", "https://login.microsoftonline.com/", null)]
        public void ExtractTenantFromTokenEndpointIfSameInstance_DifferentInstance_ReturnsNull(
            string tokenEndpoint,
            string configuredInstance,
            string? expectedResult)
        {
            // Act
            var result = OidcIdpSignedAssertionProvider.ExtractTenantFromTokenEndpointIfSameInstance(
                tokenEndpoint,
                configuredInstance);

            // Assert
            Assert.Equal(expectedResult, result);
        }

        [Theory]
        [InlineData(null, "https://login.microsoftonline.com/")]
        [InlineData("", "https://login.microsoftonline.com/")]
        [InlineData("https://login.microsoftonline.com/tenant/oauth2/v2.0/token", null)]
        [InlineData("https://login.microsoftonline.com/tenant/oauth2/v2.0/token", "")]
        [InlineData(null, null)]
        [InlineData("", "")]
        public void ExtractTenantFromTokenEndpointIfSameInstance_NullOrEmptyInputs_ReturnsNull(
            string? tokenEndpoint,
            string? configuredInstance)
        {
            // Act
            var result = OidcIdpSignedAssertionProvider.ExtractTenantFromTokenEndpointIfSameInstance(
                tokenEndpoint,
                configuredInstance);

            // Assert
            Assert.Null(result);
        }

        [Theory]
        [InlineData("not-a-valid-uri", "https://login.microsoftonline.com/")]
        [InlineData("https://login.microsoftonline.com/tenant/oauth2/v2.0/token", "not-a-valid-uri")]
        public void ExtractTenantFromTokenEndpointIfSameInstance_InvalidUri_ReturnsNull(
            string tokenEndpoint,
            string configuredInstance)
        {
            // Act
            var result = OidcIdpSignedAssertionProvider.ExtractTenantFromTokenEndpointIfSameInstance(
                tokenEndpoint,
                configuredInstance);

            // Assert
            Assert.Null(result);
        }

        [Theory]
        [InlineData("https://login.microsoftonline.com/oauth2/v2.0/token", "https://login.microsoftonline.com/")]
        [InlineData("https://login.microsoftonline.com/", "https://login.microsoftonline.com/")]
        public void ExtractTenantFromTokenEndpointIfSameInstance_NoTenantInPath_ReturnsNull(
            string tokenEndpoint,
            string configuredInstance)
        {
            // Act
            var result = OidcIdpSignedAssertionProvider.ExtractTenantFromTokenEndpointIfSameInstance(
                tokenEndpoint,
                configuredInstance);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void ExtractTenantFromTokenEndpointIfSameInstance_ValidatesOAuth2Pattern()
        {
            // Arrange
            // This endpoint has a tenant but no oauth2 segment - should return null
            var tokenEndpoint = "https://login.microsoftonline.com/my-tenant/some-other-path";
            var configuredInstance = "https://login.microsoftonline.com/";

            // Act
            var result = OidcIdpSignedAssertionProvider.ExtractTenantFromTokenEndpointIfSameInstance(
                tokenEndpoint,
                configuredInstance);

            // Assert
            Assert.Null(result);
        }

        #region Token binding (mTLS PoP) tests

        private const string SameCloudInstance = "https://login.microsoftonline.com/";
        private const string TokenBindingParameterName = "IsTokenBinding";

        private static X509Certificate2 CreateSelfSignedCertificate()
        {
            using RSA rsa = RSA.Create(2048);
            var request = new CertificateRequest(
                "CN=OidcFicBindingTest",
                rsa,
                HashAlgorithmName.SHA256,
                RSASignaturePadding.Pkcs1);

            return request.CreateSelfSigned(
                DateTimeOffset.UtcNow.AddDays(-1),
                DateTimeOffset.UtcNow.AddDays(1));
        }

        private static AcquireTokenResult CreateResult(
            string accessToken,
            X509Certificate2? bindingCertificate = null,
            string tokenType = "Bearer")
        {
            return new AcquireTokenResult(
                accessToken,
                DateTimeOffset.UtcNow.AddHours(1),
                "inner-tenant",
                "inner-id-token",
                new[] { "api://AzureADTokenExchange/.default" },
                Guid.NewGuid(),
                tokenType)
            {
                BindingCertificate = bindingCertificate!,
            };
        }

        private static (OidcIdpSignedAssertionProvider provider, ITokenAcquirer acquirer) CreateProvider(
            MicrosoftIdentityApplicationOptions? options = null,
            string? tokenExchangeUrl = null)
        {
            var acquirer = Substitute.For<ITokenAcquirer>();
            var factory = Substitute.For<ITokenAcquirerFactory>();
            options ??= new MicrosoftIdentityApplicationOptions { Instance = SameCloudInstance };
            factory.GetTokenAcquirer(Arg.Any<IdentityApplicationOptions>()).Returns(acquirer);
            var provider = new OidcIdpSignedAssertionProvider(factory, options, tokenExchangeUrl, logger: null);
            return (provider, acquirer);
        }

        /// <summary>
        /// Configures the acquirer to return <paramref name="result"/> and captures the
        /// <see cref="AcquireTokenOptions"/> and <see cref="CancellationToken"/> the provider passes in.
        /// </summary>
        private static Capture SetupAcquirer(ITokenAcquirer acquirer, AcquireTokenResult result)
        {
            var capture = new Capture();
            acquirer.GetTokenForAppAsync(
                    Arg.Any<string>(),
                    Arg.Any<AcquireTokenOptions>(),
                    Arg.Any<CancellationToken>())
                .Returns(callInfo =>
                {
                    capture.Scope = callInfo.ArgAt<string>(0);
                    capture.Options = callInfo.ArgAt<AcquireTokenOptions>(1);
                    capture.CancellationToken = callInfo.ArgAt<CancellationToken>(2);
                    return Task.FromResult(result);
                });
            return capture;
        }

        private sealed class Capture
        {
            public string? Scope { get; set; }
            public AcquireTokenOptions? Options { get; set; }
            public CancellationToken CancellationToken { get; set; }
        }

        [Fact]
        public void SupportsTokenBinding_ReturnsTrue()
        {
            var (provider, _) = CreateProvider();
            Assert.True(provider.SupportsTokenBinding);
        }

        [Fact]
        public async Task GetSignedAssertionWithBindingAsync_RequestsInnerTokenBinding()
        {
            var (provider, acquirer) = CreateProvider();
            var capture = SetupAcquirer(acquirer, CreateResult("assertion", CreateSelfSignedCertificate()));

            await provider.GetSignedAssertionWithBindingAsync(new AssertionRequestOptions());

            Assert.NotNull(capture.Options);
            Assert.NotNull(capture.Options!.ExtraParameters);
            Assert.True(capture.Options.ExtraParameters!.TryGetValue(TokenBindingParameterName, out object? value));
            Assert.IsType<bool>(value);
            Assert.True((bool)value!);
        }

        [Fact]
        public async Task GetSignedAssertionWithBindingAsync_ReturnsAccessTokenAndExactCertificate()
        {
            var (provider, acquirer) = CreateProvider();
            X509Certificate2 certificate = CreateSelfSignedCertificate();
            AcquireTokenResult acquireTokenResult = CreateResult("the-access-token", certificate);
            SetupAcquirer(acquirer, acquireTokenResult);

            ClientSignedAssertion? result = await provider.GetSignedAssertionWithBindingAsync(new AssertionRequestOptions());

            Assert.NotNull(result);
            Assert.Equal(acquireTokenResult.AccessToken, result!.Assertion);
            Assert.Same(acquireTokenResult.BindingCertificate, result.TokenBindingCertificate);
        }

        [Fact]
        public async Task GetSignedAssertionWithBindingAsync_MissingCertificateThrows()
        {
            var (provider, acquirer) = CreateProvider();
            SetupAcquirer(acquirer, CreateResult("assertion", bindingCertificate: null));

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => provider.GetSignedAssertionWithBindingAsync(new AssertionRequestOptions()));
        }

        [Fact]
        public async Task GetSignedAssertionWithBindingAsync_MissingAccessTokenThrows()
        {
            var (provider, acquirer) = CreateProvider();
            SetupAcquirer(acquirer, CreateResult(string.Empty, CreateSelfSignedCertificate()));

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => provider.GetSignedAssertionWithBindingAsync(new AssertionRequestOptions()));
        }

        [Fact]
        public async Task GetSignedAssertionWithBindingAsync_PropagatesCancellationToken()
        {
            var (provider, acquirer) = CreateProvider();
            var capture = SetupAcquirer(acquirer, CreateResult("assertion", CreateSelfSignedCertificate()));

            using var cts = new CancellationTokenSource();
            await provider.GetSignedAssertionWithBindingAsync(new AssertionRequestOptions(), cts.Token);

            Assert.Equal(cts.Token, capture.CancellationToken);
        }

        [Fact]
        public async Task GetSignedAssertionWithBindingAsync_PropagatesCorrelationId()
        {
            var (provider, acquirer) = CreateProvider();
            var capture = SetupAcquirer(acquirer, CreateResult("assertion", CreateSelfSignedCertificate()));

            Guid correlationId = Guid.NewGuid();
            await provider.GetSignedAssertionWithBindingAsync(
                new AssertionRequestOptions { CorrelationId = correlationId });

            Assert.NotNull(capture.Options);
            Assert.Equal(correlationId, capture.Options!.CorrelationId);
        }

        [Fact]
        public async Task GetSignedAssertionWithBindingAsync_PropagatesFmiPath()
        {
            var (provider, acquirer) = CreateProvider();
            var capture = SetupAcquirer(acquirer, CreateResult("assertion", CreateSelfSignedCertificate()));

            await provider.GetSignedAssertionWithBindingAsync(
                new AssertionRequestOptions { ClientAssertionFmiPath = "my/fmi/path" });

            Assert.NotNull(capture.Options);
            Assert.Equal("my/fmi/path", capture.Options!.FmiPath);
        }

        [Fact]
        public async Task GetSignedAssertionWithBindingAsync_SameCloudDifferentInnerAndOuterTenantsWithoutFmiPath_DoesNotOverrideInnerTenant()
        {
            // Inner application configured for tenant t1; the outer request is for tenant t2 on the SAME
            // cloud host, with no FMI path. The outer tenant must NOT override the inner tenant —
            // otherwise the inner application would be asked to acquire its assertion from the outer's
            // tenant t2 instead of its own configured tenant t1.
            var (provider, acquirer) = CreateProvider(
                new MicrosoftIdentityApplicationOptions { Instance = SameCloudInstance, TenantId = "t1" });
            var capture = SetupAcquirer(acquirer, CreateResult("assertion", CreateSelfSignedCertificate()));

            await provider.GetSignedAssertionWithBindingAsync(new AssertionRequestOptions
            {
                TokenEndpoint = "https://login.microsoftonline.com/t2/oauth2/v2.0/token",
                TenantId = "t2",
            });

            Assert.NotNull(capture.Options); // non-null because IsTokenBinding is set
            Assert.Null(capture.Options!.Tenant);
        }

        [Fact]
        public async Task GetSignedAssertionWithBindingAsync_PropagatesSameCloudTenantWithFmiPath()
        {
            var (provider, acquirer) = CreateProvider(
                new MicrosoftIdentityApplicationOptions { Instance = SameCloudInstance });
            var capture = SetupAcquirer(acquirer, CreateResult("assertion", CreateSelfSignedCertificate()));

            // With an FMI path, the same-cloud outer tenant is propagated from the token endpoint.
            await provider.GetSignedAssertionWithBindingAsync(new AssertionRequestOptions
            {
                ClientAssertionFmiPath = "my/fmi/path",
                TokenEndpoint = "https://login.microsoftonline.com/outer-tenant/oauth2/v2.0/token",
            });

            Assert.NotNull(capture.Options);
            Assert.Equal("outer-tenant", capture.Options!.Tenant);
            Assert.Equal("my/fmi/path", capture.Options.FmiPath);
        }

        [Fact]
        public async Task GetSignedAssertionWithBindingAsync_UsesSameCloudTenantIdWithFmiPathWhenEndpointNotParsable()
        {
            var (provider, acquirer) = CreateProvider(
                new MicrosoftIdentityApplicationOptions { Instance = SameCloudInstance });
            var capture = SetupAcquirer(acquirer, CreateResult("assertion", CreateSelfSignedCertificate()));

            // FMI path over an mtlsauth endpoint the token-endpoint parser cannot read: the authoritative
            // same-cloud TenantId is used as the fallback.
            await provider.GetSignedAssertionWithBindingAsync(new AssertionRequestOptions
            {
                ClientAssertionFmiPath = "my/fmi/path",
                Authority = "https://login.microsoftonline.com/outer-tenant/",
                TokenEndpoint = "https://mtlsauth.microsoft.com/outer-tenant/oauth2/v2.0/token",
                TenantId = "outer-tenant",
            });

            Assert.NotNull(capture.Options);
            Assert.Equal("outer-tenant", capture.Options!.Tenant);
        }

        [Fact]
        public async Task GetSignedAssertionWithBindingAsync_IgnoresCrossCloudTenantOverrideWithFmiPath()
        {
            var (provider, acquirer) = CreateProvider(
                new MicrosoftIdentityApplicationOptions { Instance = SameCloudInstance });
            var capture = SetupAcquirer(acquirer, CreateResult("assertion", CreateSelfSignedCertificate()));

            // Even with an FMI path, a cross-cloud (.us vs .com) outer tenant must not override the
            // inner tenant.
            await provider.GetSignedAssertionWithBindingAsync(new AssertionRequestOptions
            {
                ClientAssertionFmiPath = "my/fmi/path",
                Authority = "https://login.microsoftonline.us/outer-tenant/",
                TokenEndpoint = "https://login.microsoftonline.us/outer-tenant/oauth2/v2.0/token",
                TenantId = "outer-tenant",
            });

            Assert.NotNull(capture.Options);
            Assert.Null(capture.Options!.Tenant);
        }

        [Fact]
        public async Task GetSignedAssertionAsync_DoesNotSetIsTokenBinding()
        {
            var (provider, acquirer) = CreateProvider();
            var capture = SetupAcquirer(acquirer, CreateResult("assertion"));

            // Provide a correlation id so AcquireTokenOptions is materialized and we can assert on it.
            await provider.GetSignedAssertionAsync(new AssertionRequestOptions { CorrelationId = Guid.NewGuid() });

            Assert.NotNull(capture.Options);
            Assert.False(
                capture.Options!.ExtraParameters != null &&
                capture.Options.ExtraParameters.ContainsKey(TokenBindingParameterName));
        }

        [Fact]
        public async Task GetSignedAssertionAsync_PreservesExistingBearerBehavior()
        {
            var (provider, acquirer) = CreateProvider();
            SetupAcquirer(acquirer, CreateResult("bearer-assertion"));

            string assertion = await provider.GetSignedAssertionAsync(new AssertionRequestOptions());

            Assert.Equal("bearer-assertion", assertion);
        }

        [Fact]
        public async Task GetSignedAssertionWithBindingAsync_UsesCertificateFromEveryFreshResult()
        {
            var (provider, acquirer) = CreateProvider();

            X509Certificate2 cert1 = CreateSelfSignedCertificate();
            X509Certificate2 cert2 = CreateSelfSignedCertificate();
            var results = new Queue<AcquireTokenResult>(new[]
            {
                CreateResult("assertion-1", cert1),
                CreateResult("assertion-2", cert2),
            });

            acquirer.GetTokenForAppAsync(
                    Arg.Any<string>(),
                    Arg.Any<AcquireTokenOptions>(),
                    Arg.Any<CancellationToken>())
                .Returns(_ => Task.FromResult(results.Dequeue()));

            ClientSignedAssertion? first = await provider.GetSignedAssertionWithBindingAsync(new AssertionRequestOptions());
            ClientSignedAssertion? second = await provider.GetSignedAssertionWithBindingAsync(new AssertionRequestOptions());

            Assert.Equal("assertion-1", first!.Assertion);
            Assert.Same(cert1, first.TokenBindingCertificate);
            Assert.Equal("assertion-2", second!.Assertion);
            Assert.Same(cert2, second.TokenBindingCertificate);
        }

        [Fact]
        public async Task GetSignedAssertionWithBindingAsync_InnerMsiFicResult_PropagatesCertificateForThreeLegFlow()
        {
            // Simulates the three-leg composition: the inner ITokenAcquirer represents an
            // MSI-FIC-backed acquisition that returns an OIDC assertion + a dynamically created
            // binding certificate. No separate certificate is configured on the provider.
            var (provider, acquirer) = CreateProvider();

            X509Certificate2 dynamicallyCreatedCertificate = CreateSelfSignedCertificate();
            AcquireTokenResult innerMsiFicResult = CreateResult(
                "oidc-assertion-produced-from-msi-fic",
                dynamicallyCreatedCertificate,
                tokenType: "mtls_pop");
            SetupAcquirer(acquirer, innerMsiFicResult);

            ClientSignedAssertion? returned = await provider.GetSignedAssertionWithBindingAsync(new AssertionRequestOptions());

            Assert.NotNull(returned);
            Assert.Equal("oidc-assertion-produced-from-msi-fic", returned!.Assertion);
            Assert.Same(dynamicallyCreatedCertificate, returned.TokenBindingCertificate);
        }

        #endregion
    }
}

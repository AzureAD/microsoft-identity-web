// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Extensibility;
using Microsoft.Identity.Web.Test.Common;
using Microsoft.Identity.Web.Test.Common.Mocks;
using Microsoft.Identity.Web.TestOnly;
using Microsoft.IdentityModel.Tokens;
using NSubstitute;
using Xunit;
using TC = Microsoft.Identity.Web.Test.Common.TestConstants;

namespace Microsoft.Identity.Web.Test
{
    /// <summary>
    /// Surfaces MSAL PR #6009 (mTLS bearer transport extended to the user flows) through
    /// Microsoft.Identity.Web. When a certificate credential opts into bound credentials
    /// (<see cref="CredentialDescription.UseBoundCredential"/> == <c>true</c>), idweb wires
    /// <c>WithCertificate(cert, new CertificateOptions { SendCertificateOverMtls = true })</c>.
    /// The final token stays a Bearer token, but the client credential is presented over mTLS,
    /// so the token request must be routed to the mTLS token endpoint (<c>mtlsauth.*</c>) for the
    /// user flows too — On-Behalf-Of and refresh_token — not just the app (client credentials) flow.
    /// </summary>
    public class MtlsBearerUserFlowTests
    {
        private const string MtlsEndpointHost = "mtlsauth.microsoft.com";
        private const string RegularEndpointHost = "login.microsoftonline.com";
        private static readonly IEnumerable<string> s_scopes = new[] { "user.read" };

        [Fact]
        public async Task OnBehalfOf_BoundCertificate_RoutesTokenRequestOverMtlsEndpointAsync()
        {
            // Arrange
            using var httpFactory = new MockMtlsHttpClientFactory();
            MockHttpMessageHandler tokenHandler = httpFactory.AddMockHandler(
                MockHttpCreator.CreateLrOboTokenHandler("user.read"));
            IConfidentialClientApplication app = await BuildConfidentialClientAsync(httpFactory, useBoundCredential: true);

            // Act
            AuthenticationResult result = await app.AcquireTokenOnBehalfOf(
                s_scopes,
                new UserAssertion("fake-upstream-user-access-token"))
                .ExecuteAsync();

            // Assert — the OBO token request routed over the mTLS endpoint, and the bound
            // certificate was presented as the client credential (client_assertion).
            Assert.Contains(MtlsEndpointHost, result.AuthenticationResultMetadata.TokenEndpoint, System.StringComparison.Ordinal);
            Assert.NotNull(tokenHandler.ActualRequestPostData);
            Assert.True(tokenHandler.ActualRequestPostData.ContainsKey("client_assertion"));
        }

        [Fact]
        public async Task RefreshToken_BoundCertificate_RoutesTokenRequestOverMtlsEndpointAsync()
        {
            // Arrange
            using var httpFactory = new MockMtlsHttpClientFactory();
            MockHttpMessageHandler tokenHandler = httpFactory.AddMockHandler(
                MockHttpCreator.CreateLrOboTokenHandler("user.read"));
            IConfidentialClientApplication app = await BuildConfidentialClientAsync(httpFactory, useBoundCredential: true);

            // Act
            AuthenticationResult result = await ((IByRefreshToken)app).AcquireTokenByRefreshToken(
                s_scopes,
                "fake-refresh-token")
                .ExecuteAsync();

            // Assert
            Assert.Contains(MtlsEndpointHost, result.AuthenticationResultMetadata.TokenEndpoint, System.StringComparison.Ordinal);
            Assert.NotNull(tokenHandler.ActualRequestPostData);
            Assert.True(tokenHandler.ActualRequestPostData.ContainsKey("client_assertion"));
        }

        [Fact]
        public async Task AuthorizationCode_BoundCertificate_RoutesTokenRequestOverMtlsEndpointAsync()
        {
            // Arrange
            using var httpFactory = new MockMtlsHttpClientFactory();
            MockHttpMessageHandler tokenHandler = httpFactory.AddMockHandler(
                MockHttpCreator.CreateLrOboTokenHandler("user.read"));
            IConfidentialClientApplication app = await BuildConfidentialClientAsync(httpFactory, useBoundCredential: true);

            // Act
            AuthenticationResult result = await app.AcquireTokenByAuthorizationCode(
                s_scopes,
                "fake-authorization-code")
                .ExecuteAsync();

            // Assert
            Assert.Contains(MtlsEndpointHost, result.AuthenticationResultMetadata.TokenEndpoint, System.StringComparison.Ordinal);
            Assert.NotNull(tokenHandler.ActualRequestPostData);
            Assert.True(tokenHandler.ActualRequestPostData.ContainsKey("client_assertion"));
        }

        [Fact]
        public async Task OnBehalfOf_UnboundCertificate_UsesRegularEndpointAsync()
        {
            // Arrange — control: an otherwise-identical certificate credential that is NOT bound
            // must keep the classic (non-mTLS) token endpoint.
            using var httpFactory = new MockMtlsHttpClientFactory();
            httpFactory.AddMockHandler(MockHttpCreator.CreateLrOboTokenHandler("user.read"));
            IConfidentialClientApplication app = await BuildConfidentialClientAsync(httpFactory, useBoundCredential: false);

            // Act
            AuthenticationResult result = await app.AcquireTokenOnBehalfOf(
                s_scopes,
                new UserAssertion("fake-upstream-user-access-token"))
                .ExecuteAsync();

            // Assert
            Assert.Contains(RegularEndpointHost, result.AuthenticationResultMetadata.TokenEndpoint, System.StringComparison.Ordinal);
            Assert.DoesNotContain("mtls", result.AuthenticationResultMetadata.TokenEndpoint, System.StringComparison.Ordinal);
        }

        private static async Task<IConfidentialClientApplication> BuildConfidentialClientAsync(
            MockMtlsHttpClientFactory httpFactory,
            bool useBoundCredential)
        {
            X509Certificate2 testCertificate = Base64EncodedCertificateLoader.LoadFromBase64Encoded(
                TC.CertificateX5cWithPrivateKey,
                TC.CertificateX5cWithPrivateKeyPassword,
                X509KeyStorageFlags.DefaultKeySet);

            ICredentialsLoader credLoader = Substitute.For<ICredentialsLoader>();
            credLoader.LoadCredentialsIfNeededAsync(Arg.Any<CredentialDescription>(), Arg.Any<CredentialSourceLoaderParameters>())
                .Returns(args =>
                {
                    ((CredentialDescription)args[0]!).Certificate = testCertificate;
                    return Task.CompletedTask;
                });

            CredentialDescription credentialDescription = new()
            {
                SourceType = CredentialSource.StoreWithThumbprint,
                CertificateThumbprint = "test-thumbprint",
                CertificateStorePath = "CurrentUser/My",
                UseBoundCredential = useBoundCredential,
            };

            ConfidentialClientApplicationBuilder builder = ConfidentialClientApplicationBuilder
                .Create(TC.ConfidentialClientId)
                .WithAuthority(TC.AuthorityWithTenantSpecifiedWithV2)
                .WithRedirectUri("https://localhost")
                .WithExperimentalFeatures()
                .WithHttpClientFactory(httpFactory);

            // Route through idweb's credential wiring. For a certificate credential with
            // UseBoundCredential == true this calls
            // WithCertificate(cert, new CertificateOptions { SendCertificateOverMtls = true }).
            await builder.WithClientCredentialsAsync(
                new MergedOptions
                {
                    ClientCredentials = new[] { credentialDescription },
                },
                new CredentialsProvider(Substitute.For<ILogger<CredentialsProvider>>(), credLoader, [], null),
                credentialSourceLoaderParameters: null,
                isTokenBinding: false);

            return builder.Build();
        }
    }

    /// <summary>
    /// Companion to <see cref="MtlsBearerUserFlowTests"/> that exercises the same bound-credential user
    /// flow through Microsoft.Identity.Web's public consumer surface,
    /// <see cref="IAuthorizationHeaderProvider.CreateAuthorizationHeaderForUserAsync"/> — the user
    /// counterpart of <c>CreateAuthorizationHeaderForAppAsync</c>. Whereas the sibling class asserts at
    /// the MSAL boundary, this drives the full IdWeb stack (header provider →
    /// <see cref="ITokenAcquisition"/> → confidential client). It proves that when a certificate
    /// credential opts into <see cref="CredentialDescription.UseBoundCredential"/>, the user
    /// (on-behalf-of) token request is routed over the mTLS endpoint (<c>mtlsauth.*</c>) and presents
    /// the bound certificate as the client credential, while the header returned to the caller stays a
    /// plain bearer token.
    /// </summary>
    [Collection(nameof(TokenAcquirerFactorySingletonProtection))]
    public class MtlsBearerUserFlowHeaderProviderTests
    {
        private const string MtlsEndpointHost = "mtlsauth.microsoft.com";
        private const string RegularEndpointHost = "login.microsoftonline.com";
        private const string TenantId = "10c419d4-4a50-45b2-aa4e-919fb84df24f";
        private static readonly IEnumerable<string> s_scopes = new[] { "user.read" };

        [Fact]
        public async Task CreateAuthorizationHeaderForUserAsync_BoundCertificate_RoutesOboRequestOverMtlsEndpointAsync()
        {
            // Arrange
            using var mtlsFactory = new MockMtlsHttpClientFactory();
            MockHttpMessageHandler tokenHandler = mtlsFactory.AddMockHandler(
                MockHttpCreator.CreateLrOboTokenHandler("user.read"));
            IServiceProvider serviceProvider = BuildStack(mtlsFactory, useBoundCredential: true);

            IAuthorizationHeaderProvider headerProvider =
                serviceProvider.GetRequiredService<IAuthorizationHeaderProvider>();

            // Act — enter through the public "GetHeaderForUser" surface with an on-behalf-of principal.
            string header = await headerProvider.CreateAuthorizationHeaderForUserAsync(
                s_scopes,
                CreateLongRunningOboOptions(),
                CreatePrincipalWithBootstrapToken());

            // Assert — the header handed to the caller is a plain bearer token, but the user (OBO) token
            // request routed over the mTLS endpoint and presented the bound certificate (client_assertion).
            Assert.StartsWith("Bearer ", header, StringComparison.Ordinal);
            Assert.NotNull(tokenHandler.ActualRequestMessage);
            Assert.Contains(MtlsEndpointHost, tokenHandler.ActualRequestMessage.RequestUri!.AbsoluteUri, StringComparison.Ordinal);
            Assert.NotNull(tokenHandler.ActualRequestPostData);
            Assert.True(tokenHandler.ActualRequestPostData.ContainsKey("client_assertion"));
        }

        [Fact]
        public async Task CreateAuthorizationHeaderForUserAsync_UnboundCertificate_UsesRegularEndpointAsync()
        {
            // Arrange — control: an otherwise-identical certificate credential that is NOT bound must
            // keep the classic (non-mTLS) token endpoint.
            using var mtlsFactory = new MockMtlsHttpClientFactory();
            MockHttpMessageHandler tokenHandler = mtlsFactory.AddMockHandler(
                MockHttpCreator.CreateLrOboTokenHandler("user.read"));
            IServiceProvider serviceProvider = BuildStack(mtlsFactory, useBoundCredential: false);

            IAuthorizationHeaderProvider headerProvider =
                serviceProvider.GetRequiredService<IAuthorizationHeaderProvider>();

            // Act
            string header = await headerProvider.CreateAuthorizationHeaderForUserAsync(
                s_scopes,
                CreateLongRunningOboOptions(),
                CreatePrincipalWithBootstrapToken());

            // Assert
            Assert.StartsWith("Bearer ", header, StringComparison.Ordinal);
            Assert.NotNull(tokenHandler.ActualRequestMessage);
            Assert.Contains(RegularEndpointHost, tokenHandler.ActualRequestMessage.RequestUri!.AbsoluteUri, StringComparison.Ordinal);
            Assert.DoesNotContain("mtls", tokenHandler.ActualRequestMessage.RequestUri!.AbsoluteUri, StringComparison.Ordinal);
        }

        private static IServiceProvider BuildStack(
            MockMtlsHttpClientFactory mtlsFactory,
            bool useBoundCredential)
        {
            X509Certificate2 testCertificate = Base64EncodedCertificateLoader.LoadFromBase64Encoded(
                TC.CertificateX5cWithPrivateKey,
                TC.CertificateX5cWithPrivateKeyPassword,
                X509KeyStorageFlags.DefaultKeySet);

            TokenAcquirerFactoryTesting.ResetTokenAcquirerFactoryInTest();
            TokenAcquirerFactory tokenAcquirerFactory = TokenAcquirerFactory.GetDefaultInstance();
            tokenAcquirerFactory.Services.Configure<MicrosoftIdentityApplicationOptions>(options =>
            {
                options.Instance = "https://login.microsoftonline.com/";
                options.TenantId = TenantId;
                options.ClientId = TC.ConfidentialClientId;
                options.ClientCredentials = new[]
                {
                    new CredentialDescription
                    {
                        SourceType = CredentialSource.Certificate,
                        Certificate = testCertificate,
                        UseBoundCredential = useBoundCredential,
                    },
                };
            });

            // Register the mock mTLS factory as MSAL's HTTP client factory. Because
            // IMsalMtlsHttpClientFactory extends IMsalHttpClientFactory, IdWeb resolves this single
            // instance (via IMsalHttpClientFactory) and MSAL uses its GetHttpClient(certificate)
            // overload for the mTLS leg when the bound certificate is presented.
            tokenAcquirerFactory.Services.AddSingleton<IMsalHttpClientFactory>(mtlsFactory);
            tokenAcquirerFactory.Services.AddScoped<IAuthorizationHeaderProvider, DefaultAuthorizationHeaderProvider>();

            return tokenAcquirerFactory.Build();
        }

        private static AuthorizationHeaderProviderOptions CreateLongRunningOboOptions() =>
            new()
            {
                AcquireTokenOptions = new AcquireTokenOptions
                {
                    // Auto makes the first call initiate a long-running on-behalf-of session keyed off
                    // the incoming user assertion.
                    LongRunningWebApiSessionKey = TokenAcquisitionOptions.LongRunningWebApiSessionKeyAuto,
                },
            };

        /// <summary>
        /// Builds a <see cref="ClaimsPrincipal"/> whose identity carries a bootstrap token (an unsecured
        /// JWT string). IdWeb reads it via <c>GetBootstrapToken()</c> and performs an on-behalf-of token
        /// request using it as the user assertion.
        /// </summary>
        private static ClaimsPrincipal CreatePrincipalWithBootstrapToken()
        {
            string header = Base64UrlEncoder.Encode("{\"alg\":\"none\",\"typ\":\"JWT\"}");
            string payload = Base64UrlEncoder.Encode(
                "{\"aud\":\"https://graph.microsoft.com\"," +
                "\"iss\":\"https://login.microsoftonline.com/" + TenantId + "/v2.0\"," +
                "\"oid\":\"" + TC.Uid + "\"," +
                "\"tid\":\"" + TenantId + "\"," +
                "\"sub\":\"" + TC.Uid + "\"}");
            string bootstrapJwt = header + "." + payload + ".";

            var identity = new CaseSensitiveClaimsIdentity(
                new[]
                {
                    new Claim("oid", TC.Uid),
                    new Claim("tid", TC.Utid),
                },
                authenticationType: "Bearer")
            {
                BootstrapContext = bootstrapJwt,
            };

            return new ClaimsPrincipal(identity);
        }
    }
}

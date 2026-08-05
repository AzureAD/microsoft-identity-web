// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Extensibility;
using Microsoft.Identity.Web.Test.Common.Mocks;
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
}

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.Graph;
using Microsoft.Identity.Client;
using Microsoft.Identity.Web.Test.Common;
using Microsoft.Identity.Web.Test.Common.Mocks;
using Xunit;

namespace Microsoft.Identity.Web.Test
{
    public class MsAuth10AtPopTests
    {
        [Fact]
        public async Task MsAuth10AtPop_WithAtPop_ShouldPopulateBuilderWithProofOfPosessionKeyIdAndOnBeforeTokenRequestTestAsync()
        {
            // Arrange
            using MockHttpClientFactory mockHttpClientFactory = new MockHttpClientFactory();
            using var httpTokenRequest = MockHttpCreator.CreateClientCredentialTokenHandler();
            //mockHttpClientFactory.AddMockHandler(MockHttpCreator.CreateInstanceDiscoveryMockHandler());
            mockHttpClientFactory.AddMockHandler(httpTokenRequest);

            //Enables the mock handler to requeue requests that have been intercepted for instance discovery for example
            httpTokenRequest.ReplaceMockHttpMessageHandler = mockHttpClientFactory.AddMockHandler;

            var certificateDescription = CertificateDescription.FromBase64Encoded(
                TestConstants.CertificateX5cWithPrivateKey,
                TestConstants.CertificateX5cWithPrivateKeyPassword);
            ICertificateLoader loader = new DefaultCertificateLoader();
            loader.LoadIfNeeded(certificateDescription);

            Assert.NotNull(certificateDescription.Certificate);

            var app = ConfidentialClientApplicationBuilder.Create(TestConstants.ClientId)
                            .WithExperimentalFeatures()
                            .WithCertificate(certificateDescription.Certificate)
                            .WithHttpClientFactory(mockHttpClientFactory)
                            .Build();

            var popPublicKey = "pop_key";
            var jwkClaim = "jwk_claim";

            // Act
            AuthenticationResult result = await app.AcquireTokenForClient(new[] { TestConstants.Scopes })
                .WithAtPop(certificateDescription.Certificate, popPublicKey, jwkClaim, TestConstants.ClientId, true)
                .ExecuteAsync();

            // Assert
            httpTokenRequest.ActualRequestPostData.TryGetValue("request", out string? request);
            Assert.NotNull(request);
            httpTokenRequest.ActualRequestPostData.TryGetValue("client_assertion", out string? clientAssertion);
            Assert.Null(clientAssertion);

            JwtSecurityTokenHandler jwtSecurityTokenHandler = new JwtSecurityTokenHandler();
            JwtSecurityToken assertion = jwtSecurityTokenHandler.ReadJwtToken(request);

            Assert.Equal("https://login.microsoftonline.com/common/oauth2/v2.0/token", assertion.Claims.Single(c => c.Type == "aud").Value);
            Assert.Equal(TestConstants.ClientId, assertion.Claims.Single(c => c.Type == "iss").Value);
            Assert.Equal(TestConstants.ClientId, assertion.Claims.Single(c => c.Type == "sub").Value);
            Assert.NotEmpty(assertion.Claims.Single(c => c.Type == "jti").Value);
            Assert.Equal(jwkClaim, assertion.Claims.Single(c => c.Type == "pop_jwk").Value);

            assertion.Header.TryGetValue("x5c", out var x5cClaimValue);
            Assert.NotNull(x5cClaimValue);
            string actualX5c = (string)((List<object>)x5cClaimValue).Single();

            string expectedX5C= Convert.ToBase64String(certificateDescription.Certificate.RawData);

            Assert.Equal(expectedX5C, actualX5c);
        }

        [Fact]
        public void MsAuth10AtPop_ThrowsWithNullPopKeyTest()
        {
            // Arrange
            IConfidentialClientApplication app = CreateBuilder();
#pragma warning disable SYSLIB0057 // Type or member is obsolete
            using X509Certificate2 clientCertificate = new([]);
#pragma warning restore SYSLIB0057 // Type or member is obsolete
            var jwkClaim = "jwk_claim";
            var clientId = "client_id";

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => MsAuth10AtPop.WithAtPop(
                app.AcquireTokenForClient(new[] { TestConstants.Scopes }),
                clientCertificate,
                string.Empty,
                jwkClaim,
                clientId,
                true));
        }

        [Fact]
        public void MsAuth10AtPop_ThrowsWithNullJwkClaimTest()
        {
            // Arrange
            IConfidentialClientApplication app = CreateBuilder();
#pragma warning disable SYSLIB0057 // Type or member is obsolete
            using X509Certificate2 clientCertificate = new([]);
#pragma warning restore SYSLIB0057 // Type or member is obsolete
            var popPublicKey = "pop_key";
            var clientId = "client_id";

            // Act & Assert
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
            Assert.Throws<ArgumentNullException>(() => MsAuth10AtPop.WithAtPop(
                app.AcquireTokenForClient(new[] { TestConstants.Scopes }),
                clientCertificate, popPublicKey, null, clientId, true));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
        }

        private static IConfidentialClientApplication CreateBuilder()
        {
            var builder = ConfidentialClientApplicationBuilder.Create(TestConstants.ClientId);
            builder.WithExperimentalFeatures();
            builder.WithClientSecret(TestConstants.ClientSecret);
            return builder.Build();
        }
    }
}

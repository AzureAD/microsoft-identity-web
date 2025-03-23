// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Web.Test.Common;
using Microsoft.Identity.Web.Test.Common.Mocks;
using Microsoft.IdentityModel.Tokens;
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
            using var httpTokenRequest = MockHttpCreator.CreateClientCredentialTokenHandler(tokenType: "pop");
            mockHttpClientFactory.AddMockHandler(httpTokenRequest);

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
                .WithAtPop(popPublicKey, jwkClaim)
                .ExecuteAsync();

            // Assert
            httpTokenRequest.ActualRequestPostData.TryGetValue("request", out string? request);
            Assert.Null(request);

            httpTokenRequest.ActualRequestPostData.TryGetValue("client_assertion", out string? clientAssertion);
            Assert.NotNull(clientAssertion);

            // jwk is now passed in the http request as req_cnf
            httpTokenRequest.ActualRequestPostData.TryGetValue("req_cnf", out string? reqCnf);
            Assert.Equal(Base64UrlEncoder.Encode(jwkClaim), reqCnf);

            httpTokenRequest.ActualRequestPostData.TryGetValue("token_type", out string? tokenType);
            Assert.Equal("pop", tokenType);

            JwtSecurityTokenHandler jwtSecurityTokenHandler = new JwtSecurityTokenHandler();
            JwtSecurityToken assertion = jwtSecurityTokenHandler.ReadJwtToken(clientAssertion);

            Assert.Equal("https://login.microsoftonline.com/common/oauth2/v2.0/token", assertion.Claims.Single(c => c.Type == "aud").Value);
            Assert.Equal(TestConstants.ClientId, assertion.Claims.Single(c => c.Type == "iss").Value);
            Assert.Equal(TestConstants.ClientId, assertion.Claims.Single(c => c.Type == "sub").Value);
            Assert.NotEmpty(assertion.Claims.Single(c => c.Type == "jti").Value);

            // clientAssertion will no longer contain jwk
            Assert.Null(assertion.Claims.SingleOrDefault(c => c.Type == "pop_jwk"));
        }

        [Fact]
        public void MsAuth10AtPop_ThrowsWithNullPopKeyTest()
        {
            // Arrange
            IConfidentialClientApplication app = CreateBuilder();
            var jwkClaim = "jwk_claim";

            // Act & Assert
            Assert.Throws<ArgumentException>(() => MsAuth10AtPop.WithAtPop(
                app.AcquireTokenForClient([TestConstants.Scopes]),
                string.Empty,
                jwkClaim));
        }

        [Fact]
        public void MsAuth10AtPop_ThrowsWithNullJwkClaimTest()
        {
            // Arrange
            IConfidentialClientApplication app = CreateBuilder();
            var popPublicKey = "pop_key";

            // Act & Assert
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
            Assert.Throws<ArgumentNullException>(() => MsAuth10AtPop.WithAtPop(
                app.AcquireTokenForClient(new[] { TestConstants.Scopes }),
                popPublicKey, null));
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

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Identity.Client;
using Microsoft.Identity.Web.Test.Common;
using Xunit;

namespace Microsoft.Identity.Web.Test
{
    public class MsAuth10AtPopTests
    {
        [Fact]
        public void MsAuth10AtPop_WithAtPop_ShouldPopulateBuilderWithProofOfPosessionKeyIdAndOnBeforeTokenRequestTest()
        {
            // Arrange
            var builder = ConfidentialClientApplicationBuilder.Create(TestConstants.ClientId);
            builder.WithExperimentalFeatures();
            builder.WithClientSecret(TestConstants.ClientSecret);
            var app = builder.Build();
            var clientCertificate = new X509Certificate2(new byte[0]);
            var popPublicKey = "pop_key";
            var jwkClaim = "jwk_claim";
            var clientId = "client_id";

            // Act
            var result = MsAuth10AtPop.WithAtPop(app.AcquireTokenForClient(new[] { TestConstants.Scopes }), clientCertificate, popPublicKey, jwkClaim, clientId);

            // Assert
            Assert.NotNull(result);
        }

        [Fact]
        public void MsAuth10AtPop_ThrowsWithNullPopKeyTest()
        {
            // Arrange
            IConfidentialClientApplication app = CreateBuilder();
            var clientCertificate = new X509Certificate2(new byte[0]);
            var jwkClaim = "jwk_claim";
            var clientId = "client_id";

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => MsAuth10AtPop.WithAtPop(app.AcquireTokenForClient(new[] { TestConstants.Scopes }), clientCertificate, string.Empty, jwkClaim, clientId));
        }

        [Fact]
        public void MsAuth10AtPop_ThrowsWithNullJwkClaimTest()
        {
            // Arrange
            IConfidentialClientApplication app = CreateBuilder();
            var clientCertificate = new X509Certificate2(new byte[0]);
            var popPublicKey = "pop_key";
            var clientId = "client_id";

            // Act & Assert
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
            Assert.Throws<ArgumentNullException>(() => MsAuth10AtPop.WithAtPop(app.AcquireTokenForClient(new[] { TestConstants.Scopes }), clientCertificate, popPublicKey, null, clientId));
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

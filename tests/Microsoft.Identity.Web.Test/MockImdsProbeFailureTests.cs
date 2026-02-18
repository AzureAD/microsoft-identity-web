// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Net;
using System.Net.Http;
using Microsoft.Identity.Web.Test.Common.Mocks;
using Microsoft.Identity.Web.Test.Common.TestHelpers;
using Xunit;

namespace Microsoft.Identity.Web.Tests
{
    public class MockImdsProbeFailureTests
    {
        [Fact]
        public void MockImdsProbeFailure_V1_ReturnsNotFound()
        {
            // Arrange & Act
            var handler = MockHttpCreator.MockImdsProbeFailure(ImdsVersion.V1);

            // Assert
            Assert.NotNull(handler);
            Assert.Equal(HttpMethod.Get, handler.ExpectedMethod);
            Assert.Equal("http://169.254.169.254/metadata/identity/oauth2/token", handler.ExpectedUrl);
            Assert.Equal(HttpStatusCode.NotFound, handler.ResponseMessage.StatusCode);
        }

        [Fact]
        public void MockImdsProbeFailure_V2_ReturnsNotFound()
        {
            // Arrange & Act
            var handler = MockHttpCreator.MockImdsProbeFailure(ImdsVersion.V2);

            // Assert
            Assert.NotNull(handler);
            Assert.Equal(HttpMethod.Get, handler.ExpectedMethod);
            Assert.Equal("http://169.254.169.254/metadata/instance/compute/attestedData/csr", handler.ExpectedUrl);
            Assert.Equal(HttpStatusCode.NotFound, handler.ResponseMessage.StatusCode);
        }

        [Fact]
        public void MockImdsProbeFailure_WithUserAssignedClientId_ReturnsNotFound()
        {
            // Arrange & Act
            var handler = MockHttpCreator.MockImdsProbeFailure(
                ImdsVersion.V1,
                UserAssignedIdentityId.ClientId,
                "test-client-id");

            // Assert
            Assert.NotNull(handler);
            Assert.Equal(HttpMethod.Get, handler.ExpectedMethod);
            Assert.Equal("http://169.254.169.254/metadata/identity/oauth2/token", handler.ExpectedUrl);
            Assert.Equal(HttpStatusCode.NotFound, handler.ResponseMessage.StatusCode);
        }

        [Fact]
        public void MockImdsProbeFailure_WithNullUserAssignedId_ReturnsNotFound()
        {
            // Arrange & Act
            var handler = MockHttpCreator.MockImdsProbeFailure(
                ImdsVersion.V2,
                null,
                null);

            // Assert
            Assert.NotNull(handler);
            Assert.Equal(HttpMethod.Get, handler.ExpectedMethod);
            Assert.Equal("http://169.254.169.254/metadata/instance/compute/attestedData/csr", handler.ExpectedUrl);
            Assert.Equal(HttpStatusCode.NotFound, handler.ResponseMessage.StatusCode);
        }
    }
}

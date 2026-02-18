// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Net.Http;
using Microsoft.Identity.Web.Test.Common.Mocks;
using Microsoft.Identity.Web.Test.Common.TestHelpers;
using Xunit;

namespace Microsoft.Identity.Web.Tests
{
    /// <summary>
    /// Example demonstrating how to use MockImdsProbeFailure in managed identity tests.
    /// This simulates the pattern that would be used in FederatedIdentityCaeTests.cs.
    /// </summary>
    public class MockImdsUsageExample
    {
        [Fact]
        public void Example_UsingMockImdsProbeFailure_InManagedIdentityScenario()
        {
            // Arrange - Set up mock HTTP client factory with IMDS probe failure
            var mockHttp = new MockHttpClientFactory();
            
            // Add IMDS probe failure mock for V2 - this will cause MSAL to detect IMDS is unavailable
            mockHttp.AddMockHandler(
                MockHttpCreator.MockImdsProbeFailure(ImdsVersion.V2));
            
            // Add additional mocks for token acquisition or other scenarios
            // mockHttp.AddMockHandler(MockHttpCreator.CreateMsiTokenHandler("test-token"));
            
            // Act
            // In real usage, you would inject mockHttp into your service provider:
            // factory.Services.AddSingleton<IManagedIdentityTestHttpClientFactory>(_ => new TestManagedIdentityHttpFactory(mockHttp));
            // Then execute your managed identity authentication flow
            
            // Assert
            // The IMDS probe failure should cause the managed identity flow to handle the unavailability gracefully
            Assert.NotNull(mockHttp);
        }

        [Fact]
        public void Example_MockingBothV1AndV2ProbeFailures()
        {
            // Arrange
            var mockHttp = new MockHttpClientFactory();
            
            // Mock both V1 and V2 IMDS probe failures
            // This tests fallback behavior when both IMDS versions are unavailable
            mockHttp.AddMockHandler(
                MockHttpCreator.MockImdsProbeFailure(ImdsVersion.V1));
            
            mockHttp.AddMockHandler(
                MockHttpCreator.MockImdsProbeFailure(ImdsVersion.V2));
            
            // Act & Assert
            // In actual usage, the test would verify that the application handles
            // both IMDS probes failing appropriately
            Assert.NotNull(mockHttp);
        }

        [Fact]
        public void Example_MockingWithUserAssignedIdentity()
        {
            // Arrange
            var mockHttp = new MockHttpClientFactory();
            
            // Mock IMDS probe failure for a specific user-assigned managed identity
            mockHttp.AddMockHandler(
                MockHttpCreator.MockImdsProbeFailure(
                    ImdsVersion.V2,
                    UserAssignedIdentityId.ClientId,
                    "04ca4d6a-c720-4ba1-aa06-f6634b73fe7a"));
            
            // Act & Assert
            // Tests can verify that user-assigned identity scenarios
            // handle IMDS unavailability correctly
            Assert.NotNull(mockHttp);
        }
    }
}

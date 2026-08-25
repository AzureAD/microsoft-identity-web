// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Web.Test.Common;
using Microsoft.Identity.Web.Test.Common.Mocks;
using Microsoft.Identity.Web.TestOnly;
using Xunit;

namespace Microsoft.Identity.Web.Tests.Certificateless
{
    /// <summary>
    /// Verifies that <see cref="ManagedIdentityClientAssertion"/> auto-resolves the FIC token-exchange
    /// audience from the calling confidential client's authority host (flowed in via
    /// <see cref="AssertionRequestOptions.Authority"/>), so a single instance emits the correct
    /// per-cloud audience, while an explicit audience always wins.
    /// </summary>
    [Collection(nameof(TokenAcquirerFactorySingletonProtection))]
    public class ManagedIdentityClientAssertionResolutionTests
    {
        private const string UamiClientId = "04ca4d6a-c720-4ba1-aa06-f6634b73fe7a";

        [Theory]
        [InlineData("https://login.microsoftonline.com/tenant", "api://AzureADTokenExchange")]
        [InlineData("https://login.microsoftonline.us/tenant", "api://AzureADTokenExchangeUSGov")]
        [InlineData("https://login.partner.microsoftonline.cn/tenant", "api://AzureADTokenExchangeChina")]
        public async Task GetSignedAssertion_AutoResolvesAudienceFromRequestAuthorityAsync(
            string authority, string expectedAudience)
        {
            MockHttpMessageHandler handler = await AcquireAssertionAsync(
                explicitAudience: null,
                authority: authority);

            AssertResource(handler, expectedAudience);
        }

        [Fact]
        public async Task GetSignedAssertion_ExplicitAudience_WinsOverRequestAuthorityAsync()
        {
            // Even for a US Gov authority, an explicit audience supplied to the constructor takes precedence.
            MockHttpMessageHandler handler = await AcquireAssertionAsync(
                explicitAudience: "api://MyCustomTokenExchange",
                authority: "https://login.microsoftonline.us/tenant");

            AssertResource(handler, "api://MyCustomTokenExchange");
        }

        [Fact]
        public async Task GetSignedAssertion_NoAuthority_FallsBackToPublicAudienceAsync()
        {
            // The MISE managed-identity leg invokes the assertion in isolation (no authority); it must
            // fall back to the public-cloud audience rather than throwing.
            MockHttpMessageHandler handler = await AcquireAssertionAsync(
                explicitAudience: null,
                authority: null);

            AssertResource(handler, "api://AzureADTokenExchange");
        }

        private static async Task<MockHttpMessageHandler> AcquireAssertionAsync(
            string? explicitAudience, string? authority)
        {
            var mockMiHttp = new MockHttpClientFactory();
            MockHttpMessageHandler handler = mockMiHttp.AddMockHandler(
                MockHttpCreator.CreateMsiTokenHandler("mi-assertion-token"));
            var miTestFactory = new Microsoft.Identity.Web.Test.TestManagedIdentityHttpFactory(mockMiHttp);
            ManagedIdentityClientAssertionTestHook.HttpClientFactoryForTests = miTestFactory.Create();

            try
            {
                var assertion = new ManagedIdentityClientAssertion(UamiClientId, explicitAudience);
                await assertion.GetSignedAssertionAsync(new AssertionRequestOptions
                {
                    Authority = authority!,
                    // Non-empty claims force MSAL to bypass the (process-shared) managed-identity token
                    // cache, so every case issues a fresh IMDS request we can inspect.
                    Claims = "{\"access_token\":{}}",
                    CancellationToken = CancellationToken.None,
                }).ConfigureAwait(false);

                return handler;
            }
            finally
            {
                ManagedIdentityClientAssertionTestHook.HttpClientFactoryForTests = null;
            }
        }

        private static void AssertResource(MockHttpMessageHandler handler, string expectedAudience)
        {
            Assert.NotNull(handler.ActualRequestMessage);
            string query = handler.ActualRequestMessage.RequestUri!.Query;
            Assert.Contains("resource=" + expectedAudience, query, StringComparison.OrdinalIgnoreCase);
        }
    }
}

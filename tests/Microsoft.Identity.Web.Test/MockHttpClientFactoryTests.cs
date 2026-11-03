// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Identity.Web.Test.Common.Mocks;
using Xunit;

namespace Microsoft.Identity.Web.Test
{
    public class MockHttpClientFactoryTests
    {
        [Theory]
        [InlineData(false, false)]
        [InlineData(true, false)]
        [InlineData(false, true)]
        [InlineData(true, true)]
        public async Task InstanceDiscovery_RequeuesCurrentHandlerAsync(
            bool discoverBeforeFirstToken,
            bool discoverBeforeSecondToken)
        {
            const string firstTokenEndpoint = "https://login.microsoftonline.com/tenant/oauth2/v2.0/token";
            const string secondTokenEndpoint = "https://login.microsoftonline.us/tenant/oauth2/v2.0/token";
            using var factory = new MockHttpClientFactory();
            var firstTokenHandler = factory.AddMockHandler(
                MockHttpCreator.CreateClientCredentialTokenHandler("first-token"));
            firstTokenHandler.ExpectedUrl = firstTokenEndpoint;
            var secondTokenHandler = factory.AddMockHandler(
                MockHttpCreator.CreateClientCredentialTokenHandler("second-token"));
            secondTokenHandler.ExpectedUrl = secondTokenEndpoint;

            if (discoverBeforeFirstToken)
            {
                using var discoveryClient = factory.GetHttpClient();
                using var discoveryResponse = await discoveryClient.GetAsync(
                    "https://login.microsoftonline.com/common/discovery/instance");
                Assert.Equal(HttpStatusCode.OK, discoveryResponse.StatusCode);
            }

            using var firstClient = factory.GetHttpClient();
            using var firstResponse = await firstClient.PostAsync(
                firstTokenEndpoint,
                new StringContent(string.Empty));

            if (discoverBeforeSecondToken)
            {
                using var discoveryClient = factory.GetHttpClient();
                using var discoveryResponse = await discoveryClient.GetAsync(
                    "https://login.microsoftonline.us/common/discovery/instance");
                Assert.Equal(HttpStatusCode.OK, discoveryResponse.StatusCode);
            }

            using var secondClient = factory.GetHttpClient();
            using var secondResponse = await secondClient.PostAsync(
                secondTokenEndpoint,
                new StringContent(string.Empty));

            Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);
            Assert.Equal(HttpStatusCode.OK, secondResponse.StatusCode);
            using var firstPayload = JsonDocument.Parse(await firstResponse.Content.ReadAsStringAsync());
            using var secondPayload = JsonDocument.Parse(await secondResponse.Content.ReadAsStringAsync());
            Assert.Equal("first-token", firstPayload.RootElement.GetProperty("access_token").GetString());
            Assert.Equal("second-token", secondPayload.RootElement.GetProperty("access_token").GetString());
            Assert.Throws<InvalidOperationException>(() => factory.GetHttpClient());
        }
    }
}

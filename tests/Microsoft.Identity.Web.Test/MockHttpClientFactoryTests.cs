// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Identity.Web.Test.Common.Mocks;
using Xunit;

namespace Microsoft.Identity.Web.Test
{
    public class MockHttpClientFactoryTests
    {
        [Fact]
        public async Task InstanceDiscovery_RequeuesCurrentHandlerAsync()
        {
            using var factory = new MockHttpClientFactory();
            factory.AddMockHandler(MockHttpCreator.CreateClientCredentialTokenHandler("first-token"));
            var secondTokenHandler = factory.AddMockHandler(
                MockHttpCreator.CreateClientCredentialTokenHandler("second-token"));

            using var firstClient = factory.GetHttpClient();
            using var firstResponse = await firstClient.PostAsync(
                "https://login.microsoftonline.com/tenant/oauth2/v2.0/token",
                new StringContent(string.Empty));

            using var discoveryClient = factory.GetHttpClient();
            using var discoveryResponse = await discoveryClient.GetAsync(
                "https://login.microsoftonline.com/common/discovery/instance");

            using var secondClient = factory.GetHttpClient();
            using var secondResponse = await secondClient.PostAsync(
                "https://login.microsoftonline.us/tenant/oauth2/v2.0/token",
                new StringContent(string.Empty));

            Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);
            Assert.Equal(HttpStatusCode.OK, discoveryResponse.StatusCode);
            Assert.Equal(HttpStatusCode.OK, secondResponse.StatusCode);
            Assert.Equal(
                "https://login.microsoftonline.us/tenant/oauth2/v2.0/token",
                secondTokenHandler.ActualRequestMessage.RequestUri!.AbsoluteUri);
        }
    }
}

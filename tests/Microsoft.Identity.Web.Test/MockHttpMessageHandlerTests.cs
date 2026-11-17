// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Identity.Web.Test.Common;
using Microsoft.Identity.Web.Test.Common.Mocks;
using Xunit;

namespace Microsoft.Identity.Web.Test
{
    public class MockHttpMessageHandlerTests
    {
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task SendAsync_UsesConfiguredResponseAndRecordsContent(bool useFactory)
        {
            // Arrange
            using var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("expected-response")
            };
            using var fallback = new HttpResponseMessage(HttpStatusCode.Accepted);
            var handler = new MockHttpMessageHandler
            {
                ExpectedMethod = HttpMethod.Post,
                ExpectedPostData = new Dictionary<string, string> { ["key"] = "value" },
                ResponseMessage = useFactory ? fallback : response
            };
            int factoryCalls = 0;
            if (useFactory)
            {
                handler.ResponseFactory = () =>
                {
                    factoryCalls++;
                    return response;
                };
            }
            using var client = new HttpClient(handler);
            using var request = new HttpRequestMessage(HttpMethod.Post, "https://example.test/token")
            {
                Content = new FormUrlEncodedContent(new Dictionary<string, string> { ["key"] = "value" })
            };
            Assert.Equal(0, factoryCalls);

            // Act
            HttpResponseMessage actual = await client.SendAsync(request);

            // Assert
            Assert.Same(response, actual);
            Assert.Same(response, handler.ResponseMessage);
            Assert.Same(request, handler.ActualRequestMessage);
            Assert.Equal("value", handler.ActualRequestPostData["key"]);
            Assert.Equal("expected-response", handler.ResponseString);
            Assert.Equal(useFactory ? 1 : 0, factoryCalls);
        }

        [Fact]
        public async Task SendAsync_DiscoveryRequest_DoesNotConsumeResponseFactory()
        {
            // Arrange
            int factoryCalls = 0;
            var handler = new MockHttpMessageHandler
            {
                ExpectedMethod = HttpMethod.Post,
                ResponseFactory = () =>
                {
                    factoryCalls++;
                    throw new InvalidOperationException("The token response must not serve instance discovery.");
                }
            };
            using var client = new HttpClient(handler);

            // Act
            using HttpResponseMessage response = await client.GetAsync("https://login.microsoftonline.com/common/discovery/instance");

            // Assert
            Assert.Equal(0, factoryCalls);
            Assert.Equal(TestConstants.DiscoveryJsonResponse, await response.Content.ReadAsStringAsync());
            Assert.Equal(TestConstants.DiscoveryJsonResponse, handler.ResponseString);
        }

        [Theory]
        [InlineData(60)]
        [InlineData(3599)]
        public async Task CreateMsiTokenHandler_CalculatesExpiryWhenEachRequestArrives(int expiresIn)
        {
            // Arrange
            var now = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
            int clockReads = 0;
            var handler = MockHttpCreator.CreateMsiTokenHandler(
                "fixture-assertion",
                expiresIn: expiresIn,
                getUtcNow: () =>
                {
                    clockReads++;
                    return now;
                });
            using var client = new HttpClient(handler);
            Assert.Equal(0, clockReads);

            for (int requestNumber = 1; requestNumber <= 2; requestNumber++)
            {
                // Act
                now = now.AddHours(1);
                using HttpResponseMessage response = await client.GetAsync("https://example.test/metadata/identity/oauth2/token");
                using JsonDocument body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

                // Assert
                Assert.Equal(requestNumber, clockReads);
                Assert.Equal(
                    now.AddSeconds(expiresIn).ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture),
                    body.RootElement.GetProperty("expires_on").GetString());
                Assert.Equal(expiresIn.ToString(CultureInfo.InvariantCulture), body.RootElement.GetProperty("expires_in").GetString());
                Assert.Equal("fixture-assertion", body.RootElement.GetProperty("access_token").GetString());
            }
        }
    }
}

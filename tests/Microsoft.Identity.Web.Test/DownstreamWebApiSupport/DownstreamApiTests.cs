// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Web.Test.Resource;
using Xunit;

namespace Microsoft.Identity.Web.Tests
{
    public class DownstreamApiTests
    {
        private readonly IAuthorizationHeaderProvider _authorizationHeaderProvider;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IOptionsMonitor<DownstreamApiOptions> _namedDownstreamApiOptions;
        private readonly ILogger<DownstreamApi> _logger;
        private readonly DownstreamApi _input;

        public DownstreamApiTests()
        {
            _authorizationHeaderProvider = new MyAuthorizationHeaderProvider();
            _httpClientFactory = new HttpClientFactoryTest();
            _namedDownstreamApiOptions = new MyMonitor();
            _logger = new LoggerFactory().CreateLogger<DownstreamApi>();

            _input = new DownstreamApi(
             _authorizationHeaderProvider,
             _namedDownstreamApiOptions,
             _httpClientFactory,
             _logger);
        }

        [Fact]
        public async Task UpdateRequestAsync_WithContent_AddsContentToRequest()
        {
            // Arrange
            var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, "https://example.com");
            var content = new StringContent("test content");

            // Act
            await _input.UpdateRequestAsync(httpRequestMessage, content, new DownstreamApiOptions(), false, null, CancellationToken.None);

            // Assert
            Assert.Equal(content, httpRequestMessage.Content);
            Assert.Equal("application/json", httpRequestMessage.Headers.Accept.Single().MediaType);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]

        public async Task UpdateRequestAsync_WithScopes_AddsAuthorizationHeaderToRequest(bool appToken)
        {
            // Arrange
            var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, "https://example.com");
            var content = new StringContent("test content");
            var options = new DownstreamApiOptions
            {
                Scopes = ["scope1", "scope2"],
                BaseUrl = "https://localhost:44321/WeatherForecast"
            };
            var user = new ClaimsPrincipal();

            // Act
            await _input.UpdateRequestAsync(httpRequestMessage, content, options, appToken, user, CancellationToken.None);

            // Assert
            Assert.True(httpRequestMessage.Headers.Contains("Authorization"));
            Assert.Equal("ey", httpRequestMessage.Headers.Authorization?.Parameter);
            Assert.Equal("Bearer", httpRequestMessage.Headers.Authorization?.Scheme);
            Assert.Equal("application/json", httpRequestMessage.Headers.Accept.Single().MediaType);
        }

        [Fact]
        public void SerializeInput_ReturnsCorrectHttpContent()
        {
            // Arrange
            var options = new DownstreamApiOptions
            {
                Serializer = (obj) => new StringContent(JsonSerializer.Serialize(obj), Encoding.UTF8, "application/json"),
                ContentType = "application/json",
                AcceptHeader = "application/json"
            };

            // Act
            var result = DownstreamApi.SerializeInput(_input, options);

            // Assert
            Assert.NotNull(result);
            Assert.IsType<StringContent>(result);
            Assert.Equal("application/json", result.Headers.ContentType?.MediaType);
        }
    }
    public class MyMonitor : IOptionsMonitor<DownstreamApiOptions>
    {
        public DownstreamApiOptions CurrentValue => new DownstreamApiOptions();

        public DownstreamApiOptions Get(string name)
        {
            return new DownstreamApiOptions();
        }

        public DownstreamApiOptions Get(string name, string key)
        {
            return new DownstreamApiOptions();
        }

        public IDisposable OnChange(Action<DownstreamApiOptions, string> listener)
        {
            throw new NotImplementedException();
        }
    }

    public class MyAuthorizationHeaderProvider : IAuthorizationHeaderProvider
    {
        public Task<string> CreateAuthorizationHeaderForAppAsync(string scopes, AuthorizationHeaderProviderOptions? downstreamApiOptions = null, CancellationToken cancellationToken = default)
        {
            return Task.FromResult("Bearer ey");
        }

        public Task<string> CreateAuthorizationHeaderForUserAsync(IEnumerable<string> scopes, AuthorizationHeaderProviderOptions? authorizationHeaderProviderOptions = null, ClaimsPrincipal? claimsPrincipal = null, CancellationToken cancellationToken = default)
        {
            return Task.FromResult("Bearer ey");
        }
    }
}


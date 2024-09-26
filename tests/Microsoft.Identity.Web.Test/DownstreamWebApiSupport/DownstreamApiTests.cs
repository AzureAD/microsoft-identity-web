// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
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
        public async Task UpdateRequestAsync_WithContent_AddsContentToRequestAsync()
        {
            // Arrange
            var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, "https://example.com");
            var content = new StringContent("test content");

            // Act
            await _input.UpdateRequestAsync(httpRequestMessage, content, new DownstreamApiOptions(), false, null, CancellationToken.None);

            // Assert
            Assert.Equal(content, httpRequestMessage.Content);
            Assert.Equal("application/json", httpRequestMessage.Headers.Accept.Single().MediaType);
            Assert.Equal("text/plain", httpRequestMessage.Content?.Headers.ContentType?.MediaType);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]

        public async Task UpdateRequestAsync_WithScopes_AddsAuthorizationHeaderToRequestAsync(bool appToken)
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
                Serializer = (obj) => new StringContent(JsonSerializer.Serialize(obj), Encoding.UTF8, "application/json")
            };

            // Act
            var result = DownstreamApi.SerializeInput(_input, options);

            // Assert
            Assert.NotNull(result);
            Assert.IsType<StringContent>(result);
            Assert.Equal("application/json", result.Headers.ContentType?.MediaType);
        }

        private readonly DownstreamApiOptions _options = new();

        [Fact]
        public void SerializeInput_WithHttpContent_ReturnsSameContent()
        {
            // Arrange
            var content = new StringContent("test");

            // Act
            var result = DownstreamApi.SerializeInput(content, _options);

            // Assert
            Assert.Equal(content, result);
        }

        [Fact]
        public async Task SerializeInput_WithSerializer_ReturnsSerializedContentAsync()
        {
            // Arrange
            var input = new Person();
            _options.Serializer = o => new StringContent("serialized");

            // Act
            var httpContent = DownstreamApi.SerializeInput(input, _options);
            var result = await httpContent?.ReadAsStringAsync()!;

            // Assert
            Assert.Equal("serialized", result);
        }

        [Fact]
        public async Task SerializeInput_WithStringAndContentType_ReturnsStringContentAsync()
        {
            // Arrange
            var input = "test";
            _options.ContentType = "text/plain";

            // Act
            var httpContent = DownstreamApi.SerializeInput(input, _options);
            var result = await httpContent?.ReadAsStringAsync()!;

            // Assert
            Assert.IsType<StringContent>(httpContent);
            Assert.Equal(input, result);
        }

        [Fact]
        public async Task SerializeInput_WithByteArray_ReturnsByteArrayContentAsync()
        {
            // Arrange
            var input = new byte[] { 1, 2, 3 };

            // Act
            var httpContent = DownstreamApi.SerializeInput(input, _options);
            var result = await httpContent?.ReadAsByteArrayAsync()!;

            // Assert
            Assert.IsType<ByteArrayContent>(httpContent);
            Assert.Equal(input, result);
        }

        [Fact]
        public async Task SerializeInput_WithStream_ReturnsStreamContentAsync()
        {
            // Arrange
            var input = new MemoryStream(Encoding.UTF8.GetBytes("test"));

            // Act
            var httpContent = DownstreamApi.SerializeInput(input, _options);
            var result = await httpContent?.ReadAsStreamAsync()!;

            // Assert
            Assert.IsType<StreamContent>(httpContent);
            Assert.Equal("test", await new StreamReader(result).ReadToEndAsync());
        }

        [Fact]
        public void SerializeInput_WithNull_ReturnsNull()
        {
            // Arrange
            object? input = null;

            // Act
            var result = DownstreamApi.SerializeInput(input, _options);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task DeserializeOutput_ThrowsHttpRequestException_WhenResponseIsNotSuccessfulAsync()
        {
            // Arrange
            var response = new HttpResponseMessage(HttpStatusCode.BadRequest);
            var options = new DownstreamApiOptions();

            // Act and Assert
            await Assert.ThrowsAsync<HttpRequestException>(() => DownstreamApi.DeserializeOutputAsync<HttpContent>(response, options));
        }

        [Fact]
        public async Task DeserializeOutput_ReturnsDefault_WhenContentIsNullAsync()
        {
            // Arrange
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = null
            };
            var options = new DownstreamApiOptions();

            // Act
            var result = await DownstreamApi.DeserializeOutputAsync<HttpContent>(response, options);

            // Assert
            Assert.Empty(result!.Headers);
        }

        [Fact]
        public async Task DeserializeOutput_ReturnsContent_WhenOutputTypeIsHttpContentAsync()
        {
            // Arrange
            var content = new StringContent("test");
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = content
            };
            var options = new DownstreamApiOptions();

            // Act
            var result = await DownstreamApi.DeserializeOutputAsync<HttpContent>(response, options);

            // Assert
            Assert.Equal(content, result);
        }

        [Fact]
        public async Task DeserializeOutput_ReturnsDeserializedContent_WhenDeserializerIsProvidedAsync()
        {
            // Arrange
            var content = new StringContent("{\"Name\":\"John\",\"Age\":30}", Encoding.UTF8, "application/json");
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = content
            };
            var options = new DownstreamApiOptions
            {
                Deserializer = c => JsonSerializer.Deserialize<Person>(c!.ReadAsStringAsync().Result)
            };

            // Act
            var result = await DownstreamApi.DeserializeOutputAsync<Person>(response, options);

            // Assert
            Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
            Assert.Equal("John", result?.Name);
            Assert.Equal(30, result?.Age);
        }

        [Fact]
        public async Task DeserializeOutput_ReturnsDeserializedContentAsync()
        {
            // Arrange
            var content = new StringContent("{\"Name\":\"John\",\"Age\":30}", Encoding.UTF8, "application/json");
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = content
            };
            var options = new DownstreamApiOptions();

            // Act
            var result = await DownstreamApi.DeserializeOutputAsync<Person>(response, options);

            // Assert
            Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
            Assert.Equal("John", result?.Name);
            Assert.Equal(30, result?.Age);
        }

#if NET8_0_OR_GREATER
        [Fact]
        public async Task SerializeInput_WithSerializer_ReturnsSerializedContent_WhenJsonTypeInfoProvidedAsync()
        {
            // Arrange
            var input = new Person();
            _options.Serializer = o => new StringContent("serialized");

            // Act
            var httpContent = DownstreamApi.SerializeInput(input, _options, CustomJsonContext.Default.Person);
            string result = await httpContent?.ReadAsStringAsync()!;

            // Assert
            Assert.Equal("serialized", result);
        }

        [Fact]
        public async Task DeserializeOutput_ReturnsDeserializedContent_WhenJsonTypeInfoProvidedAsync()
        {
            // Arrange
            var content = new StringContent("{\"Name\":\"John\",\"Age\":30}", Encoding.UTF8, "application/json");
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = content
            };
            var options = new DownstreamApiOptions();

            // Act
            var result = await DownstreamApi.DeserializeOutputAsync<Person>(response, options, CustomJsonContext.Default.Person);

            // Assert
            Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
            Assert.Equal("John", result?.Name);
            Assert.Equal(30, result?.Age);
        }
#endif

        [Fact]
        public async Task DeserializeOutput_ThrowsNotSupportedException_WhenContentTypeIsNotSupportedAsync()
        {
            // Arrange
            var content = new StringContent("test", Encoding.UTF8, "application/xml");
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = content
            };
            var options = new DownstreamApiOptions();

            // Act and Assert
            await Assert.ThrowsAsync<NotSupportedException>(() => DownstreamApi.DeserializeOutputAsync<string>(response, options));
        }
    }

    public class Person
    {
        public string? Name { get; set; }
        public int? Age { get; set; }
    }

    [JsonSerializable(typeof(Person))]
    internal partial class CustomJsonContext : JsonSerializerContext
    {
    }

    public class MyMonitor : IOptionsMonitor<DownstreamApiOptions>
    {
        public DownstreamApiOptions CurrentValue => new DownstreamApiOptions();

        public DownstreamApiOptions Get(string? name)
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

        public Task<string> CreateAuthorizationHeaderAsync(IEnumerable<string> scopes, AuthorizationHeaderProviderOptions? authorizationHeaderProviderOptions = null, ClaimsPrincipal? claimsPrincipal = null, CancellationToken cancellationToken = default)
        {
            return Task.FromResult("Bearer ey");
        }
    }
}


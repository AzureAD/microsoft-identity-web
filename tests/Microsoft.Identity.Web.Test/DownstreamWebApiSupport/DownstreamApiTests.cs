// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Client;
using Microsoft.Identity.Web.Test.Common;
using Microsoft.Identity.Web.Test.Common.Mocks;
using Microsoft.Identity.Web.Test.Resource;
using NSubstitute;
using Xunit;

namespace Microsoft.Identity.Web.Tests
{
    public class DownstreamApiTests
    {
        private readonly IAuthorizationHeaderProvider _authorizationHeaderProvider;
        private readonly IAuthorizationHeaderProvider _authorizationHeaderProviderSaml;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IOptionsMonitor<DownstreamApiOptions> _namedDownstreamApiOptions;
        private readonly ILogger<DownstreamApi> _logger;
        private readonly DownstreamApi _input;
        private readonly DownstreamApi _inputSaml;

        public DownstreamApiTests()
        {
            _authorizationHeaderProvider = new MyAuthorizationHeaderProvider();
            _authorizationHeaderProviderSaml = new MySamlAuthorizationHeaderProvider();
            _httpClientFactory = new HttpClientFactoryTest();
            _namedDownstreamApiOptions = new MyMonitor();
            _logger = new LoggerFactory().CreateLogger<DownstreamApi>();

            _input = new DownstreamApi(
             _authorizationHeaderProvider,
             _namedDownstreamApiOptions,
             _httpClientFactory,
             _logger);

            _inputSaml = new DownstreamApi(
             _authorizationHeaderProviderSaml,
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
            var options = new DownstreamApiOptions();

            // Act
            await _input.UpdateRequestAsync(httpRequestMessage, content, options, false, null, CancellationToken.None);

            // Assert
            Assert.Equal(content, httpRequestMessage.Content);
            Assert.Equal("application/json", httpRequestMessage.Headers.Accept.Single().MediaType);
            Assert.Equal("text/plain", httpRequestMessage.Content?.Headers.ContentType?.MediaType);
            Assert.Equal(options.AcquireTokenOptions.ExtraQueryParameters, DownstreamApi.CallerSDKDetails);
        }


        [Fact]
        public async Task UpdateRequestAsync_AddsToExtraQP()
        {
            // Arrange
            var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, "https://example.com");
            var content = new StringContent("test content");
            var options = new DownstreamApiOptions() {
                AcquireTokenOptions = new AcquireTokenOptions() {
                    ExtraQueryParameters = new Dictionary<string, string>()
                    {
                        { "n1", "v1" },
                        { "n2", "v2" },
                        { "caller-sdk-id", "bogus" } // value will be overwritten by the SDK
                    }
                } };

            // Act
            await _input.UpdateRequestAsync(httpRequestMessage, content, options, false, null, CancellationToken.None);

            // Assert
            Assert.Equal(content, httpRequestMessage.Content);
            Assert.Equal("application/json", httpRequestMessage.Headers.Accept.Single().MediaType);
            Assert.Equal("text/plain", httpRequestMessage.Content?.Headers.ContentType?.MediaType);
            Assert.Equal("v1", options.AcquireTokenOptions.ExtraQueryParameters["n1"]);
            Assert.Equal("v2", options.AcquireTokenOptions.ExtraQueryParameters["n2"]);
            Assert.Equal(
                DownstreamApi.CallerSDKDetails["caller-sdk-id"],
                options.AcquireTokenOptions.ExtraQueryParameters["caller-sdk-id"] );
            Assert.Equal(
                DownstreamApi.CallerSDKDetails["caller-sdk-ver"],
                options.AcquireTokenOptions.ExtraQueryParameters["caller-sdk-ver"]);

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
            Assert.Equal(options.AcquireTokenOptions.ExtraQueryParameters, DownstreamApi.CallerSDKDetails);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]

        public async Task UpdateRequestAsync_WithScopes_AddsSamlAuthorizationHeaderToRequestAsync(bool appToken)
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
            await _inputSaml.UpdateRequestAsync(httpRequestMessage, content, options, appToken, user, CancellationToken.None);

            // Assert
            Assert.True(httpRequestMessage.Headers.Contains("Authorization"));
            Assert.Equal("http://schemas.microsoft.com/dsts/saml2-bearer ey", httpRequestMessage.Headers.GetValues("Authorization").FirstOrDefault());
            Assert.Equal("application/json", httpRequestMessage.Headers.Accept.Single().MediaType);
            Assert.Equal(options.AcquireTokenOptions.ExtraQueryParameters, DownstreamApi.CallerSDKDetails);
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

        [Fact]
        public async Task ReadErrorResponseContentAsync_ReturnsFullContent_WhenContentLengthIsBelowThresholdAsync()
        {
            // Arrange
            var errorContent = "Error message";
            var response = new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent(errorContent)
            };

            // Act
            var result = await DownstreamApi.ReadErrorResponseContentAsync(response);

            // Assert
            Assert.Equal(errorContent, result);
        }

        [Fact]
        public async Task ReadErrorResponseContentAsync_ReturnsTruncatedContent_WhenContentLengthExceedsThresholdAsync()
        {
            // Arrange
            var longErrorContent = new string('a', 5000); // 5000 characters, exceeds 4096 threshold
            var response = new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent(longErrorContent)
            };

            // Act
            var result = await DownstreamApi.ReadErrorResponseContentAsync(response);

            // Assert
            // When Content-Length header is set and exceeds threshold, we get a message about the size
            Assert.True(
                result.Contains("[Error response too large:", StringComparison.Ordinal) ||
                result.EndsWith("... (truncated)", StringComparison.Ordinal),
                "Error response should be limited in size");
            Assert.True(result.Length <= 4096 + "... (truncated)".Length);
        }

        [Fact]
        public async Task ReadErrorResponseContentAsync_ReturnsMessage_WhenContentLengthHeaderExceedsThresholdAsync()
        {
            // Arrange
            var response = new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent(new string('a', 5000))
            };
            // Ensure Content-Length header is set
            _ = response.Content.Headers.ContentLength;

            // Act
            var result = await DownstreamApi.ReadErrorResponseContentAsync(response);

            // Assert
            // When Content-Length is known and exceeds threshold, we should either skip reading or truncate
            Assert.NotNull(result);
            // Either we get the truncation message about size, or the actual content is truncated
            Assert.True(
                result.Contains("[Error response too large:", StringComparison.Ordinal) ||
                result.EndsWith("... (truncated)", StringComparison.Ordinal) ||
                result.Length <= 4096 + "... (truncated)".Length,
                "Error response should be limited in size");
        }

        [Fact]
        public void DownstreamApi_Constructor_WithBoundProvider_AcceptsIMsalMtlsHttpClientFactory()
        {
            // Arrange
            var mockBoundProvider = Substitute.For<IAuthorizationHeaderProvider, IBoundAuthorizationHeaderProvider>();
            var mockMtlsHttpClientFactory = Substitute.For<IHttpClientFactory, IMsalMtlsHttpClientFactory>();

            // Act & Assert - Should not throw
            var downstreamApi = new DownstreamApi(
                mockBoundProvider,
                _namedDownstreamApiOptions,
                mockMtlsHttpClientFactory,
                _logger);

            Assert.NotNull(downstreamApi);
        }

        [Theory]
        [InlineData(null, false)]
        [InlineData("", false)]
        [InlineData("Bearer", false)]
        [InlineData("mtls_pop", true)]
        [InlineData("Mtls_Pop", true)]
        [InlineData("MTLS_POP", true)]
        public async Task UpdateRequestAsync_WithAuthorizationHeaderBoundProvider_CallsCorrectInterface(string? ProtocolScheme, bool shouldCallAuthorizationHeaderProvider2)
        {
            // Arrange
            var mockBoundProvider = Substitute.For<IAuthorizationHeaderProvider, IBoundAuthorizationHeaderProvider>();
            var testCertificate = Substitute.For<X509Certificate2>();

            var downstreamApi = new DownstreamApi(
                mockBoundProvider,
                _namedDownstreamApiOptions,
                _httpClientFactory,
                _logger);

            var options = new DownstreamApiOptions
            {
                Scopes = new[] { "https://api.example.com/.default" },
            };

            if (ProtocolScheme != null)
            {
                options.ProtocolScheme = ProtocolScheme;
            }

            var authHeaderInfo = new AuthorizationHeaderInformation
            {
                AuthorizationHeaderValue = "MTLS_POP test-token",
                BindingCertificate = testCertificate
            };

            var mockResult = new OperationResult<AuthorizationHeaderInformation, AuthorizationHeaderError>(authHeaderInfo);

            ((IAuthorizationHeaderProvider)mockBoundProvider)
                .CreateAuthorizationHeaderAsync(
                    Arg.Any<IEnumerable<string>>(),
                    Arg.Any<AuthorizationHeaderProviderOptions>(),
                    Arg.Any<ClaimsPrincipal>(),
                    Arg.Any<CancellationToken>())
                .Returns("Bearer test-token");

            ((IBoundAuthorizationHeaderProvider)mockBoundProvider)
                .CreateBoundAuthorizationHeaderAsync(
                    Arg.Any<DownstreamApiOptions>(),
                    Arg.Any<ClaimsPrincipal>(),
                    Arg.Any<CancellationToken>())
                .Returns(mockResult);

            var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, "https://api.example.com");

            // Act
            var result = await downstreamApi.UpdateRequestAsync(
                httpRequestMessage,
                null,
                options,
                false,
                null,
                CancellationToken.None);

            // Assert
            if (shouldCallAuthorizationHeaderProvider2)
            {
                await ((IBoundAuthorizationHeaderProvider)mockBoundProvider).Received(1).CreateBoundAuthorizationHeaderAsync(
                    Arg.Any<DownstreamApiOptions>(),
                    Arg.Any<ClaimsPrincipal>(),
                    Arg.Any<CancellationToken>());

                await mockBoundProvider.DidNotReceive().CreateAuthorizationHeaderAsync(
                    Arg.Any<IEnumerable<string>>(),
                    Arg.Any<DownstreamApiOptions>(),
                    Arg.Any<ClaimsPrincipal>(),
                    Arg.Any<CancellationToken>());

                Assert.NotNull(result);
                Assert.Equal("MTLS_POP test-token", result.AuthorizationHeaderValue);
                Assert.Equal(testCertificate, result.BindingCertificate);
                Assert.Equal("MTLS_POP test-token", httpRequestMessage.Headers.Authorization?.ToString());
            }
            else
            {
                await ((IBoundAuthorizationHeaderProvider)mockBoundProvider).DidNotReceive().CreateBoundAuthorizationHeaderAsync(
                    Arg.Any<DownstreamApiOptions>(),
                    Arg.Any<ClaimsPrincipal>(),
                    Arg.Any<CancellationToken>());

                await mockBoundProvider.Received(1).CreateAuthorizationHeaderAsync(
                    Arg.Any<IEnumerable<string>>(),
                    Arg.Any<DownstreamApiOptions>(),
                    Arg.Any<ClaimsPrincipal>(),
                    Arg.Any<CancellationToken>());

                Assert.Null(result);
                Assert.Equal("Bearer test-token", httpRequestMessage.Headers.Authorization?.ToString());
            }
        }

        [Fact]
        public async Task UpdateRequestAsync_WithRegularAuthorizationHeaderProvider_FallsBackCorrectly()
        {
            // Arrange - Using the existing regular provider from the constructor
            var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, "https://api.example.com");
            var options = new DownstreamApiOptions
            {
                Scopes = new[] { "https://api.example.com/.default" }
            };

            // Act
            var result = await _input.UpdateRequestAsync(
                httpRequestMessage,
                null,
                options,
                false,
                null,
                CancellationToken.None);

            // Assert
            Assert.Null(result); // Regular provider doesn't return AuthorizationHeaderInformation
            Assert.Equal("Bearer ey", httpRequestMessage.Headers.Authorization?.ToString());
        }

        [Fact]
        public async Task CallApiInternalAsync_WithRegularAuthorizationHeaderProvider_UsesRegularHttpClientFactory()
        {
            // Arrange
            var mockHttpClientFactory = Substitute.For<IHttpClientFactory>();
            var mockHandler = new MockHttpMessageHandler()
            {
                ExpectedMethod = HttpMethod.Get,
                ResponseMessage = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{\"result\": \"success\"}")
                }
            };
            var mockHttpClient = new HttpClient(mockHandler);

            mockHttpClientFactory.CreateClient(Arg.Any<string>()).Returns(mockHttpClient);

            var downstreamApi = new DownstreamApi(
                _authorizationHeaderProvider, // Regular provider
                _namedDownstreamApiOptions,
                mockHttpClientFactory,
                _logger);

            var options = new DownstreamApiOptions
            {
                BaseUrl = "https://api.example.com",
                Scopes = new[] { "https://api.example.com/.default" },
                HttpMethod = "GET"
            };

            // Act
            await downstreamApi.CallApiInternalAsync(null, options, false, null, null, CancellationToken.None);

            // Assert
            mockHttpClientFactory.Received(1).CreateClient(Arg.Any<string>());

            // Note: HttpClient is disposed by DownstreamApi, no manual disposal needed
        }

        [Fact]
        public async Task CallApiInternalAsync_WithAuthorizationHeaderBoundProviderAndWithBindingCertificate_UsesMtlsHttpClientFactory()
        {
            // Arrange
            var mockBoundProvider = Substitute.For<IAuthorizationHeaderProvider, IBoundAuthorizationHeaderProvider>();
            var mockMtlsHttpClientFactory = Substitute.For<IHttpClientFactory, IMsalHttpClientFactory, IMsalMtlsHttpClientFactory>();
            var testCertificate = CreateTestCertificate();

            var mockHandler = new MockHttpMessageHandler()
            {
                ExpectedMethod = HttpMethod.Get,
                ResponseMessage = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{\"result\": \"success\"}")
                }
            };

            // Create HttpClient with our mock handler
            var mockMtlsHttpClient = new HttpClient(mockHandler);

            var downstreamApi = new DownstreamApi(
                mockBoundProvider,
                _namedDownstreamApiOptions,
                mockMtlsHttpClientFactory,
                _logger,
                (IMsalHttpClientFactory)mockMtlsHttpClientFactory);

            var options = new DownstreamApiOptions
            {
                BaseUrl = "https://api.example.com",
                Scopes = new[] { "https://api.example.com/.default" },
                HttpMethod = "GET",
                ProtocolScheme = "MTLS_POP"
            };

            var authHeaderInfo = new AuthorizationHeaderInformation
            {
                AuthorizationHeaderValue = "Bearer test-token",
                BindingCertificate = testCertificate
            };

            var mockResult = new OperationResult<AuthorizationHeaderInformation, AuthorizationHeaderError>(authHeaderInfo);

            ((IBoundAuthorizationHeaderProvider)mockBoundProvider)
                .CreateBoundAuthorizationHeaderAsync(
                    Arg.Any<DownstreamApiOptions>(),
                    Arg.Any<ClaimsPrincipal>(),
                    Arg.Any<CancellationToken>())
                .Returns(mockResult);

            // Setup mTLS HTTP client factory to return our pre-configured HttpClient
            ((IMsalMtlsHttpClientFactory)mockMtlsHttpClientFactory)
                .GetHttpClient(testCertificate)
                .Returns(mockMtlsHttpClient);

            // Act
            await downstreamApi.CallApiInternalAsync(null, options, false, null, null, CancellationToken.None);

            // Assert - Verify mTLS HTTP client factory was used
            var _ = ((IMsalMtlsHttpClientFactory)mockMtlsHttpClientFactory).Received(1).GetHttpClient(testCertificate);

            // Verify regular HTTP client factory was NOT used
            ((IHttpClientFactory)mockMtlsHttpClientFactory).DidNotReceive().CreateClient(Arg.Any<string>());
        }

        [Fact]
        public async Task CallApiInternalAsync_WithAuthorizationHeaderBoundProviderButWithoutBindingCertificate_UsesRegularHttpClientFactory()
        {
            // Arrange
            var mockBoundProvider = Substitute.For<IAuthorizationHeaderProvider, IBoundAuthorizationHeaderProvider>();
            var mockMtlsHttpClientFactory = Substitute.For<IHttpClientFactory, IMsalHttpClientFactory, IMsalMtlsHttpClientFactory>();

            var mockHandler = new MockHttpMessageHandler()
            {
                ExpectedMethod = HttpMethod.Get,
                ResponseMessage = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{\"result\": \"success\"}")
                }
            };
            var mockRegularHttpClient = new HttpClient(mockHandler);

            var downstreamApi = new DownstreamApi(
                mockBoundProvider,
                _namedDownstreamApiOptions,
                mockMtlsHttpClientFactory,
                _logger,
                (IMsalHttpClientFactory)mockMtlsHttpClientFactory);

            var options = new DownstreamApiOptions
            {
                BaseUrl = "https://api.example.com",
                Scopes = new[] { "https://api.example.com/.default" },
                HttpMethod = "GET",
                ProtocolScheme = "MTLS_POP"
            };

            var authHeaderInfo = new AuthorizationHeaderInformation
            {
                AuthorizationHeaderValue = "Bearer test-token",
                BindingCertificate = null // No binding certificate
            };

            var mockResult = new OperationResult<AuthorizationHeaderInformation, AuthorizationHeaderError>(authHeaderInfo);

            ((IBoundAuthorizationHeaderProvider)mockBoundProvider)
                .CreateBoundAuthorizationHeaderAsync(
                    Arg.Any<DownstreamApiOptions>(),
                    Arg.Any<ClaimsPrincipal>(),
                    Arg.Any<CancellationToken>())
                .Returns(mockResult);

            // Setup regular HTTP client factory to return our mocked HttpClient
            ((IHttpClientFactory)mockMtlsHttpClientFactory)
                .CreateClient(Arg.Any<string>())
                .Returns(mockRegularHttpClient);

            // Act
            await downstreamApi.CallApiInternalAsync(null, options, false, null, null, CancellationToken.None);

            // Assert - Verify regular HTTP client factory was used
            ((IHttpClientFactory)mockMtlsHttpClientFactory).Received(1).CreateClient(Arg.Any<string>());

            // Verify mTLS HTTP client factory was NOT used
            ((IMsalMtlsHttpClientFactory)mockMtlsHttpClientFactory).DidNotReceive().GetHttpClient(Arg.Any<X509Certificate2>());

            // Note: HttpClient is disposed by DownstreamApi, no manual disposal needed
        }

        [Fact]
        public async Task CallApiInternalAsync_WithAuthorizationHeaderBoundProviderWithAuthenticationFailure_ThrowsException()
        {
            // Arrange
            var mockBoundProvider = Substitute.For<IAuthorizationHeaderProvider, IBoundAuthorizationHeaderProvider>();
            var mockHttpClientFactory = Substitute.For<IHttpClientFactory>();

            var downstreamApi = new DownstreamApi(
                mockBoundProvider,
                _namedDownstreamApiOptions,
                mockHttpClientFactory,
                _logger);

            var options = new DownstreamApiOptions
            {
                BaseUrl = "https://api.example.com",
                Scopes = new[] { "https://api.example.com/.default" }
            };

            // Mock authentication failure
            var mockResult = new OperationResult<AuthorizationHeaderInformation, AuthorizationHeaderError>(
                new AuthorizationHeaderError("token_acquisition_failed", "Failed to acquire token"));

            ((IBoundAuthorizationHeaderProvider)mockBoundProvider)
                .CreateBoundAuthorizationHeaderAsync(
                    Arg.Any<DownstreamApiOptions>(),
                    Arg.Any<ClaimsPrincipal>(),
                    Arg.Any<CancellationToken>())
                .Returns(mockResult);

            // Act & Assert
            await Assert.ThrowsAnyAsync<Exception>(() =>
                downstreamApi.CallApiInternalAsync(null, options, false, null, null, CancellationToken.None));
        }

        private static X509Certificate2 CreateTestCertificate()
        {
            // Create a simple test certificate for mocking purposes
            // We don't need a real certificate with private key for HTTP client factory testing
            var bytes = Convert.FromBase64String(TestConstants.CertificateX5c);

#if NET9_0_OR_GREATER
            // Use the new X509CertificateLoader for .NET 9.0+
            return X509CertificateLoader.LoadCertificate(bytes);
#else
            // Use the legacy constructor for older frameworks
#pragma warning disable SYSLIB0057 // Type or member is obsolete
            return new X509Certificate2(bytes);
#pragma warning restore SYSLIB0057 // Type or member is obsolete
#endif
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

    public class MySamlAuthorizationHeaderProvider : IAuthorizationHeaderProvider
    {
        public Task<string> CreateAuthorizationHeaderForAppAsync(string scopes, AuthorizationHeaderProviderOptions? downstreamApiOptions = null, CancellationToken cancellationToken = default)
        {
            return Task.FromResult("http://schemas.microsoft.com/dsts/saml2-bearer ey");
        }

        public Task<string> CreateAuthorizationHeaderForUserAsync(IEnumerable<string> scopes, AuthorizationHeaderProviderOptions? authorizationHeaderProviderOptions = null, ClaimsPrincipal? claimsPrincipal = null, CancellationToken cancellationToken = default)
        {
            return Task.FromResult("http://schemas.microsoft.com/dsts/saml2-bearer ey");
        }

        public Task<string> CreateAuthorizationHeaderAsync(IEnumerable<string> scopes, AuthorizationHeaderProviderOptions? authorizationHeaderProviderOptions = null, ClaimsPrincipal? claimsPrincipal = null, CancellationToken cancellationToken = default)
        {
            return Task.FromResult("http://schemas.microsoft.com/dsts/saml2-bearer ey");
        }
    }
}


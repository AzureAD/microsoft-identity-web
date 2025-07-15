// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Web.Test.Resource;
using Xunit;

namespace Microsoft.Identity.Web.Tests
{
    public class ExtraParametersTests
    {
        private readonly IAuthorizationHeaderProvider _authorizationHeaderProvider;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IOptionsMonitor<DownstreamApiOptions> _namedDownstreamApiOptions;
        private readonly ILogger<DownstreamApi> _logger;
        private readonly DownstreamApi _downstreamApi;

        public ExtraParametersTests()
        {
            _authorizationHeaderProvider = new MyAuthorizationHeaderProvider();
            _httpClientFactory = new HttpClientFactoryTest();
            _namedDownstreamApiOptions = new MyMonitor();
            _logger = new LoggerFactory().CreateLogger<DownstreamApi>();

            _downstreamApi = new DownstreamApi(
                _authorizationHeaderProvider,
                _namedDownstreamApiOptions,
                _httpClientFactory,
                _logger);
        }

        [Fact]
        public async Task UpdateRequestAsync_WithExtraHeaderParameters_AddsHeadersToRequest()
        {
            // Arrange
            var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, "https://example.com/api");
            var options = new DownstreamApiOptions()
            {
                ExtraHeaderParameters = new Dictionary<string, string>
                {
                    { "OData-Version", "4.0" },
                    { "Custom-Header", "test-value" }
                }
            };

            // Act
            await _downstreamApi.UpdateRequestAsync(httpRequestMessage, null, options, false, null, CancellationToken.None);

            // Assert
            Assert.True(httpRequestMessage.Headers.Contains("OData-Version"));
            Assert.True(httpRequestMessage.Headers.Contains("Custom-Header"));
            Assert.Equal("4.0", httpRequestMessage.Headers.GetValues("OData-Version").First());
            Assert.Equal("test-value", httpRequestMessage.Headers.GetValues("Custom-Header").First());
        }

        [Fact]
        public async Task UpdateRequestAsync_WithExtraQueryParameters_AddsQueryParametersToUrl()
        {
            // Arrange
            var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, "https://example.com/api");
            var options = new DownstreamApiOptions()
            {
                ExtraQueryParameters = new Dictionary<string, string>
                {
                    { "param1", "value1" },
                    { "param2", "value2" }
                }
            };

            // Act
            await _downstreamApi.UpdateRequestAsync(httpRequestMessage, null, options, false, null, CancellationToken.None);

            // Assert
            var requestUri = httpRequestMessage.RequestUri!.ToString();
            Assert.Contains("param1=value1", requestUri, StringComparison.Ordinal);
            Assert.Contains("param2=value2", requestUri, StringComparison.Ordinal);
        }

        [Fact]
        public async Task UpdateRequestAsync_WithExtraQueryParameters_AppendsToExistingQuery()
        {
            // Arrange
            var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, "https://example.com/api?existing=true");
            var options = new DownstreamApiOptions()
            {
                ExtraQueryParameters = new Dictionary<string, string>
                {
                    { "new", "param" }
                }
            };

            // Act
            await _downstreamApi.UpdateRequestAsync(httpRequestMessage, null, options, false, null, CancellationToken.None);

            // Assert
            var requestUri = httpRequestMessage.RequestUri!.ToString();
            Assert.Contains("existing=true", requestUri, StringComparison.Ordinal);
            Assert.Contains("new=param", requestUri, StringComparison.Ordinal);
        }

        [Fact]
        public async Task UpdateRequestAsync_WithoutExtraParameters_DoesNotModifyRequest()
        {
            // Arrange
            var originalUri = "https://example.com/api";
            var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, originalUri);
            var options = new DownstreamApiOptions(); // No extra parameters

            // Act
            await _downstreamApi.UpdateRequestAsync(httpRequestMessage, null, options, false, null, CancellationToken.None);

            // Assert
            Assert.Equal(originalUri, httpRequestMessage.RequestUri!.ToString());
        }


        [Fact]
        public async Task UpdateRequestAsync_WithEmptyExtraParameters_DoesNotModifyRequest()
        {
            // Arrange
            var originalUri = "https://example.com/api";
            var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, originalUri);
            var options = new DownstreamApiOptions()
            {
                ExtraHeaderParameters = new Dictionary<string, string>(), // Empty dictionary
                ExtraQueryParameters = new Dictionary<string, string>()  // Empty dictionary
            };

            // Act
            await _downstreamApi.UpdateRequestAsync(httpRequestMessage, null, options, false, null, CancellationToken.None);

            // Assert
            Assert.Equal(originalUri, httpRequestMessage.RequestUri!.ToString());
        }

        [Fact]
        public async Task UpdateRequestAsync_WithSpecialCharacters_EscapesCorrectly()
        {
            // Arrange
            var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, "https://example.com/api");
            var options = new DownstreamApiOptions()
            {
                ExtraQueryParameters = new Dictionary<string, string>
                {
                    { "special", "value with spaces & symbols" }
                }
            };

            // Act
            await _downstreamApi.UpdateRequestAsync(httpRequestMessage, null, options, false, null, CancellationToken.None);

            // Assert
            var requestUri = httpRequestMessage.RequestUri!.ToString();
            Assert.Contains("special=value with spaces %26 symbols", requestUri, StringComparison.Ordinal);
        }

        private class MyAuthorizationHeaderProvider : IAuthorizationHeaderProvider
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

        private class MyMonitor : IOptionsMonitor<DownstreamApiOptions>
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
    }
}

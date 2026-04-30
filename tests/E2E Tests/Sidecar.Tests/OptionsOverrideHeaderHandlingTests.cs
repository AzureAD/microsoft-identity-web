// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Net;
using System.Net.Http.Headers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Web;
using Xunit;

namespace Sidecar.Tests;

/// <summary>
/// Validates how the library treats caller-supplied entries in
/// <see cref="DownstreamApiOptions.ExtraHeaderParameters"/> when those entries
/// arrive through the sidecar's <c>optionsOverride.*</c> query parameters.
/// </summary>
public class OptionsOverrideHeaderHandlingTests(SidecarApiFactory factory) : IClassFixture<SidecarApiFactory>
{
    private readonly SidecarApiFactory _factory = factory;

    [Fact(Skip = "Pending dependency merge")]
    public async Task DownstreamApi_OptionsOverrideExtraHeaderParameters_AuthorizationOverride_DoesNotReachOutboundRequest()
    {
        // Arrange
        const string apiName = "test-api";
        const string libraryAuthHeader = "Bearer library-issued-token";
        var capture = new CapturingHttpMessageHandler(HttpStatusCode.OK);

        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                TestAuthenticationHandler.AddAlwaysSucceedTestAuthentication(services);

                services.AddSingleton<IAuthorizationHeaderProvider>(
                    new TestAuthorizationHeaderProvider { Result = libraryAuthHeader });

                services.AddSingleton<IHttpClientFactory>(new CapturingHttpClientFactory(capture));

                services.Configure<DownstreamApiOptions>(apiName, options =>
                {
                    options.BaseUrl = "https://api.example.com";
                    options.RelativePath = "/test";
                    options.HttpMethod = HttpMethod.Get.Method;
                    options.Scopes = new[] { "User.Read" };
                });
            });
        }).CreateClient();

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "valid-test-token");

        // Act ΓÇö caller tries to inject a different Authorization value via optionsOverride.
        var response = await client.PostAsync(
            $"/DownstreamApi/{apiName}?optionsOverride.ExtraHeaderParameters.Authorization=Bearer%20caller-supplied",
            content: null);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(capture.LastRequest);
        var authValues = capture.LastRequest!.Headers.GetValues("Authorization").ToArray();
        Assert.Single(authValues);
        Assert.Equal(libraryAuthHeader, authValues[0]);
    }

    [Fact(Skip = "Pending dependency merge")]
    public async Task DownstreamApi_OptionsOverrideExtraHeaderParameters_AllowedHeader_IsForwarded()
    {
        // Arrange
        const string apiName = "test-api";
        var capture = new CapturingHttpMessageHandler(HttpStatusCode.OK);

        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                TestAuthenticationHandler.AddAlwaysSucceedTestAuthentication(services);

                services.AddSingleton<IAuthorizationHeaderProvider>(
                    new TestAuthorizationHeaderProvider { Result = "Bearer library-issued-token" });

                services.AddSingleton<IHttpClientFactory>(new CapturingHttpClientFactory(capture));

                services.Configure<DownstreamApiOptions>(apiName, options =>
                {
                    options.BaseUrl = "https://api.example.com";
                    options.RelativePath = "/test";
                    options.HttpMethod = HttpMethod.Get.Method;
                    options.Scopes = new[] { "User.Read" };
                });
            });
        }).CreateClient();

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "valid-test-token");

        // Act
        var response = await client.PostAsync(
            $"/DownstreamApi/{apiName}?optionsOverride.ExtraHeaderParameters.X-Custom-Tracking=trace-id-42",
            content: null);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(capture.LastRequest);
        Assert.True(capture.LastRequest!.Headers.Contains("X-Custom-Tracking"));
        Assert.Equal("trace-id-42", capture.LastRequest!.Headers.GetValues("X-Custom-Tracking").Single());
    }

    private sealed class CapturingHttpMessageHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _statusCode;

        public CapturingHttpMessageHandler(HttpStatusCode statusCode)
        {
            _statusCode = statusCode;
        }

        public HttpRequestMessage? LastRequest { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            return Task.FromResult(new HttpResponseMessage(_statusCode));
        }
    }

    private sealed class CapturingHttpClientFactory : IHttpClientFactory
    {
        private readonly CapturingHttpMessageHandler _handler;

        public CapturingHttpClientFactory(CapturingHttpMessageHandler handler)
        {
            _handler = handler;
        }

        public HttpClient CreateClient(string name) => new(_handler, disposeHandler: false);
    }
}

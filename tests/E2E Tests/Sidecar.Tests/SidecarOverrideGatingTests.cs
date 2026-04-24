// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Web;
using Xunit;

namespace Sidecar.Tests;

/// <summary>
/// Exercises the per-route <c>Sidecar:AllowOverrides</c> configuration that
/// controls whether <c>optionsOverride.*</c> query parameters are applied to a
/// resolved <see cref="DownstreamApiOptions"/> instance, and verifies that
/// <c>optionsOverride.BaseUrl</c> is unconditionally dropped on every route.
/// </summary>
public class SidecarOverrideGatingTests : IClassFixture<SidecarApiFactory>
{
    private readonly SidecarApiFactory _factory;

    public SidecarOverrideGatingTests(SidecarApiFactory factory)
    {
        _factory = factory;
    }

    private HttpClient CreateClient(
        Dictionary<string, string?> sidecarConfig,
        IAuthorizationHeaderProvider headerProvider,
        Action<DownstreamApiOptions> configureApi,
        string apiName,
        IHttpClientFactory? httpClientFactory = null)
    {
        return _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(sidecarConfig);
            });

            builder.ConfigureServices(services =>
            {
                TestAuthenticationHandler.AddAlwaysSucceedTestAuthentication(services);
                services.AddSingleton(headerProvider);
                services.Configure<DownstreamApiOptions>(apiName, configureApi);

                if (httpClientFactory is not null)
                {
                    services.AddSingleton(httpClientFactory);
                }
            });
        }).CreateClient();
    }

    private static void AddBearer(HttpClient client) =>
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "valid-test-token");

    [Fact]
    public async Task AuthorizationHeader_OverridesAllowed_AppliesScopeOverride()
    {
        const string apiName = "test-api";
        var capture = new CapturingAuthorizationHeaderProvider { Result = "Bearer t" };

        var client = CreateClient(
            new Dictionary<string, string?>
            {
                { "Sidecar:AllowOverrides:GetAuthorizationHeader", "true" },
            },
            capture,
            options =>
            {
                options.BaseUrl = "https://api.example.com";
                options.RelativePath = "/test";
                options.Scopes = new[] { "User.Read" };
            },
            apiName);
        AddBearer(client);

        var response = await client.GetAsync($"/AuthorizationHeader/{apiName}?optionsOverride.Scopes=Mail.Read");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(capture.LastScopes);
        Assert.Contains("Mail.Read", capture.LastScopes!);
    }

    [Fact]
    public async Task AuthorizationHeader_OverridesDisabled_IgnoresScopeOverride()
    {
        const string apiName = "test-api";
        var capture = new CapturingAuthorizationHeaderProvider { Result = "Bearer t" };

        var client = CreateClient(
            new Dictionary<string, string?>
            {
                { "Sidecar:AllowOverrides:GetAuthorizationHeader", "false" },
            },
            capture,
            options =>
            {
                options.BaseUrl = "https://api.example.com";
                options.RelativePath = "/test";
                options.Scopes = new[] { "User.Read" };
            },
            apiName);
        AddBearer(client);

        var response = await client.GetAsync($"/AuthorizationHeader/{apiName}?optionsOverride.Scopes=Mail.Read");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(capture.LastScopes);
        Assert.DoesNotContain("Mail.Read", capture.LastScopes!);
        Assert.Contains("User.Read", capture.LastScopes!);
    }

    [Fact]
    public async Task AuthorizationHeaderUnauthenticated_DefaultIgnoresOverride()
    {
        const string apiName = "test-api";
        var capture = new CapturingAuthorizationHeaderProvider { Result = "Bearer t" };

        // No explicit Sidecar config: default is GetAuthorizationHeaderUnauthenticated=false.
        var client = CreateClient(
            new Dictionary<string, string?>(),
            capture,
            options =>
            {
                options.BaseUrl = "https://api.example.com";
                options.RelativePath = "/test";
                options.Scopes = new[] { "User.Read" };
            },
            apiName);

        var response = await client.GetAsync($"/AuthorizationHeaderUnauthenticated/{apiName}?optionsOverride.Scopes=Mail.Read");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(capture.LastScopes);
        Assert.DoesNotContain("Mail.Read", capture.LastScopes!);
    }

    [Fact]
    public async Task AuthorizationHeaderUnauthenticated_OverridesEnabled_AppliesOverride()
    {
        const string apiName = "test-api";
        var capture = new CapturingAuthorizationHeaderProvider { Result = "Bearer t" };

        var client = CreateClient(
            new Dictionary<string, string?>
            {
                { "Sidecar:AllowOverrides:GetAuthorizationHeaderUnauthenticated", "true" },
            },
            capture,
            options =>
            {
                options.BaseUrl = "https://api.example.com";
                options.RelativePath = "/test";
                options.Scopes = new[] { "User.Read" };
            },
            apiName);

        var response = await client.GetAsync($"/AuthorizationHeaderUnauthenticated/{apiName}?optionsOverride.Scopes=Mail.Read");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(capture.LastScopes);
        Assert.Contains("Mail.Read", capture.LastScopes!);
    }

    [Fact]
    public async Task DownstreamApi_BaseUrlOverride_AlwaysDropped_OverridesEnabled()
    {
        const string apiName = "test-api";
        var capture = new CapturingHttpMessageHandler(HttpStatusCode.OK);
        var headerProvider = new CapturingAuthorizationHeaderProvider { Result = "Bearer library-token" };

        var client = CreateClient(
            new Dictionary<string, string?>
            {
                { "Sidecar:AllowOverrides:CallDownstreamApi", "true" },
            },
            headerProvider,
            options =>
            {
                options.BaseUrl = "https://api.example.com";
                options.RelativePath = "/test";
                options.HttpMethod = HttpMethod.Get.Method;
                options.Scopes = new[] { "User.Read" };
            },
            apiName,
            httpClientFactory: new SingleHandlerHttpClientFactory(capture));
        AddBearer(client);

        var response = await client.PostAsync(
            $"/DownstreamApi/{apiName}?optionsOverride.BaseUrl=https://evil.example.com",
            content: null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(capture.LastRequest);
        Assert.StartsWith("https://api.example.com", capture.LastRequest!.RequestUri!.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task DownstreamApi_BaseUrlOverride_AlwaysDropped_OverridesDisabled()
    {
        const string apiName = "test-api";
        var capture = new CapturingHttpMessageHandler(HttpStatusCode.OK);
        var headerProvider = new CapturingAuthorizationHeaderProvider { Result = "Bearer library-token" };

        var client = CreateClient(
            new Dictionary<string, string?>
            {
                { "Sidecar:AllowOverrides:CallDownstreamApi", "false" },
            },
            headerProvider,
            options =>
            {
                options.BaseUrl = "https://api.example.com";
                options.RelativePath = "/test";
                options.HttpMethod = HttpMethod.Get.Method;
                options.Scopes = new[] { "User.Read" };
            },
            apiName,
            httpClientFactory: new SingleHandlerHttpClientFactory(capture));
        AddBearer(client);

        var response = await client.PostAsync(
            $"/DownstreamApi/{apiName}?optionsOverride.BaseUrl=https://evil.example.com",
            content: null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(capture.LastRequest);
        Assert.StartsWith("https://api.example.com", capture.LastRequest!.RequestUri!.ToString(), StringComparison.Ordinal);
    }

    private sealed class CapturingAuthorizationHeaderProvider : IAuthorizationHeaderProvider
    {
        public string? Result { get; init; }

        public IEnumerable<string>? LastScopes { get; private set; }

        public Task<string> CreateAuthorizationHeaderAsync(
            IEnumerable<string> scopes,
            AuthorizationHeaderProviderOptions? options = null,
            ClaimsPrincipal? claimsPrincipal = null,
            CancellationToken cancellationToken = default)
        {
            LastScopes = scopes?.ToArray();
            return Task.FromResult(Result ?? string.Empty);
        }

        public Task<string> CreateAuthorizationHeaderForAppAsync(
            string scopes,
            AuthorizationHeaderProviderOptions? downstreamApiOptions = null,
            CancellationToken cancellationToken = default)
        {
            LastScopes = string.IsNullOrEmpty(scopes) ? Array.Empty<string>() : new[] { scopes };
            return Task.FromResult(Result ?? string.Empty);
        }

        public Task<string> CreateAuthorizationHeaderForUserAsync(
            IEnumerable<string> scopes,
            AuthorizationHeaderProviderOptions? authorizationHeaderProviderOptions = null,
            ClaimsPrincipal? claimsPrincipal = null,
            CancellationToken cancellationToken = default)
        {
            LastScopes = scopes?.ToArray();
            return Task.FromResult(Result ?? string.Empty);
        }
    }

    private sealed class CapturingHttpMessageHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _statusCode;

        public CapturingHttpMessageHandler(HttpStatusCode statusCode)
        {
            _statusCode = statusCode;
        }

        public HttpRequestMessage? LastRequest { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            return Task.FromResult(new HttpResponseMessage(_statusCode));
        }
    }

    private sealed class SingleHandlerHttpClientFactory : IHttpClientFactory
    {
        private readonly HttpMessageHandler _handler;

        public SingleHandlerHttpClientFactory(HttpMessageHandler handler)
        {
            _handler = handler;
        }

        public HttpClient CreateClient(string name) => new(_handler, disposeHandler: false);
    }
}

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
/// Verifies that <c>AgentIdentity</c>, <c>AgentUsername</c>, and <c>AgentUserId</c>
/// query parameters are subject to the per-route <c>Sidecar:AllowOverrides</c> gate
/// and are not applied on routes where overrides are disabled.
/// </summary>
public class AgentOverrideGatingTests : IClassFixture<SidecarApiFactory>
{
    private readonly SidecarApiFactory _factory;

    // The key used by WithAgentUserIdentity / WithAgentIdentity to store the
    // agent identity in AcquireTokenOptions.ExtraParameters.
    private const string AgentIdentityKey = "IDWEB_AGENT_IDENTITY";

    public AgentOverrideGatingTests(SidecarApiFactory factory)
    {
        _factory = factory;
    }

    private HttpClient CreateClient(
        Dictionary<string, string?> sidecarConfig,
        OptionsCapturingAuthorizationHeaderProvider capture,
        string apiName = "test-api")
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
                services.AddSingleton<IAuthorizationHeaderProvider>(capture);
                services.Configure<DownstreamApiOptions>(apiName, options =>
                {
                    options.BaseUrl = "https://api.example.com";
                    options.RelativePath = "/test";
                    options.Scopes = new[] { "User.Read" };
                });
            });
        }).CreateClient();
    }

    private static void AddBearer(HttpClient client) =>
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "valid-test-token");

    // ── AuthorizationHeader (authenticated) ──────────────────────────────

    [Fact]
    public async Task AuthorizationHeader_OverridesAllowed_AppliesAgentIdentity()
    {
        var capture = new OptionsCapturingAuthorizationHeaderProvider();
        var client = CreateClient(
            new Dictionary<string, string?>
            {
                { "Sidecar:AllowOverrides:GetAuthorizationHeader", "true" },
            },
            capture);
        AddBearer(client);

        var response = await client.GetAsync(
            "/AuthorizationHeader/test-api?AgentIdentity=agent-app-id&AgentUsername=testuser@contoso.com");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(capture.LastOptions);
        Assert.NotNull(capture.LastOptions!.AcquireTokenOptions.ExtraParameters);
        Assert.True(capture.LastOptions.AcquireTokenOptions.ExtraParameters!.ContainsKey(AgentIdentityKey));
        Assert.Equal("agent-app-id", capture.LastOptions.AcquireTokenOptions.ExtraParameters[AgentIdentityKey]);
    }

    [Fact]
    public async Task AuthorizationHeader_OverridesDisabled_IgnoresAgentIdentity()
    {
        var capture = new OptionsCapturingAuthorizationHeaderProvider();
        var client = CreateClient(
            new Dictionary<string, string?>
            {
                { "Sidecar:AllowOverrides:GetAuthorizationHeader", "false" },
            },
            capture);
        AddBearer(client);

        var response = await client.GetAsync(
            "/AuthorizationHeader/test-api?AgentIdentity=agent-app-id&AgentUsername=testuser@contoso.com");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(capture.LastOptions);
        // Agent identity should NOT have been applied.
        var extras = capture.LastOptions!.AcquireTokenOptions.ExtraParameters;
        Assert.True(
            extras is null || !extras.ContainsKey(AgentIdentityKey),
            "Agent identity should be ignored when overrides are disabled.");
    }

    // ── AuthorizationHeaderUnauthenticated ────────────────────────────────

    [Fact]
    public async Task AuthorizationHeaderUnauthenticated_DefaultConfig_IgnoresAgentIdentity()
    {
        var capture = new OptionsCapturingAuthorizationHeaderProvider();
        // No explicit config — default is GetAuthorizationHeaderUnauthenticated = false.
        var client = CreateClient(new Dictionary<string, string?>(), capture);

        var response = await client.GetAsync(
            "/AuthorizationHeaderUnauthenticated/test-api?AgentIdentity=agent-app-id&AgentUsername=testuser@contoso.com");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(capture.LastOptions);
        var extras = capture.LastOptions!.AcquireTokenOptions.ExtraParameters;
        Assert.True(
            extras is null || !extras.ContainsKey(AgentIdentityKey),
            "Agent identity should be ignored on unauthenticated route by default.");
    }

    [Fact]
    public async Task AuthorizationHeaderUnauthenticated_OverridesEnabled_AppliesAgentIdentity()
    {
        var capture = new OptionsCapturingAuthorizationHeaderProvider();
        var client = CreateClient(
            new Dictionary<string, string?>
            {
                { "Sidecar:AllowOverrides:GetAuthorizationHeaderUnauthenticated", "true" },
            },
            capture);

        var response = await client.GetAsync(
            "/AuthorizationHeaderUnauthenticated/test-api?AgentIdentity=agent-app-id&AgentUsername=testuser@contoso.com");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(capture.LastOptions);
        Assert.NotNull(capture.LastOptions!.AcquireTokenOptions.ExtraParameters);
        Assert.True(capture.LastOptions.AcquireTokenOptions.ExtraParameters!.ContainsKey(AgentIdentityKey));
    }

    // ── DownstreamApi (authenticated) ────────────────────────────────────

    [Fact]
    public async Task DownstreamApi_OverridesDisabled_IgnoresAgentIdentity()
    {
        var capture = new OptionsCapturingAuthorizationHeaderProvider();
        var captureHandler = new CapturingHttpMessageHandler(HttpStatusCode.OK);

        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    { "Sidecar:AllowOverrides:CallDownstreamApi", "false" },
                });
            });

            builder.ConfigureServices(services =>
            {
                TestAuthenticationHandler.AddAlwaysSucceedTestAuthentication(services);
                services.AddSingleton<IAuthorizationHeaderProvider>(capture);
                services.Configure<DownstreamApiOptions>("test-api", options =>
                {
                    options.BaseUrl = "https://api.example.com";
                    options.RelativePath = "/test";
                    options.HttpMethod = HttpMethod.Get.Method;
                    options.Scopes = new[] { "User.Read" };
                });
                services.AddSingleton<IHttpClientFactory>(new SingleHandlerHttpClientFactory(captureHandler));
            });
        }).CreateClient();
        AddBearer(client);

        var response = await client.PostAsync(
            "/DownstreamApi/test-api?AgentIdentity=agent-app-id&AgentUsername=testuser@contoso.com",
            content: null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(capture.LastOptions);
        var extras = capture.LastOptions!.AcquireTokenOptions.ExtraParameters;
        Assert.True(
            extras is null || !extras.ContainsKey(AgentIdentityKey),
            "Agent identity should be ignored when overrides are disabled on DownstreamApi.");
    }

    // ── DownstreamApiUnauthenticated ─────────────────────────────────────

    [Fact]
    public async Task DownstreamApiUnauthenticated_DefaultConfig_IgnoresAgentIdentity()
    {
        var capture = new OptionsCapturingAuthorizationHeaderProvider();
        var captureHandler = new CapturingHttpMessageHandler(HttpStatusCode.OK);

        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>());
            });

            builder.ConfigureServices(services =>
            {
                TestAuthenticationHandler.AddAlwaysSucceedTestAuthentication(services);
                services.AddSingleton<IAuthorizationHeaderProvider>(capture);
                services.Configure<DownstreamApiOptions>("test-api", options =>
                {
                    options.BaseUrl = "https://api.example.com";
                    options.RelativePath = "/test";
                    options.HttpMethod = HttpMethod.Get.Method;
                    options.Scopes = new[] { "User.Read" };
                });
                services.AddSingleton<IHttpClientFactory>(new SingleHandlerHttpClientFactory(captureHandler));
            });
        }).CreateClient();

        var response = await client.PostAsync(
            "/DownstreamApiUnauthenticated/test-api?AgentIdentity=agent-app-id&AgentUsername=testuser@contoso.com",
            content: null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(capture.LastOptions);
        var extras = capture.LastOptions!.AcquireTokenOptions.ExtraParameters;
        Assert.True(
            extras is null || !extras.ContainsKey(AgentIdentityKey),
            "Agent identity should be ignored on unauthenticated DownstreamApi route by default.");
    }

    // ── Helpers ──────────────────────────────────────────────────────────

    /// <summary>
    /// Captures the <see cref="AuthorizationHeaderProviderOptions"/> passed to
    /// <see cref="IAuthorizationHeaderProvider"/> so tests can assert on agent
    /// identity fields.
    /// </summary>
    private sealed class OptionsCapturingAuthorizationHeaderProvider : IAuthorizationHeaderProvider
    {
        public AuthorizationHeaderProviderOptions? LastOptions { get; private set; }

        public Task<string> CreateAuthorizationHeaderAsync(
            IEnumerable<string> scopes,
            AuthorizationHeaderProviderOptions? options = null,
            ClaimsPrincipal? claimsPrincipal = null,
            CancellationToken cancellationToken = default)
        {
            LastOptions = options;
            return Task.FromResult("Bearer test-token");
        }

        public Task<string> CreateAuthorizationHeaderForAppAsync(
            string scopes,
            AuthorizationHeaderProviderOptions? downstreamApiOptions = null,
            CancellationToken cancellationToken = default)
        {
            LastOptions = downstreamApiOptions;
            return Task.FromResult("Bearer test-token");
        }

        public Task<string> CreateAuthorizationHeaderForUserAsync(
            IEnumerable<string> scopes,
            AuthorizationHeaderProviderOptions? authorizationHeaderProviderOptions = null,
            ClaimsPrincipal? claimsPrincipal = null,
            CancellationToken cancellationToken = default)
        {
            LastOptions = authorizationHeaderProviderOptions;
            return Task.FromResult("Bearer test-token");
        }
    }

    private sealed class CapturingHttpMessageHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _statusCode;

        public CapturingHttpMessageHandler(HttpStatusCode statusCode)
        {
            _statusCode = statusCode;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
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

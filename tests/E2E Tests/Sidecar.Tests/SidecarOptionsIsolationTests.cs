// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.Sidecar;
using Microsoft.Identity.Web.Sidecar.Models;
using Moq;
using Xunit;

namespace Sidecar.Tests;

/// <summary>
/// Verifies that per-request downstream API options are isolated from the
/// process-wide <see cref="IOptionsMonitor{TOptions}"/> singleton.
/// </summary>
public class SidecarOptionsIsolationTests : IDisposable
{
    private const string ApiName = "test-api";

    // Key used by WithAgentUserIdentity to store the agent identity in
    // AcquireTokenOptions.ExtraParameters (Constants.AgentIdentityKey).
    private const string AgentIdentityKey = "IDWEB_AGENT_IDENTITY";

    [Fact]
    public void Clone_IsShallow_OnSharedMutableCollections()
    {
        var original = new DownstreamApiOptions
        {
            Scopes = new[] { "User.Read" },
            ExtraHeaderParameters = new Dictionary<string, string> { ["h"] = "hv" },
            ExtraQueryParameters = new Dictionary<string, string> { ["q"] = "qv" },
        };
        original.AcquireTokenOptions.ExtraParameters = new Dictionary<string, object> { ["k"] = "v" };

        var clone = original.Clone();

        Assert.NotSame(original.AcquireTokenOptions, clone.AcquireTokenOptions);
        Assert.Same(original.AcquireTokenOptions.ExtraParameters, clone.AcquireTokenOptions.ExtraParameters);
        Assert.Same(original.ExtraHeaderParameters, clone.ExtraHeaderParameters);
        Assert.Same(original.ExtraQueryParameters, clone.ExtraQueryParameters);

        clone.AcquireTokenOptions.ExtraParameters!["k2"] = "v2";
        Assert.True(original.AcquireTokenOptions.ExtraParameters!.ContainsKey("k2"));
    }

    /// <summary>
    /// CloneForRequest re-allocates each shared collection, so writes to the copy
    /// don't affect the original.
    /// </summary>
    [Fact]
    public void CloneForRequest_ReallocatesSharedMutableCollections()
    {
        var original = new DownstreamApiOptions
        {
            Scopes = new[] { "User.Read" },
            ExtraHeaderParameters = new Dictionary<string, string> { ["h"] = "hv" },
            ExtraQueryParameters = new Dictionary<string, string> { ["q"] = "qv" },
        };
        original.AcquireTokenOptions.ExtraParameters = new Dictionary<string, object> { ["k"] = "v" };

        var isolated = DownstreamApiOptionsMerger.CloneForRequest(original);

        Assert.NotSame(original.AcquireTokenOptions.ExtraParameters, isolated.AcquireTokenOptions.ExtraParameters);
        Assert.NotSame(original.ExtraHeaderParameters, isolated.ExtraHeaderParameters);
        Assert.NotSame(original.ExtraQueryParameters, isolated.ExtraQueryParameters);

        isolated.AcquireTokenOptions.ExtraParameters!["k2"] = "v2";
        isolated.ExtraHeaderParameters!["h2"] = "hv2";
        isolated.ExtraQueryParameters!["q2"] = "qv2";

        Assert.Single(original.AcquireTokenOptions.ExtraParameters!);
        Assert.Single(original.ExtraHeaderParameters!);
        Assert.Single(original.ExtraQueryParameters!);
    }

    // ── AuthorizationHeader: an agent identity does not affect later requests ──

    [Fact]
    public async Task AuthorizationHeaderUnauthenticated_AgentIdentity_IsNotRetainedForLaterRequests()
    {
        var recorder = new RecordingAuthorizationHeaderProvider();

        using var factory = CreateHeaderFactory(
            recorder,
            new Dictionary<string, string?>
            {
                { "Sidecar:AllowOverrides:GetAuthorizationHeaderUnauthenticated", "true" },
            });
        using var client = factory.CreateClient();

        // The config-bound singleton starts with a null ExtraParameters.
        var monitor = factory.Services.GetRequiredService<IOptionsMonitor<DownstreamApiOptions>>();
        Assert.Null(monitor.Get(ApiName).AcquireTokenOptions.ExtraParameters);

        const string appA = "app-a-11111111-1111-1111-1111-111111111111";
        var r1 = await client.GetAsync(
            $"/AuthorizationHeaderUnauthenticated/{ApiName}?agentIdentity={appA}&agentUsername=usera@contoso.com");
        Assert.Equal(HttpStatusCode.OK, r1.StatusCode);

        var r2 = await client.GetAsync($"/AuthorizationHeaderUnauthenticated/{ApiName}");
        Assert.Equal(HttpStatusCode.OK, r2.StatusCode);

        var request2Agent = recorder.Recorded[^1];
        Assert.Null(request2Agent);

        var singletonExtras = monitor.Get(ApiName).AcquireTokenOptions.ExtraParameters;
        Assert.True(
            singletonExtras is null || !singletonExtras.ContainsKey(AgentIdentityKey),
            "The IOptionsMonitor singleton must not retain the previous request's agent identity.");
    }

    // ── AuthorizationHeader: concurrent requests keep distinct agent identities ──

    [Fact]
    public async Task AuthorizationHeaderUnauthenticated_ConcurrentDistinctAgents_EachRequestUsesItsOwnIdentity()
    {
        // Re-read after a delay so requests that share mutable state observe each other's writes.
        var recorder = new RecordingAuthorizationHeaderProvider(delayMs: 50);

        using var factory = CreateHeaderFactory(
            recorder,
            new Dictionary<string, string?>
            {
                { "Sidecar:AllowOverrides:GetAuthorizationHeaderUnauthenticated", "true" },
            });
        using var client = factory.CreateClient();

        const int concurrency = 40;
        var identities = Enumerable.Range(0, concurrency)
            .Select(i => $"agent-{i}-{Guid.NewGuid()}")
            .ToArray();

        var tasks = identities.Select(async id =>
        {
            var response = await client.GetAsync(
                $"/AuthorizationHeaderUnauthenticated/{ApiName}?agentIdentity={id}&agentUsername={id}@contoso.com");
            var body = response.StatusCode == HttpStatusCode.OK
                ? await response.Content.ReadFromJsonAsync<Microsoft.Identity.Web.Sidecar.Models.AuthorizationHeaderResult>()
                : null;
            return (id, response.StatusCode, used: body?.AuthorizationHeader);
        }).ToArray();

        var results = await Task.WhenAll(tasks);

        // A request can fail if a shared Dictionary is mutated concurrently
        // (InvalidOperationException -> HTTP 500).
        Assert.All(results, r => Assert.Equal(HttpStatusCode.OK, r.StatusCode));

        var mismatched = results.Where(r => r.used != r.id).ToArray();
        Assert.True(
            mismatched.Length == 0,
            $"{mismatched.Length}/{concurrency} requests observed a different agent identity than they supplied. " +
            $"Examples: {string.Join("; ", mismatched.Take(3).Select(c => $"sent={c.id} used={c.used ?? "<null>"}"))}");
    }

    // ── DownstreamApi: an agent identity does not affect later requests ──

    [Fact]
    public async Task DownstreamApiUnauthenticated_AgentIdentity_IsNotRetainedForLaterRequests()
    {
        var recorded = new List<string?>();
        var mockDownstream = new Mock<IDownstreamApi>();
        mockDownstream
            .Setup(d => d.CallApiAsync(
                It.IsAny<DownstreamApiOptions>(),
                It.IsAny<ClaimsPrincipal>(),
                It.IsAny<HttpContent>(),
                It.IsAny<CancellationToken>()))
            .Callback<DownstreamApiOptions, ClaimsPrincipal, HttpContent?, CancellationToken>((opts, _, _, _) =>
            {
                recorded.Add(GetAgentIdentity(opts));
            })
            .ReturnsAsync(() => new HttpResponseMessage(HttpStatusCode.OK));

        using var factory = _sharedFactory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    { "Sidecar:AllowOverrides:CallDownstreamApiUnauthenticated", "true" },
                });
            });
            builder.ConfigureServices(services =>
            {
                services.Configure<DownstreamApiOptions>(ApiName, o =>
                {
                    o.BaseUrl = "https://api.example.com";
                    o.RelativePath = "/test";
                    o.HttpMethod = HttpMethod.Get.Method;
                    o.Scopes = new[] { "User.Read" };
                });
                services.AddSingleton(mockDownstream.Object);
            });
        });
        using var client = factory.CreateClient();

        var monitor = factory.Services.GetRequiredService<IOptionsMonitor<DownstreamApiOptions>>();
        Assert.Null(monitor.Get(ApiName).AcquireTokenOptions.ExtraParameters);

        const string appA = "app-a-22222222-2222-2222-2222-222222222222";
        var r1 = await client.PostAsync(
            $"/DownstreamApiUnauthenticated/{ApiName}?agentIdentity={appA}&agentUsername=usera@contoso.com",
            content: null);
        Assert.Equal(HttpStatusCode.OK, r1.StatusCode);

        var r2 = await client.PostAsync($"/DownstreamApiUnauthenticated/{ApiName}", content: null);
        Assert.Equal(HttpStatusCode.OK, r2.StatusCode);

        Assert.Null(recorded[^1]);

        var singletonExtras = monitor.Get(ApiName).AcquireTokenOptions.ExtraParameters;
        Assert.True(
            singletonExtras is null || !singletonExtras.ContainsKey(AgentIdentityKey),
            "The IOptionsMonitor singleton must not retain the previous request's agent identity.");
    }

    // ── Non-null singleton collections are not modified by a request ──

    [Fact]
    public async Task AuthorizationHeaderUnauthenticated_NonNullSingletonExtraParameters_NotMutatedByRequest()
    {
        const string seedKey = "seed-key";
        const string seedValue = "seed-value";

        var recorder = new RecordingAuthorizationHeaderProvider();

        using var factory = _sharedFactory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    { "Sidecar:AllowOverrides:GetAuthorizationHeaderUnauthenticated", "true" },
                });
            });
            builder.ConfigureServices(services =>
            {
                services.AddSingleton<IAuthorizationHeaderProvider>(recorder);
                services.Configure<DownstreamApiOptions>(ApiName, o =>
                {
                    o.BaseUrl = "https://api.example.com";
                    o.RelativePath = "/test";
                    o.Scopes = new[] { "User.Read" };
                    // Pre-existing (non-null) ExtraParameters, which a shallow clone alone would not isolate.
                    o.AcquireTokenOptions.ExtraParameters = new Dictionary<string, object>
                    {
                        [seedKey] = seedValue,
                    };
                });
            });
        });
        using var client = factory.CreateClient();

        var monitor = factory.Services.GetRequiredService<IOptionsMonitor<DownstreamApiOptions>>();
        var seededExtras = monitor.Get(ApiName).AcquireTokenOptions.ExtraParameters;
        Assert.NotNull(seededExtras);
        Assert.Single(seededExtras!);

        const string appA = "app-a-33333333-3333-3333-3333-333333333333";
        var r1 = await client.GetAsync(
            $"/AuthorizationHeaderUnauthenticated/{ApiName}?agentIdentity={appA}&agentUsername=usera@contoso.com");
        Assert.Equal(HttpStatusCode.OK, r1.StatusCode);

        var afterExtras = monitor.Get(ApiName).AcquireTokenOptions.ExtraParameters;
        Assert.NotNull(afterExtras);
        Assert.False(
            afterExtras!.ContainsKey(AgentIdentityKey),
            "The seeded singleton ExtraParameters dictionary must not receive the request's agent identity.");
        Assert.Single(afterExtras);
        Assert.Equal(seedValue, afterExtras[seedKey]);
    }

    // ── Merge path does not modify the singleton's collections ──

    [Fact]
    public async Task MergePath_WithNonNullSingletonCollections_DoesNotMutateSingleton()
    {
        const string seedParamKey = "seed-param";
        const string seedHeaderKey = "seed-header";
        const string seedQueryKey = "seed-query";

        var recorder = new RecordingAuthorizationHeaderProvider();

        using var factory = _sharedFactory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    { "Sidecar:AllowOverrides:GetAuthorizationHeaderUnauthenticated", "true" },
                });
            });
            builder.ConfigureServices(services =>
            {
                services.AddSingleton<IAuthorizationHeaderProvider>(recorder);
                services.Configure<DownstreamApiOptions>(ApiName, o =>
                {
                    o.BaseUrl = "https://api.example.com";
                    o.RelativePath = "/test";
                    o.Scopes = new[] { "User.Read" };
                    // Non-null collections that MergeOptions and SetOverrides write to in place.
                    o.AcquireTokenOptions.ExtraParameters = new Dictionary<string, object>
                    {
                        [seedParamKey] = "seed-param-value",
                    };
                    o.ExtraHeaderParameters = new Dictionary<string, string>
                    {
                        [seedHeaderKey] = "seed-header-value",
                    };
                    o.ExtraQueryParameters = new Dictionary<string, string>
                    {
                        [seedQueryKey] = "seed-query-value",
                    };
                });
            });
        });
        using var client = factory.CreateClient();

        var monitor = factory.Services.GetRequiredService<IOptionsMonitor<DownstreamApiOptions>>();
        var before = monitor.Get(ApiName);
        Assert.Single(before.AcquireTokenOptions.ExtraParameters!);
        Assert.Single(before.ExtraHeaderParameters!);
        Assert.Single(before.ExtraQueryParameters!);

        // Enters the merge path (HasAny via RelativePath) with header/query overrides,
        // and supplies an agent identity so SetOverrides also runs.
        const string appA = "app-a-44444444-4444-4444-4444-444444444444";
        var response = await client.GetAsync(
            $"/AuthorizationHeaderUnauthenticated/{ApiName}" +
            "?optionsOverride.RelativePath=/override" +
            "&optionsOverride.ExtraHeaderParameters.X-Override=override-header-value" +
            "&optionsOverride.ExtraQueryParameters.override-query=override-query-value" +
            $"&agentIdentity={appA}&agentUsername=usera@contoso.com");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var after = monitor.Get(ApiName);

        // AcquireTokenOptions.ExtraParameters, written by SetOverrides.
        var extraParameters = after.AcquireTokenOptions.ExtraParameters;
        Assert.NotNull(extraParameters);
        Assert.Single(extraParameters!);
        Assert.True(extraParameters!.ContainsKey(seedParamKey));
        Assert.False(extraParameters.ContainsKey(AgentIdentityKey));

        // ExtraHeaderParameters, written by MergeOptions.
        var headers = after.ExtraHeaderParameters;
        Assert.NotNull(headers);
        Assert.Single(headers!);
        Assert.True(headers!.ContainsKey(seedHeaderKey));
        Assert.False(headers.ContainsKey("X-Override"));

        // ExtraQueryParameters, written by MergeOptions.
        var queries = after.ExtraQueryParameters;
        Assert.NotNull(queries);
        Assert.Single(queries!);
        Assert.True(queries!.ContainsKey(seedQueryKey));
        Assert.False(queries.ContainsKey("override-query"));
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private readonly SidecarApiFactory _sharedFactory = new();

    public void Dispose() => _sharedFactory.Dispose();

    private WebApplicationFactory<Program> CreateHeaderFactory(
        RecordingAuthorizationHeaderProvider recorder,
        Dictionary<string, string?> sidecarConfig)
    {
        return _sharedFactory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(sidecarConfig);
            });
            builder.ConfigureServices(services =>
            {
                services.AddSingleton<IAuthorizationHeaderProvider>(recorder);
                services.Configure<DownstreamApiOptions>(ApiName, o =>
                {
                    o.BaseUrl = "https://api.example.com";
                    o.RelativePath = "/test";
                    o.Scopes = new[] { "User.Read" };
                });
            });
        });
    }

    private static string? GetAgentIdentity(AuthorizationHeaderProviderOptions? options)
    {
        var extras = options?.AcquireTokenOptions?.ExtraParameters;
        if (extras is null)
        {
            return null;
        }

        return extras.TryGetValue(AgentIdentityKey, out var value) ? value as string : null;
    }

    /// <summary>
    /// Records the agent identity from each call and echoes it back as the header,
    /// so a response can be matched to the request that produced it.
    /// </summary>
    private sealed class RecordingAuthorizationHeaderProvider : IAuthorizationHeaderProvider
    {
        private readonly int _delayMs;

        public RecordingAuthorizationHeaderProvider(int delayMs = 0)
        {
            _delayMs = delayMs;
        }

        public ConcurrentQueue<string?> RecordedQueue { get; } = new();

        public IReadOnlyList<string?> Recorded => RecordedQueue.ToArray();

        private async Task<string> RecordAsync(AuthorizationHeaderProviderOptions? options)
        {
            if (_delayMs > 0)
            {
                // Widen the window in which a concurrent request could overwrite shared state.
                await Task.Delay(_delayMs).ConfigureAwait(false);
            }

            var agent = GetAgentIdentity(options);
            RecordedQueue.Enqueue(agent);
            return agent ?? "no-agent";
        }

        public Task<string> CreateAuthorizationHeaderAsync(
            IEnumerable<string> scopes,
            AuthorizationHeaderProviderOptions? options = null,
            ClaimsPrincipal? claimsPrincipal = null,
            CancellationToken cancellationToken = default) => RecordAsync(options);

        public Task<string> CreateAuthorizationHeaderForAppAsync(
            string scopes,
            AuthorizationHeaderProviderOptions? downstreamApiOptions = null,
            CancellationToken cancellationToken = default) => RecordAsync(downstreamApiOptions);

        public Task<string> CreateAuthorizationHeaderForUserAsync(
            IEnumerable<string> scopes,
            AuthorizationHeaderProviderOptions? authorizationHeaderProviderOptions = null,
            ClaimsPrincipal? claimsPrincipal = null,
            CancellationToken cancellationToken = default) => RecordAsync(authorizationHeaderProviderOptions);
    }
}

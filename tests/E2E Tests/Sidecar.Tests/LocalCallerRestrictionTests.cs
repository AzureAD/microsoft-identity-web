// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Sidecar.Tests;

public class LocalCallerRestrictionTests(SidecarApiFactory factory) : IClassFixture<SidecarApiFactory>
{
    readonly SidecarApiFactory _factory = factory;

    // A routable, non-loopback address standing in for a remote caller.
    static readonly IPAddress s_remoteAddress = IPAddress.Parse("203.0.113.10");

    const string SensitiveEndpoint = "/AuthorizationHeaderUnauthenticated/MsGraph";

    HttpClient CreateClient(string? environment, IPAddress? remoteIpAddress)
    {
        var configured = _factory.WithWebHostBuilder(builder =>
        {
            if (environment is not null)
            {
                builder.UseSetting(WebHostDefaults.EnvironmentKey, environment);
            }

            if (remoteIpAddress is not null)
            {
                builder.ConfigureServices(services =>
                {
                    services.AddSingleton<IStartupFilter>(new RemoteIpStartupFilter(remoteIpAddress));
                });
            }
        });

        return configured.CreateClient();
    }

    [Fact]
    public async Task Production_NonLoopbackCaller_SensitiveEndpoint_IsForbidden()
    {
        var client = CreateClient(Environments.Production, s_remoteAddress);

        var response = await client.GetAsync(SensitiveEndpoint);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Production_NonLoopbackCaller_UnauthenticatedDownstreamApi_IsForbidden()
    {
        var client = CreateClient(Environments.Production, s_remoteAddress);

        using var content = new StringContent(string.Empty);
        var response = await client.PostAsync("/DownstreamApiUnauthenticated/MsGraph", content);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Production_NonLoopbackCaller_AuthenticatedEndpoint_IsForbidden()
    {
        var client = CreateClient(Environments.Production, s_remoteAddress);

        // A non-local caller is rejected even on an authenticated endpoint.
        var response = await client.GetAsync("/Validate");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Production_NonLoopbackCaller_HealthEndpoint_Succeeds()
    {
        var client = CreateClient(Environments.Production, s_remoteAddress);

        // The health endpoint must remain reachable from non-loopback callers
        // (for example, orchestrator probes that target the routable address).
        var response = await client.GetAsync(LocalCallerRestrictionPath());

        Assert.True(response.IsSuccessStatusCode, "Expected the health endpoint to be reachable from a non-loopback caller.");
        Assert.Contains("Healthy", await response.Content.ReadAsStringAsync(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Production_LoopbackCaller_SensitiveEndpoint_IsNotForbidden()
    {
        var client = CreateClient(Environments.Production, IPAddress.Loopback);

        var response = await client.GetAsync(SensitiveEndpoint);

        // The local caller is allowed through; the request may still fail later
        // for other reasons, but it must not be Forbidden.
        Assert.NotEqual(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Production_IPv4MappedLoopbackCaller_SensitiveEndpoint_IsNotForbidden()
    {
        var client = CreateClient(Environments.Production, IPAddress.Parse("::ffff:127.0.0.1"));

        var response = await client.GetAsync(SensitiveEndpoint);

        Assert.NotEqual(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Production_NoConnectionAddress_SensitiveEndpoint_IsNotForbidden()
    {
        // In-process hosting exposes no transport peer (null remote address),
        // which is treated as local.
        var client = CreateClient(Environments.Production, remoteIpAddress: null);

        var response = await client.GetAsync(SensitiveEndpoint);

        Assert.NotEqual(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Development_NonLoopbackCaller_SensitiveEndpoint_IsNotForbidden()
    {
        // The restriction is not applied in development, so the inner-loop
        // experience is unchanged.
        var client = CreateClient(Environments.Development, s_remoteAddress);

        var response = await client.GetAsync(SensitiveEndpoint);

        Assert.NotEqual(HttpStatusCode.Forbidden, response.StatusCode);
    }

    static string LocalCallerRestrictionPath() =>
        Microsoft.Identity.Web.Sidecar.LocalCallerRestriction.HealthEndpointPath;

    /// <summary>
    /// Prepends a middleware that sets the connection's remote address, so the
    /// in-memory test server can exercise the loopback boundary.
    /// </summary>
    sealed class RemoteIpStartupFilter(IPAddress remoteIpAddress) : IStartupFilter
    {
        readonly IPAddress _remoteIpAddress = remoteIpAddress;

        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) =>
            app =>
            {
                app.Use(async (context, requestDelegate) =>
                {
                    context.Connection.RemoteIpAddress = _remoteIpAddress;
                    await requestDelegate(context);
                });

                next(app);
            };
    }
}

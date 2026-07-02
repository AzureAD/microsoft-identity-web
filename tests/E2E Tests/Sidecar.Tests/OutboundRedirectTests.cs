// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Net;
using System.Net.Http.Json;
using System.Net.Sockets;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Web.Sidecar.Models;
using Moq;
using Xunit;

namespace Sidecar.Tests;

public class OutboundRedirectTests
{
    [Fact]
    public async Task OutboundRedirect_NotFollowedByDefault_ReturnsRedirectAndTargetNotContacted()
    {
        // Arrange
        const string apiName = "redirect-api";

        using var redirectTarget = new RawHttpServer(_ =>
            "HTTP/1.1 200 OK\r\nContent-Length: 2\r\nConnection: close\r\n\r\nOK");
        string redirectTargetUrl = $"http://127.0.0.1:{redirectTarget.Port}/next";

        using var configured = new RawHttpServer(_ =>
            $"HTTP/1.1 302 Found\r\nLocation: {redirectTargetUrl}\r\nContent-Length: 0\r\nConnection: close\r\n\r\n");

        var headerProvider = CreateHeaderProviderMock();

        using var factory = new SidecarApiFactory();
        var client = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.AddSingleton(headerProvider.Object);
                services.Configure<DownstreamApiOptions>(apiName, o =>
                {
                    o.BaseUrl = $"http://127.0.0.1:{configured.Port}";
                    o.RelativePath = "/start";
                    o.HttpMethod = HttpMethod.Get.Method;
                    o.RequestAppToken = true;
                    o.Scopes = ["api.default"];
                });
            });
        }).CreateClient();

        // Act
        var response = await client.PostAsync($"/DownstreamApiUnauthenticated/{apiName}", content: null);
        var body = await response.Content.ReadFromJsonAsync<DownstreamApiResult>();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(body);
        Assert.Equal(302, body!.StatusCode);
        Assert.True(configured.HitCount >= 1);
        Assert.Equal(0, redirectTarget.HitCount);
    }

    [Fact]
    public async Task OutboundRedirect_FollowedWhenOptedIn_TargetContacted()
    {
        // Arrange
        const string apiName = "redirect-api";

        using var redirectTarget = new RawHttpServer(_ =>
            "HTTP/1.1 200 OK\r\nContent-Length: 2\r\nConnection: close\r\n\r\nOK");
        string redirectTargetUrl = $"http://127.0.0.1:{redirectTarget.Port}/next";

        using var configured = new RawHttpServer(_ =>
            $"HTTP/1.1 302 Found\r\nLocation: {redirectTargetUrl}\r\nContent-Length: 0\r\nConnection: close\r\n\r\n");

        var headerProvider = CreateHeaderProviderMock();

        using var factory = new SidecarApiFactory();
        var client = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    { "Sidecar:AllowOutboundRedirects", "true" },
                });
            });

            builder.ConfigureServices(services =>
            {
                services.AddSingleton(headerProvider.Object);
                services.Configure<DownstreamApiOptions>(apiName, o =>
                {
                    o.BaseUrl = $"http://127.0.0.1:{configured.Port}";
                    o.RelativePath = "/start";
                    o.HttpMethod = HttpMethod.Get.Method;
                    o.RequestAppToken = true;
                    o.Scopes = ["api.default"];
                });
            });
        }).CreateClient();

        // Act
        var response = await client.PostAsync($"/DownstreamApiUnauthenticated/{apiName}", content: null);
        var body = await response.Content.ReadFromJsonAsync<DownstreamApiResult>();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(body);
        Assert.Equal(200, body!.StatusCode);
        Assert.True(configured.HitCount >= 1);
        Assert.True(redirectTarget.HitCount >= 1);
    }

    // Sidecar supports bearer tokens only. To add mTLS, also set AllowAutoRedirect = false
    // on the mTLS client.
    [Fact]
    public async Task Sidecar_DownstreamCalls_UseDefaultHttpClientFactoryPath()
    {
        // Arrange
        const string apiName = "guard-api";

        using var factory = new SidecarApiFactory();
        using var host = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.Configure<DownstreamApiOptions>(apiName, o =>
                {
                    o.BaseUrl = "https://downstream.example";
                    o.Scopes = ["api.default"];
                });
            });
        });

        using var scope = host.Services.CreateScope();
        var provider = scope.ServiceProvider;

        // Act
        var schemes = (await provider.GetRequiredService<IAuthenticationSchemeProvider>()
            .GetAllSchemesAsync()).ToList();
        var defaultScheme = await provider.GetRequiredService<IAuthenticationSchemeProvider>()
            .GetDefaultAuthenticateSchemeAsync();
        var options = provider.GetRequiredService<IOptionsMonitor<DownstreamApiOptions>>().Get(apiName);

        // Assert
        Assert.All(schemes, s => Assert.Equal(JwtBearerDefaults.AuthenticationScheme, s.Name));
        Assert.Equal(JwtBearerDefaults.AuthenticationScheme, defaultScheme?.Name);
        Assert.NotEqual("MTLS", options.ProtocolScheme, StringComparer.OrdinalIgnoreCase);
        Assert.NotEqual("MTLS_POP", options.ProtocolScheme, StringComparer.OrdinalIgnoreCase);
    }

    private static Mock<IAuthorizationHeaderProvider> CreateHeaderProviderMock()
    {
        var mock = new Mock<IAuthorizationHeaderProvider>();
        mock.Setup(p => p.CreateAuthorizationHeaderAsync(
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<AuthorizationHeaderProviderOptions?>(),
                It.IsAny<ClaimsPrincipal?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("Bearer test-token");
        return mock;
    }

    private sealed class RawHttpServer : IDisposable
    {
        private readonly TcpListener _listener;
        private readonly Func<string, string> _responder;
        private readonly CancellationTokenSource _cts = new();
        private int _hitCount;

        public RawHttpServer(Func<string, string> responder)
        {
            _responder = responder;
            _listener = new TcpListener(IPAddress.Loopback, 0);
            _listener.Start();
            Port = ((IPEndPoint)_listener.LocalEndpoint).Port;
            _ = Task.Run(AcceptLoopAsync);
        }

        public int Port { get; }

        public int HitCount => Volatile.Read(ref _hitCount);

        private async Task AcceptLoopAsync()
        {
            while (!_cts.IsCancellationRequested)
            {
                TcpClient tcp;
                try
                {
                    tcp = await _listener.AcceptTcpClientAsync(_cts.Token);
                }
                catch
                {
                    break;
                }

                _ = Task.Run(() => HandleAsync(tcp));
            }
        }

        private async Task HandleAsync(TcpClient tcp)
        {
            try
            {
                using (tcp)
                await using (var stream = tcp.GetStream())
                {
                    var buffer = new byte[8192];
                    int read = await stream.ReadAsync(buffer);
                    string requestText = Encoding.ASCII.GetString(buffer, 0, read);
                    Interlocked.Increment(ref _hitCount);

                    byte[] responseBytes = Encoding.ASCII.GetBytes(_responder(requestText));
                    await stream.WriteAsync(responseBytes);
                    await stream.FlushAsync();
                }
            }
            catch
            {
                // Best-effort test listener.
            }
        }

        public void Dispose()
        {
            _cts.Cancel();
            _listener.Stop();
            _cts.Dispose();
        }
    }
}

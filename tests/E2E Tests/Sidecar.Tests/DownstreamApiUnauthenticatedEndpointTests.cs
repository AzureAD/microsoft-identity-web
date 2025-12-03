// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Client;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.Sidecar.Models;
using Moq;
using Xunit;

namespace Sidecar.Tests;

public class DownstreamApiUnauthenticatedEndpointTests(SidecarApiFactory factory) : IClassFixture<SidecarApiFactory>
{
    private readonly SidecarApiFactory _factory = factory;

    [Fact]
    public async Task DownstreamApiUnauthenticated_WithValidApiAndScopes_NoAuthHeader_ReturnsOkAsync()
    {
        // Arrange
        var mockResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"success\":true}", Encoding.UTF8, "application/json")
        };

        ClaimsPrincipal? capturedPrincipal = null;

        var mockDownstream = new Mock<IDownstreamApi>();
        mockDownstream
            .Setup(d => d.CallApiAsync(
                It.IsAny<DownstreamApiOptions>(),
                It.IsAny<ClaimsPrincipal>(),
                It.IsAny<HttpContent>(),
                It.IsAny<CancellationToken>()))
            .Callback<DownstreamApiOptions, ClaimsPrincipal, HttpContent?, CancellationToken>((_, principal, _, _) =>
            {
                capturedPrincipal = principal;
            })
            .ReturnsAsync(mockResponse);

        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.Configure<DownstreamApiOptions>("test-api", o =>
                {
                    o.BaseUrl = "https://api.example.com";
                    o.Scopes = ["api.read"];
                });
                services.AddSingleton(mockDownstream.Object);
            });
        }).CreateClient();

        // Act (no Authorization header)
        var response = await client.PostAsync("/DownstreamApiUnauthenticated/test-api", new StringContent("", Encoding.UTF8, "application/json"));

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<DownstreamApiResult>();
        Assert.NotNull(result);
        Assert.Equal(200, result!.StatusCode);
        Assert.False(capturedPrincipal?.Identity?.IsAuthenticated);
    }

    [Fact]
    public async Task DownstreamApiUnauthenticated_WithNonExistentApi_ReturnsBadRequestAsync()
    {
        // Arrange
        var mockDownstream = new Mock<IDownstreamApi>();
        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.AddSingleton(mockDownstream.Object);
                // No configuration for api
            });
        }).CreateClient();

        // Act
        var response = await client.PostAsync("/DownstreamApiUnauthenticated/unknown-api", null);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        // For unknown we expect missing scopes message (consistent with authenticated tests)
        Assert.Contains("No scopes found for the API 'unknown-api'", content, StringComparison.OrdinalIgnoreCase);
        mockDownstream.Verify(d => d.CallApiAsync(
            It.IsAny<DownstreamApiOptions>(),
            It.IsAny<ClaimsPrincipal>(),
            It.IsAny<HttpContent>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DownstreamApiUnauthenticated_WithConfiguredApiButNoScopes_ReturnsBadRequestAsync()
    {
        // Arrange
        var mockDownstream = new Mock<IDownstreamApi>();

        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.Configure<DownstreamApiOptions>("test-api", o =>
                {
                    o.BaseUrl = "https://api.example.com";
                    // No scopes
                });
                services.AddSingleton(mockDownstream.Object);
            });
        }).CreateClient();

        // Act
        var response = await client.PostAsync("/DownstreamApiUnauthenticated/test-api", null);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("No scopes found for the API 'test-api'", content, StringComparison.OrdinalIgnoreCase);
        mockDownstream.Verify(d => d.CallApiAsync(
            It.IsAny<DownstreamApiOptions>(),
            It.IsAny<ClaimsPrincipal>(),
            It.IsAny<HttpContent>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DownstreamApiUnauthenticated_WithChallengeUserException_ReturnsUnauthorizedAsync()
    {
        // Arrange
        var msalException = new MsalUiRequiredException("AADSTS50076", "MFA required.");
        var mockDownstream = new Mock<IDownstreamApi>();
        mockDownstream
            .Setup(d => d.CallApiAsync(
                It.IsAny<DownstreamApiOptions>(),
                It.IsAny<ClaimsPrincipal>(),
                It.IsAny<HttpContent>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new MicrosoftIdentityWebChallengeUserException(msalException, ["api.read"]));

        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.Configure<DownstreamApiOptions>("test-api", o =>
                {
                    o.BaseUrl = "https://api.example.com";
                    o.Scopes = ["api.read"];
                });
                services.AddSingleton(mockDownstream.Object);
            });
        }).CreateClient();

        // Act
        var response = await client.PostAsync("/DownstreamApiUnauthenticated/test-api", null);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("MFA required", content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DownstreamApiUnauthenticated_GenericException_ReturnsInternalServerErrorAsync()
    {
        // Arrange
        var mockDownstream = new Mock<IDownstreamApi>();
        mockDownstream
            .Setup(d => d.CallApiAsync(
                It.IsAny<DownstreamApiOptions>(),
                It.IsAny<ClaimsPrincipal>(),
                It.IsAny<HttpContent>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Failure"));

        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.Configure<DownstreamApiOptions>("test-api", o =>
                {
                    o.BaseUrl = "https://api.example.com";
                    o.Scopes = ["api.read"];
                });
                services.AddSingleton(mockDownstream.Object);
            });
        }).CreateClient();

        // Act
        var response = await client.PostAsync("/DownstreamApiUnauthenticated/test-api", null);

        // Assert
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("An unexpected error occurred", content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DownstreamApiUnauthenticated_WithSuccessfulCall_ReturnsResultAsync()
    {
        // Arrange
        var payload = "{\"ok\":true}";
        var mockResponse = new HttpResponseMessage(HttpStatusCode.Accepted)
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json")
        };

        var mockDownstream = new Mock<IDownstreamApi>();
        mockDownstream
            .Setup(d => d.CallApiAsync(
                It.IsAny<DownstreamApiOptions>(),
                It.IsAny<ClaimsPrincipal>(),
                It.IsAny<HttpContent>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockResponse);

        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.Configure<DownstreamApiOptions>("test-api", o =>
                {
                    o.BaseUrl = "https://api.example.com";
                    o.Scopes = ["api.read"];
                });
                services.AddSingleton(mockDownstream.Object);
            });
        }).CreateClient();

        // Act
        var response = await client.PostAsync("/DownstreamApiUnauthenticated/test-api", null);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<DownstreamApiResult>();
        Assert.NotNull(result);
        Assert.Equal((int)HttpStatusCode.Accepted, result!.StatusCode);
        Assert.Equal(payload, result.Content);
    }
}

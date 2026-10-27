// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Web;
using Moq;
using Xunit;

namespace Sidecar.Tests;

public class AuthorizationHeaderEndpointTests(SidecarApiFactory factory) : IClassFixture<SidecarApiFactory>
{
    private readonly SidecarApiFactory _factory = factory;

    [Fact]
    public async Task AuthorizationHeader_WithInvalidToken_ReturnsUnauthorizedAsync()
    {
        // Arrange
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "invalid-token");

        // Act
        var response = await client.GetAsync("/AuthorizationHeader/test-api?OptionsOverride.Scopes=scopes");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task AuthorizationHeader_WithNonExistentApi_AndNoScope_OverrideReturnsBadRequestAsync()
    {
        // Arrange
        var mockHeaderProvider = new TestAuthorizationHeaderProvider();

        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.AddSingleton(mockHeaderProvider);
                TestAuthenticationHandler.AddAlwaysSucceedTestAuthentication(services);
            });
        }).CreateClient();

        // Add a valid token (mocked)
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "valid-token");

        // Act
        var response = await client.GetAsync("/AuthorizationHeader/non-existent-api");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("No scopes found", content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task AuthorizationHeader_UnknownService_ReturnsBadRequestAsync()
    {
        // Arrange
        var mockHeaderProvider = new TestAuthorizationHeaderProvider();
        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.AddSingleton(mockHeaderProvider);
                TestAuthenticationHandler.AddAlwaysSucceedTestAuthentication(services);
            });
            builder.UseSetting("DownstreamApi:test-api:BaseUrl", "https://api.example.com");
            // Don't set scopes to trigger the error
        }).CreateClient();

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "valid-token");

        // Act
        var response = await client.GetAsync("/AuthorizationHeader/unknown");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task AuthorizationHeader_WithValidApiButNoScopes_ReturnsBadRequestAsync()
    {
        // Arrange
        var mockHeaderProvider = new TestAuthorizationHeaderProvider();
        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.AddSingleton(mockHeaderProvider);
                TestAuthenticationHandler.AddAlwaysSucceedTestAuthentication(services);
            });
            builder.UseSetting("DownstreamApi:test-api:BaseUrl", "https://api.example.com");
            // Don't set scopes to trigger the error
        }).CreateClient();

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "valid-token");

        // Act
        var response = await client.GetAsync("/AuthorizationHeader/test-api");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("No scopes found for the API 'test-api'", content, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-a-guid")]
    [InlineData("00000000-0000-0000-0000-000000000000")]
    public async Task AuthorizationHeader_WithInvalidSelectedAgentUserId_ReturnsBadRequestWithoutAcquiringTokenAsync(
        string agentUserId)
    {
        // Arrange
        var mockHeaderProvider = new Mock<IAuthorizationHeaderProvider>(MockBehavior.Strict);
        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                TestAuthenticationHandler.AddAlwaysSucceedTestAuthentication(services);
                services.Configure<DownstreamApiOptions>("test-api", options =>
                {
                    options.BaseUrl = "https://api.example.com";
                    options.Scopes = ["user.read"];
                });
                services.AddSingleton(mockHeaderProvider.Object);
            });
        }).CreateClient();

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "valid-token");

        // Act
        var response = await client.GetAsync(
            $"/AuthorizationHeader/test-api?AgentIdentity=agent-app-id&AgentUserId={agentUserId}");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        string content = await response.Content.ReadAsStringAsync();
        Assert.Contains("AgentUserId must be a non-empty GUID.", content, StringComparison.Ordinal);
        if (agentUserId.Length > 0)
        {
            Assert.DoesNotContain(agentUserId, content, StringComparison.Ordinal);
        }
        mockHeaderProvider.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task AuthorizationHeader_WithAgentUserIdButNoAgentIdentity_IgnoresAgentUserIdAsync()
    {
        // Arrange
        var mockHeaderProvider = new Mock<IAuthorizationHeaderProvider>();
        mockHeaderProvider
            .Setup(provider => provider.CreateAuthorizationHeaderAsync(
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<AuthorizationHeaderProviderOptions>(),
                It.IsAny<ClaimsPrincipal>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("******");

        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                TestAuthenticationHandler.AddAlwaysSucceedTestAuthentication(services);
                services.Configure<DownstreamApiOptions>("test-api", options =>
                {
                    options.BaseUrl = "https://api.example.com";
                    options.Scopes = ["user.read"];
                });
                services.AddSingleton(mockHeaderProvider.Object);
            });
        }).CreateClient();

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "valid-token");

        // Act
        var response = await client.GetAsync(
            "/AuthorizationHeader/test-api?AgentUserId=not-a-guid");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        mockHeaderProvider.Verify(
            provider => provider.CreateAuthorizationHeaderAsync(
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<AuthorizationHeaderProviderOptions>(),
                It.IsAny<ClaimsPrincipal>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task AuthorizationHeader_ThrowsException_Returns500Async()
    {
        // Arrange
        var mockDownstreamApi = new Mock<IDownstreamApi>();
        var exception = new InvalidOperationException();
        mockDownstreamApi
            .Setup(x => x.CallApiAsync(It.IsAny<DownstreamApiOptions>(), It.IsAny<System.Security.Claims.ClaimsPrincipal>(), It.IsAny<HttpContent>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(exception);

        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                TestAuthenticationHandler.AddAlwaysSucceedTestAuthentication(services);

                services.Configure<DownstreamApiOptions>("test-api", options =>
                {
                    options.BaseUrl = "https://api.example.com";
                    options.Scopes = ["user.read"];
                });

                services.AddSingleton(mockDownstreamApi.Object);
            });
        }).CreateClient();

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "valid-token");

        // Act
        var response = await client.GetAsync("/AuthorizationHeader/test-api");

        // Assert
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
    }

}

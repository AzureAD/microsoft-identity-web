// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Abstractions;
using Xunit;

namespace Sidecar.Tests;

public class AuthorizationHeaderEndpointTests(SidecarApiFactory factory) : IClassFixture<SidecarApiFactory>
{
    private readonly SidecarApiFactory _factory = factory;

    [Fact]
    public async Task AuthorizationHeader_WithInvalidToken_ReturnsUnauthorized()
    {
        // Arrange
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "invalid-token");

        // Act
        var response = await client.PostAsync("/AuthorizationHeader/test-api", JsonContent.Create(new { Scopes = new string[] { "scopes" }}));

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task AuthorizationHeader_WithNonExistentApi_AndNoScope_OverrideReturnsBadRequest()
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
        var response = await client.PostAsync("/AuthorizationHeader/non-existent-api", null);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("No scopes found", content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task AuthorizationHeader_WithValidApiButNoScopes_ReturnsBadRequest()
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
        var response = await client.PostAsync("/AuthorizationHeader/test-api", null);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("No scopes found for the API 'test-api'", content, StringComparison.OrdinalIgnoreCase);
    }
}

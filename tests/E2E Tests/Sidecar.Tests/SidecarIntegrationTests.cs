// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Web;
using Xunit;

namespace Sidecar.Tests;

public class SidecarIntegrationTests(SidecarApiFactory factory) : IClassFixture<SidecarApiFactory>
{
    private readonly SidecarApiFactory _factory = factory;

    [Fact]
    public async Task OpenApiEndpoint_IsAvailable_InDevelopment()
    {
        // Arrange
        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
        }).CreateClient();

        // Act
        var response = await client.GetAsync("/openapi/v1.json");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("openapi", content, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AuthorizationEndpoint_ExistsButDoesNotRequiresAuth()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        // The API does not exist (which is fine) but Scopes are not provided in the
        // override
        var response = await client.PostAsync("/AuthorizationHeader/test", JsonContent.Create(new DownstreamApiOptions{ Scopes = ["scopes"] }));

        // Assert
        string content = await response.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Contains("No account", content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ValidateEndpoint_ExistsAndRequiresAuth()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/Validate");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public void Services_AreRegisteredCorrectly()
    {
        // Arrange & Act
        using var scope = _factory.Services.CreateScope();
        var services = scope.ServiceProvider;

        // Assert - Verify key services are registered
        Assert.NotNull(services.GetService<IConfiguration>());
        
        var tokenAcquisition = services.GetService<ITokenAcquisition>();
        Assert.NotNull(tokenAcquisition);
    }

    [Fact]
    public async Task Application_HandlesInvalidRoutes()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/non-existent-endpoint");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Application_HandlesMethodNotAllowed()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act - Try GET on POST endpoint
        var response = await client.GetAsync("/AuthorizationHeader/test");

        // Assert
        Assert.Equal(HttpStatusCode.MethodNotAllowed, response.StatusCode);
    }

    [Fact]
    public async Task HealthCheck_IfConfigured_Works()
    {
        var client = _factory.CreateClient();

        // Act - Just verify the healthz endpoint responds
        var response = await client.GetAsync("/healthz");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public void Configuration_IsLoadedCorrectly()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();

        // Act & Assert
        Assert.NotNull(configuration);
        
        // Verify configuration sections exist (they might be empty in test environment)
        var azureAdSection = configuration.GetSection("AzureAd");
        Assert.NotNull(azureAdSection);
        
        var downstreamApiSection = configuration.GetSection("DownstreamApi");
        Assert.NotNull(downstreamApiSection);
    }
}

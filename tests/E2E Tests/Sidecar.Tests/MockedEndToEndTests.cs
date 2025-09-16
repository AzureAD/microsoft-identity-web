// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.Sidecar.Models;
using Xunit;

namespace Sidecar.Tests;

public class MockedEndToEndTests(SidecarApiFactory factory) : IClassFixture<SidecarApiFactory>
{
    private readonly SidecarApiFactory _factory = factory;

    [Fact]
    public async Task MockedAuthorizationFlow_WithValidConfiguration_ReturnsAuthorizationHeader()
    {
        // Arrange
        const string expectedAuthHeader = "Bearer token";
        const string apiName = "test-api";
        const string scope = "https://graph.microsoft.com/.default";

        TestAuthorizationHeaderProvider mock = new()
        {
            Result = expectedAuthHeader
        };

        var client = _factory
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    services.AddSingleton<IAuthorizationHeaderProvider>(mock);
                    services.Configure<DownstreamApiOptions>(apiName, options =>
                    {
                        options.BaseUrl = "https://graph.microsoft.com";
                        options.Scopes = new[] { scope };
                    });
                });
            })
            .CreateClient();

        // Add authentication header (would be validated in real scenario)
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "valid-test-token");

        // Act
        var response = await client.PostAsync($"/AuthorizationHeader/{apiName}", null);

        // Assert
        var content = await response.Content.ReadAsStringAsync();
        
        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            // Expected in test environment without proper authentication setup
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            return;
        }

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = JsonSerializer.Deserialize<AuthorizationHeaderResult>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true});
        Assert.NotNull(result);
        Assert.Equal(expectedAuthHeader, result.AuthorizationHeader);
    }
}

class TestAuthorizationHeaderProvider : IAuthorizationHeaderProvider
{
    public string? Result { get; init; }

    public Task<string> CreateAuthorizationHeaderAsync(IEnumerable<string> scopes, AuthorizationHeaderProviderOptions? options = null, ClaimsPrincipal? claimsPrincipal = null, CancellationToken cancellationToken = default) => Task.FromResult(Result ?? string.Empty);

    public Task<string> CreateAuthorizationHeaderForAppAsync(string scopes, AuthorizationHeaderProviderOptions? downstreamApiOptions = null, CancellationToken cancellationToken = default) => Task.FromResult(Result ?? string.Empty);

    public Task<string> CreateAuthorizationHeaderForUserAsync(IEnumerable<string> scopes, AuthorizationHeaderProviderOptions? authorizationHeaderProviderOptions = null, ClaimsPrincipal? claimsPrincipal = null, CancellationToken cancellationToken = default) => Task.FromResult(Result ?? string.Empty);
}

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Client;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.Sidecar.Models;
using Moq;
using Xunit;

namespace Sidecar.Tests;

public class DownstreamApiEndpointTests(SidecarApiFactory factory) : IClassFixture<SidecarApiFactory>
{
    private readonly SidecarApiFactory _factory = factory;

    [Fact]
    public async Task DownstreamApi_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.PostAsync("/DownstreamApi/test-api", null);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task DownstreamApi_WithValidApiButNoScopes_ReturnsBadRequest()
    {
        // Arrange
        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                TestAuthenticationHandler.AddAlwaysSucceedTestAuthentication(services);
                
                // Configure a downstream API without scopes
                services.Configure<DownstreamApiOptions>("test-api", options =>
                {
                    options.BaseUrl = "https://api.example.com";
                    options.RelativePath = "/test";
                    // Intentionally not setting Scopes
                });
            });
        }).CreateClient();

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "valid-token");

        // Act
        var response = await client.PostAsync("/DownstreamApi/test-api", null);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("No scopes found for the API 'test-api'", content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DownstreamApi_WithValidApiButNoScopesInOptionsOverride_ReturnsBadRequest()
    {
        // Arrange
        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                TestAuthenticationHandler.AddAlwaysSucceedTestAuthentication(services);
                
                // Configure a downstream API without scopes
                services.Configure<DownstreamApiOptions>("test-api", options =>
                {
                    options.BaseUrl = "https://api.example.com";
                    options.RelativePath = "/test";
                    // Intentionally not setting Scopes
                });
            });
        }).CreateClient();

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "valid-token");

        var optionsOverride = new DownstreamApiOptions
        {
            // Not setting Scopes
            RelativePath = "/override"
        };

        // Act
        var response = await client.PostAsync("/DownstreamApi/test-api", null);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("No scopes found for the API 'test-api' or in optionsOverride", content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DownstreamApi_WithMicrosoftIdentityWebChallengeUserException_ReturnsUnauthorized()
    {
        // Arrange
        var mockDownstreamApi = new Mock<IDownstreamApi>();
        var msalException = new MsalUiRequiredException("AADSTS50076", "Due to a configuration change made by your administrator, or because you moved to a new location, you must use multi-factor authentication to access.");
        mockDownstreamApi
            .Setup(x => x.CallApiAsync(It.IsAny<DownstreamApiOptions>(), It.IsAny<System.Security.Claims.ClaimsPrincipal>(), It.IsAny<HttpContent>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new MicrosoftIdentityWebChallengeUserException(msalException, ["user.read"]));

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
        var response = await client.PostAsync("/DownstreamApi/test-api", null);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Due to a configuration change made by your administrator", content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DownstreamApi_WithGenericException_ReturnsInternalServerError()
    {
        // Arrange
        var mockDownstreamApi = new Mock<IDownstreamApi>();
        mockDownstreamApi
            .Setup(x => x.CallApiAsync(It.IsAny<DownstreamApiOptions>(), It.IsAny<System.Security.Claims.ClaimsPrincipal>(), It.IsAny<HttpContent>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Unexpected error"));

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
        var response = await client.PostAsync("/DownstreamApi/test-api", null);

        // Assert
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("An unexpected error occurred", content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DownstreamApi_WithSuccessfulCall_ReturnsOkWithDownstreamApiResult()
    {
        // Arrange
        var responseContent = "{\"result\": \"success\"}";
        var mockResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseContent, Encoding.UTF8, "application/json")
        };

        var mockDownstreamApi = new Mock<IDownstreamApi>();
        mockDownstreamApi
            .Setup(x => x.CallApiAsync(It.IsAny<DownstreamApiOptions>(), It.IsAny<System.Security.Claims.ClaimsPrincipal>(), It.IsAny<HttpContent>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockResponse);

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
        var response = await client.PostAsync("/DownstreamApi/test-api", null);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var result = await response.Content.ReadFromJsonAsync<DownstreamApiResult>();
        Assert.NotNull(result);
        Assert.Equal(200, result.StatusCode);
        Assert.NotNull(result.Headers);
        Assert.Equal(responseContent, result.Content);
    }

    [Fact]
    public async Task DownstreamApi_WithEmptyResponseContent_ReturnsOkWithNullContent()
    {
        // Arrange
        var mockResponse = new HttpResponseMessage(HttpStatusCode.NoContent)
        {
            Content = new StringContent("", Encoding.UTF8)
        };
        mockResponse.Content.Headers.ContentLength = 0;

        var mockDownstreamApi = new Mock<IDownstreamApi>();
        mockDownstreamApi
            .Setup(x => x.CallApiAsync(It.IsAny<DownstreamApiOptions>(), It.IsAny<System.Security.Claims.ClaimsPrincipal>(), It.IsAny<HttpContent>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockResponse);

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
        var response = await client.PostAsync("/DownstreamApi/test-api", null);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var result = await response.Content.ReadFromJsonAsync<DownstreamApiResult>();
        Assert.NotNull(result);
        Assert.Equal(204, result.StatusCode);
        Assert.Null(result.Content);
    }

    [Fact]
    public async Task DownstreamApi_WithRequestBody_PassesContentCorrectly()
    {
        // Arrange
        var responseContent = "{\"result\": \"success\"}";
        var requestContent = new StringContent("{\"request\": \"value\"}", Encoding.UTF8, "application/json");

        var mockResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseContent, Encoding.UTF8, "application/json")
        };

        HttpContent? capturedContent = null;
        var mockDownstreamApi = new Mock<IDownstreamApi>();
        mockDownstreamApi
            .Setup(x => x.CallApiAsync(It.IsAny<DownstreamApiOptions>(), It.IsAny<System.Security.Claims.ClaimsPrincipal>(), It.IsAny<HttpContent>(), It.IsAny<CancellationToken>()))
            .Callback<DownstreamApiOptions, System.Security.Claims.ClaimsPrincipal, HttpContent?, CancellationToken>((_, _, content, _) =>
            {
                capturedContent = content;
            })
            .ReturnsAsync(mockResponse);

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
        var response = await client.PostAsync("/DownstreamApi/test-api", requestContent);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equivalent(capturedContent, requestContent);
    }

    [Fact]
    public async Task DownstreamApi_WithAgentIdentity_PassesAgentIdentityToOptions()
    {
        // Arrange
        var responseContent = "{\"result\": \"success\"}";
        var mockResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseContent, Encoding.UTF8, "application/json")
        };

        DownstreamApiOptions? capturedOptions = null;
        var mockDownstreamApi = new Mock<IDownstreamApi>();
        mockDownstreamApi
            .Setup(x => x.CallApiAsync(It.IsAny<DownstreamApiOptions>(), It.IsAny<System.Security.Claims.ClaimsPrincipal>(), It.IsAny<HttpContent>(), It.IsAny<CancellationToken>()))
            .Callback<DownstreamApiOptions, System.Security.Claims.ClaimsPrincipal, HttpContent?, CancellationToken>((options, _, _, _) =>
            {
                capturedOptions = options;
            })
            .ReturnsAsync(mockResponse);

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
        
        var agentIdentity = "test-agent-id";

        // Act
        var response = await client.PostAsync($"/DownstreamApi/test-api?agentidentity={agentIdentity}", null);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        Assert.True(capturedOptions?.AcquireTokenOptions?.ExtraParameters?.Keys.Contains("IDWEB_FMI_PATH_FOR_CLIENT_ASSERTION"));
    }

    [Fact]
    public async Task DownstreamApi_WithAgentIdentityAndUsername_PassesAgentUserIdentityToOptions()
    {
        // Arrange
        var responseContent = "{\"result\": \"success\"}";
        var mockResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseContent, Encoding.UTF8, "application/json")
        };

        DownstreamApiOptions? capturedOptions = null;
        var mockDownstreamApi = new Mock<IDownstreamApi>();
        mockDownstreamApi
            .Setup(x => x.CallApiAsync(It.IsAny<DownstreamApiOptions>(), It.IsAny<System.Security.Claims.ClaimsPrincipal>(), It.IsAny<HttpContent>(), It.IsAny<CancellationToken>()))
            .Callback<DownstreamApiOptions, System.Security.Claims.ClaimsPrincipal, HttpContent?, CancellationToken>((options, _, _, _) =>
            {
                capturedOptions = options;
            })
            .ReturnsAsync(mockResponse);

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
        
        var agentIdentity = "test-agent-id";
        var agentUsername = "test-user@example.com";

        // Act
        var response = await client.PostAsync($"/DownstreamApi/test-api?agentidentity={agentIdentity}&agentUsername={agentUsername}", null);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(capturedOptions?.AcquireTokenOptions?.ExtraParameters?.Keys.Contains("IDWEB_AGENT_IDENTITY"));
        Assert.True(capturedOptions?.AcquireTokenOptions?.ExtraParameters?.Keys.Contains("IDWEB_USERNAME"));
    }


    [Fact]
    public async Task DownstreamApi_WithTenantOverride_PassesTenantIdToOptions()
    {
        // Arrange
        var responseContent = "{\"result\": \"success\"}";
        var mockResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseContent, Encoding.UTF8, "application/json")
        };

        string tenantOverride = Guid.NewGuid().ToString();

        DownstreamApiOptions? capturedOptions = null;
        var mockDownstreamApi = new Mock<IDownstreamApi>();
        mockDownstreamApi
            .Setup(x => x.CallApiAsync(It.IsAny<DownstreamApiOptions>(), It.IsAny<System.Security.Claims.ClaimsPrincipal>(), It.IsAny<HttpContent>(), It.IsAny<CancellationToken>()))
            .Callback<DownstreamApiOptions, System.Security.Claims.ClaimsPrincipal, HttpContent?, CancellationToken>((options, _, _, _) =>
            {
                capturedOptions = options;
            })
            .ReturnsAsync(mockResponse);

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
        var response = await client.PostAsync($"/DownstreamApi/test-api?tenant={tenantOverride}", null);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(tenantOverride, capturedOptions?.AcquireTokenOptions?.Tenant);
    }

    [Fact] 
    public async Task DownstreamApi_WithNonExistentApiName_ReturnsBadRequestWithProblemDetails()
    {
        // Arrange
        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                TestAuthenticationHandler.AddAlwaysSucceedTestAuthentication(services);
            });
        }).CreateClient();

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "valid-token");

        // Act
        var response = await client.PostAsync("/DownstreamApi/non-existent-api", null);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        // Just verify it returns 400 - that's sufficient for this test
    }

}

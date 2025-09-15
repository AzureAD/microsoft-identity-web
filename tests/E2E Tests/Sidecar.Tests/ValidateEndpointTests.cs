// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Net.Http.Headers;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Identity.Web.Sidecar;
using Xunit;

namespace Sidecar.Tests;

public class SidecarApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
        });
    }
}

public class ValidateEndpointTests : IClassFixture<SidecarApiFactory>
{
    private readonly SidecarApiFactory _factory;

    public ValidateEndpointTests(SidecarApiFactory factory) => _factory = factory;

    [Fact]
    public async Task Validate_WhenBadTokenAsync()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "dummy-token");
        var response = await client.GetAsync("/Validate");
        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("invalid_token", response.Headers.WwwAuthenticate.ToString(), StringComparison.CurrentCultureIgnoreCase);
    }
}

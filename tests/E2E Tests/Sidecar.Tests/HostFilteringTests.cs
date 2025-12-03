// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Net;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Sidecar.Tests;

public class HostFilteringTests(SidecarApiFactory factory) : IClassFixture<SidecarApiFactory>
{
    readonly SidecarApiFactory _factory = factory;
    const string EnvironmentKey = "environment"; // WebHostDefaults.EnvironmentKey constant value

    [Fact]
    public async Task HostFiltering_NotApplied_In_Development()
    {
        // Development environment (default for factory) -> no host filtering.
        var devFactory = _factory.WithWebHostBuilder(b => { /* Development by default */ });
        var client = devFactory.CreateClient();

        var request = new HttpRequestMessage(HttpMethod.Get, "/healthz")
        {
            Headers = { Host = "notlocalhost" }
        };
        var response = await client.SendAsync(request);

        Assert.True(response.IsSuccessStatusCode, "Expected success without host filtering in Development environment.");
    }

    [Fact]
    public async Task HostFiltering_Blocks_Disallowed_Host_In_Production()
    {
        // Force Production by setting environment variable before building client.
        var prodFactory = _factory.WithWebHostBuilder(b =>
        {
            b.UseSetting(EnvironmentKey, Environments.Production);
        });
        var client = prodFactory.CreateClient(); 

        // Allowed host (localhost) should succeed.
        var okResponse = await client.GetAsync("/healthz");
        Assert.True(okResponse.IsSuccessStatusCode, "Expected success for localhost host.");

        // Disallowed host should be rejected.
        var badRequest = new HttpRequestMessage(HttpMethod.Get, "/healthz")
        {
            Headers = { Host = "notlocalhost" }
        };
        var badResponse = await client.SendAsync(badRequest);

        var content = await badResponse.Content.ReadAsStringAsync();

        Assert.Contains("Invalid Hostname", content, StringComparison.OrdinalIgnoreCase);

        Assert.Equal(HttpStatusCode.BadRequest, badResponse.StatusCode);
    }
}

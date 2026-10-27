// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Identity.Web.Sidecar;
using Xunit;

namespace Sidecar.Tests;

public class HostFilteringTests(SidecarApiFactory factory) : IClassFixture<SidecarApiFactory>
{
    readonly SidecarApiFactory _factory = factory;

    // A Host header value that is not one of the sidecar's local host names.
    const string UnexpectedHost = "unexpected.example.com";

    // A path with no matching endpoint. A request that clears Host validation
    // falls through to a 404, which distinguishes it from a 400 rejection.
    const string UnmappedPath = "/host-validation-probe";

    HttpClient CreateClient(string environment)
    {
        var configured = _factory.WithWebHostBuilder(builder =>
            builder.UseSetting(WebHostDefaults.EnvironmentKey, environment));

        return configured.CreateClient();
    }

    static HttpRequestMessage Get(string path, string host)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.TryAddWithoutValidation("Host", host);
        return request;
    }

    [Fact]
    public async Task Production_UnexpectedHost_IsRejected()
    {
        var client = CreateClient(Environments.Production);

        var response = await client.SendAsync(Get(UnmappedPath, UnexpectedHost));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData("localhost")]
    [InlineData("127.0.0.1")]
    [InlineData("[::1]")]
    public async Task Production_LocalHost_ClearsHostValidation(string host)
    {
        var client = CreateClient(Environments.Production);

        var response = await client.SendAsync(Get(UnmappedPath, host));

        // A local host name clears Host validation and falls through to routing,
        // which has no endpoint for this path.
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Theory]
    [InlineData(LocalCallerRestriction.HealthEndpointPath)]
    [InlineData(LocalCallerRestriction.HealthEndpointPath + "/")]
    public async Task Production_UnexpectedHost_HealthEndpoint_Succeeds(string path)
    {
        var client = CreateClient(Environments.Production);

        // The health endpoint, including the trailing-slash form routing accepts,
        // is exempt so orchestrator probes, which may target the routable address,
        // continue to work.
        var response = await client.SendAsync(Get(path, UnexpectedHost));

        var body = await response.Content.ReadAsStringAsync();
        Assert.True(
            response.IsSuccessStatusCode,
            $"Expected the health endpoint to be exempt from Host validation; got {(int)response.StatusCode}.");
        Assert.Contains("Healthy", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Development_UnexpectedHost_IsNotRejected()
    {
        // Host validation is not applied in development, so the inner-loop
        // experience is unchanged.
        var client = CreateClient(Environments.Development);

        var response = await client.SendAsync(Get(UnmappedPath, UnexpectedHost));

        Assert.NotEqual(HttpStatusCode.BadRequest, response.StatusCode);
    }
}

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using WebApp;
using Xunit;

namespace IntegrationTests;

// provided by https://github.com/f1nzer/ms-identity-web-1957-bug-repro
public class Net7ThreadingTest : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public Net7ThreadingTest(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Get_EndpointsReturnUnauthorizedEveryTimeAsync()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var requestTasks = Enumerable.Range(1, 100).Select(_ => client.GetAsync("/"));
        var responses = await Task.WhenAll(requestTasks);

        // Assert
        Assert.All(responses, x =>
        {
            Assert.Equal(HttpStatusCode.Unauthorized, x.StatusCode);
        });
    }
}

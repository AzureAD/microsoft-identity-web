// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Extensions.Hosting;
using Microsoft.Identity.Web.Sidecar;
using Xunit;

namespace Sidecar.Tests;

public class ForwardedHeadersConfigurationTests
{
    [Theory]
    [InlineData("Production", "true")]
    [InlineData("Staging", "TrUe")]
    public void Startup_ForwardedHeadersEnabledOutsideDevelopment_Throws(
        string environmentName,
        string value)
    {
        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() =>
            Program.CreateBuilder(CreateArguments(environmentName, value)));

        Assert.Equal(
            "Forwarded headers cannot be enabled outside Development. " +
            "Remove 'ForwardedHeaders_Enabled' or set it to 'false'.",
            exception.Message);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("false")]
    [InlineData("enabled")]
    [InlineData(" true ")]
    public void Startup_ForwardedHeadersNotActivatedOutsideDevelopment_Succeeds(string? value)
    {
        // Act
        var builder = Program.CreateBuilder(CreateArguments("Production", value));

        // Assert
        Assert.Equal("Production", builder.Environment.EnvironmentName);
    }

    [Fact]
    public void Startup_ForwardedHeadersEnabledInDevelopment_Succeeds()
    {
        // Act
        var builder = Program.CreateBuilder(CreateArguments("Development", "true"));

        // Assert
        Assert.True(builder.Environment.IsDevelopment());
    }

    private static string[] CreateArguments(string environmentName, string? value)
    {
        if (value is null)
        {
            return ["--environment", environmentName];
        }

        return
        [
            "--environment",
            environmentName,
            "--ForwardedHeaders_Enabled",
            value,
        ];
    }
}

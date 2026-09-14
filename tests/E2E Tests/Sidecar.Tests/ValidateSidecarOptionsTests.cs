// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Web.Sidecar.Configuration;
using Xunit;

namespace Sidecar.Tests;

/// <summary>
/// Unit tests for <see cref="ValidateSidecarOptions"/>, the startup validator that bounds the PoP
/// <c>SignedHttpRequestLifetime</c> so a misconfiguration fails the host boot instead of every PoP request.
/// </summary>
public class ValidateSidecarOptionsTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-3600)]
    public void Validate_WithNonPositiveLifetime_Fails(int seconds)
    {
        // Arrange
        var options = new SidecarOptions();
        options.PopValidation.SignedHttpRequestLifetime = TimeSpan.FromSeconds(seconds);

        // Act
        ValidateOptionsResult result = new ValidateSidecarOptions().Validate(name: null, options);

        // Assert
        Assert.True(result.Failed);
        Assert.Contains("SignedHttpRequestLifetime", result.FailureMessage!, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(300)]
    public void Validate_WithPositiveLifetime_Succeeds(int seconds)
    {
        // Arrange
        var options = new SidecarOptions();
        options.PopValidation.SignedHttpRequestLifetime = TimeSpan.FromSeconds(seconds);

        // Act
        ValidateOptionsResult result = new ValidateSidecarOptions().Validate(name: null, options);

        // Assert
        Assert.True(result.Succeeded);
    }

    [Fact]
    public void Validate_WithDefaultOptions_Succeeds()
    {
        // Arrange: binding with no Sidecar:PopValidation section yields the five-minute default.
        var options = new SidecarOptions();

        // Act
        ValidateOptionsResult result = new ValidateSidecarOptions().Validate(name: null, options);

        // Assert
        Assert.True(result.Succeeded);
    }
}

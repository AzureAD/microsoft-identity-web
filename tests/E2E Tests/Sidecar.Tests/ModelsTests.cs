// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json.Nodes;
using Microsoft.Identity.Web.Sidecar.Models;
using Xunit;

namespace Sidecar.Tests;

public class ModelsTests
{
    [Fact]
    public void AuthorizationHeaderResult_WithNullValue_HandlesCorrectly()
    {
        // Arrange & Act
        var result = new AuthorizationHeaderResult(null!);

        // Assert
        Assert.Null(result.AuthorizationHeader);
    }

    [Fact]
    public void AuthorizationHeaderResult_WithEmptyString_HandlesCorrectly()
    {
        // Arrange & Act
        var result = new AuthorizationHeaderResult(string.Empty);

        // Assert
        Assert.Equal(string.Empty, result.AuthorizationHeader);
    }

    [Fact]
    public void AuthorizationHeaderResult_ToString_ReturnsExpectedFormat()
    {
        // Arrange
        var header = "Bearer test-token";
        var result = new AuthorizationHeaderResult(header);

        // Act
        var stringResult = result.ToString();

        // Assert
        Assert.Contains(header, stringResult, StringComparison.Ordinal);
        Assert.Contains("AuthorizationHeaderResult", stringResult, StringComparison.Ordinal);
    }

    [Fact]
    public void ValidateAuthorizationHeaderResult_Constructor_SetsAllProperties()
    {
        // Arrange
        var protocol = "Bearer";
        var token = "token";
        var claims = JsonNode.Parse("""
            {
                "sub": "user123",
                "name": "Test User",
                "scope": "user.read mail.read",
                "aud": "api://test-api",
                "iss": "https://login.microsoftonline.com/test-tenant/v2.0",
                "exp": 1234567890
            }
            """);

        // Act
        var result = new ValidateAuthorizationHeaderResult(protocol, token, claims!);

        // Assert
        Assert.Equal(protocol, result.Protocol);
        Assert.Equal(token, result.Token);
        Assert.Equal(claims, result.Claims);
    }

    [Fact]
    public void ValidateAuthorizationHeaderResult_WithNullClaims_HandlesCorrectly()
    {
        // Arrange & Act
        var result = new ValidateAuthorizationHeaderResult("Bearer", "token", null!);

        // Assert
        Assert.Equal("Bearer", result.Protocol);
        Assert.Equal("token", result.Token);
        Assert.Null(result.Claims);
    }

    [Fact]
    public void ValidateAuthorizationHeaderResult_Equality_WorksCorrectly()
    {
        // Arrange
        var claims = JsonNode.Parse("""{"sub": "user123"}""");
        var result1 = new ValidateAuthorizationHeaderResult("Bearer", "token", claims!);
        var result2 = new ValidateAuthorizationHeaderResult("Bearer", "token", claims!);
        var result3 = new ValidateAuthorizationHeaderResult("Bearer", "different-token", claims!);

        // Act & Assert
        Assert.Equal(result1, result2);
        Assert.NotEqual(result1, result3);
    }

    [Fact]
    public void ValidateAuthorizationHeaderResult_ToString_ReturnsExpectedFormat()
    {
        // Arrange
        var claims = JsonNode.Parse("""{"sub": "user123"}""");
        var result = new ValidateAuthorizationHeaderResult("Bearer", "test-token", claims!);

        // Act
        var stringResult = result.ToString();

        // Assert
        Assert.Contains("Bearer", stringResult, StringComparison.Ordinal);
        Assert.Contains("test-token", stringResult, StringComparison.Ordinal);
        Assert.Contains("ValidateAuthorizationHeaderResult", stringResult, StringComparison.Ordinal);
    }

    [Fact]
    public void ValidateAuthorizationHeaderResult_WithComplexClaims_HandlesCorrectly()
    {
        // Arrange
        var complexClaims = JsonNode.Parse("""
            {
                "sub": "user123",
                "name": "Test User",
                "roles": ["admin", "user"],
                "permissions": {
                    "read": true,
                    "write": false
                },
                "nested": {
                    "deep": {
                        "value": "test"
                    }
                }
            }
            """);

        // Act
        var result = new ValidateAuthorizationHeaderResult("Bearer", "token", complexClaims!);

        // Assert
        Assert.Equal("Bearer", result.Protocol);
        Assert.Equal("token", result.Token);
        Assert.NotNull(result.Claims);
        
        // Verify we can access nested properties
        Assert.Equal("user123", result.Claims["sub"]?.GetValue<string>());
        Assert.Equal("Test User", result.Claims["name"]?.GetValue<string>());
        Assert.NotNull(result.Claims["roles"]?.AsArray());
        Assert.Equal(2, result.Claims["roles"]?.AsArray().Count);
        Assert.Equal("test", result.Claims["nested"]?["deep"]?["value"]?.GetValue<string>());
    }

    [Theory]
    [InlineData("Bearer")]
    [InlineData("Basic")]
    [InlineData("Custom")]
    [InlineData("")]
    public void ValidateAuthorizationHeaderResult_WithDifferentProtocols_HandlesCorrectly(string protocol)
    {
        // Arrange
        var claims = JsonNode.Parse("""{"sub": "user123"}""");

        // Act
        var result = new ValidateAuthorizationHeaderResult(protocol, "token", claims!);

        // Assert
        Assert.Equal(protocol, result.Protocol);
    }

    [Theory]
    [InlineData("short")]
    [InlineData("header.body.signature")]
    [InlineData("")]
    public void ValidateAuthorizationHeaderResult_WithDifferentTokenLengths_HandlesCorrectly(string token)
    {
        // Arrange
        var claims = JsonNode.Parse("""{"sub": "user123"}""");

        // Act
        var result = new ValidateAuthorizationHeaderResult("Bearer", token, claims!);

        // Assert
        Assert.Equal(token, result.Token);
    }
}

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Web.Sidecar;
using Microsoft.Identity.Web.Sidecar.Endpoints;
using Xunit;

namespace Sidecar.Tests;

public class DownstreamApiOptionsMergeTests
{
    [Fact]
    public void MergeDownstreamApiOptionsOverrides_WithNullRight_ReturnsClonedLeft()
    {
        // Arrange
        var left = new DownstreamApiOptions
        {
            Scopes = ["user.read"],
            RelativePath = "/me"
        };

        // Act
        var result = DownstreamApiOptionsMerger.MergeOptions(left, null!);

        // Assert
        Assert.NotSame(left, result);
        Assert.Equal(left.Scopes, result.Scopes);
        Assert.Equal(left.RelativePath, result.RelativePath);
    }

    [Fact]
    public void MergeDownstreamApiOptionsOverrides_WithScopesOverride_OverridesScopes()
    {
        // Arrange
        var left = new DownstreamApiOptions
        {
            Scopes = ["user.read"]
        };
        var right = new DownstreamApiOptions
        {
            Scopes = ["mail.read", "calendars.read"]
        };

        // Act
        var result = DownstreamApiOptionsMerger.MergeOptions(left, right);

        // Assert
        Assert.Equal(["mail.read", "calendars.read"], result.Scopes);
    }

    [Fact]
    public void MergeDownstreamApiOptionsOverrides_WithEmptyScopes_DoesNotOverride()
    {
        // Arrange
        var left = new DownstreamApiOptions
        {
            Scopes = ["user.read"]
        };
        var right = new DownstreamApiOptions
        {
            Scopes = Array.Empty<string>()
        };

        // Act
        var result = DownstreamApiOptionsMerger.MergeOptions(left, right);

        // Assert
        Assert.Equal(["user.read"], result.Scopes);
    }

    [Fact]
    public void MergeDownstreamApiOptionsOverrides_WithTenantOverride_OverridesTenant()
    {
        // Arrange
        var left = new DownstreamApiOptions
        {
            AcquireTokenOptions = new AcquireTokenOptions { Tenant = "original-tenant" }
        };
        var right = new DownstreamApiOptions
        {
            AcquireTokenOptions = new AcquireTokenOptions { Tenant = "new-tenant" }
        };

        // Act
        var result = DownstreamApiOptionsMerger.MergeOptions(left, right);

        // Assert
        Assert.Equal("new-tenant", result.AcquireTokenOptions.Tenant);
    }

    [Fact]
    public void MergeDownstreamApiOptionsOverrides_WithClaimsOverride_OverridesClaims()
    {
        // Arrange
        var left = new DownstreamApiOptions
        {
            AcquireTokenOptions = new AcquireTokenOptions { Claims = "original-claims" }
        };
        var right = new DownstreamApiOptions
        {
            AcquireTokenOptions = new AcquireTokenOptions { Claims = "new-claims" }
        };

        // Act
        var result = DownstreamApiOptionsMerger.MergeOptions(left, right);

        // Assert
        Assert.Equal("new-claims", result.AcquireTokenOptions.Claims);
    }

    [Fact]
    public void MergeDownstreamApiOptionsOverrides_WithAuthenticationOptionsNameOverride_OverridesAuthenticationOptionsName()
    {
        // Arrange
        var left = new DownstreamApiOptions
        {
            AcquireTokenOptions = new AcquireTokenOptions { AuthenticationOptionsName = "original-auth" }
        };
        var right = new DownstreamApiOptions
        {
            AcquireTokenOptions = new AcquireTokenOptions { AuthenticationOptionsName = "new-auth" }
        };

        // Act
        var result = DownstreamApiOptionsMerger.MergeOptions(left, right);

        // Assert
        Assert.Equal("new-auth", result.AcquireTokenOptions.AuthenticationOptionsName);
    }

    [Fact]
    public void MergeDownstreamApiOptionsOverrides_WithFmiPathOverride_OverridesFmiPath()
    {
        // Arrange
        var left = new DownstreamApiOptions
        {
            AcquireTokenOptions = new AcquireTokenOptions { FmiPath = "/original/path" }
        };
        var right = new DownstreamApiOptions
        {
            AcquireTokenOptions = new AcquireTokenOptions { FmiPath = "/new/path" }
        };

        // Act
        var result = DownstreamApiOptionsMerger.MergeOptions(left, right);

        // Assert
        Assert.Equal("/new/path", result.AcquireTokenOptions.FmiPath);
    }

    [Fact]
    public void MergeDownstreamApiOptionsOverrides_WithRelativePathOverride_OverridesRelativePath()
    {
        // Arrange
        var left = new DownstreamApiOptions
        {
            RelativePath = "/original/path"
        };
        var right = new DownstreamApiOptions
        {
            RelativePath = "/new/path"
        };

        // Act
        var result = DownstreamApiOptionsMerger.MergeOptions(left, right);

        // Assert
        Assert.Equal("/new/path", result.RelativePath);
    }

    [Fact]
    public void MergeDownstreamApiOptionsOverrides_WithForceRefreshOverride_OverridesForceRefresh()
    {
        // Arrange
        var left = new DownstreamApiOptions
        {
            AcquireTokenOptions = new AcquireTokenOptions { ForceRefresh = false }
        };
        var right = new DownstreamApiOptions
        {
            AcquireTokenOptions = new AcquireTokenOptions { ForceRefresh = true }
        };

        // Act
        var result = DownstreamApiOptionsMerger.MergeOptions(left, right);

        // Assert
        Assert.True(result.AcquireTokenOptions.ForceRefresh);
    }

    [Fact]
    public void MergeDownstreamApiOptionsOverrides_WithExtraParameters_MergesExtraParameters()
    {
        // Arrange
        var left = new DownstreamApiOptions
        {
            AcquireTokenOptions = new AcquireTokenOptions
            {
                ExtraParameters = new Dictionary<string, object>
                {
                    { "param1", "value1" },
                    { "param2", "value2" }
                }
            }
        };
        var right = new DownstreamApiOptions
        {
            AcquireTokenOptions = new AcquireTokenOptions
            {
                ExtraParameters = new Dictionary<string, object>
                {
                    { "param3", "value3" },
                    { "param4", "value4" }
                }
            }
        };

        // Act
        var result = DownstreamApiOptionsMerger.MergeOptions(left, right);

        // Assert
        Assert.NotNull(result.AcquireTokenOptions.ExtraParameters);
        Assert.Equal(4, result.AcquireTokenOptions.ExtraParameters.Count);
        Assert.Equal("value1", result.AcquireTokenOptions.ExtraParameters["param1"]);
        Assert.Equal("value2", result.AcquireTokenOptions.ExtraParameters["param2"]);
        Assert.Equal("value3", result.AcquireTokenOptions.ExtraParameters["param3"]);
        Assert.Equal("value4", result.AcquireTokenOptions.ExtraParameters["param4"]);
    }

    [Fact]
    public void MergeDownstreamApiOptionsOverrides_WithExtraParametersConflict_DoesNotOverwriteExisting()
    {
        // Arrange
        var left = new DownstreamApiOptions
        {
            AcquireTokenOptions = new AcquireTokenOptions
            {
                ExtraParameters = new Dictionary<string, object>
                {
                    { "param1", "original-value" }
                }
            }
        };
        var right = new DownstreamApiOptions
        {
            AcquireTokenOptions = new AcquireTokenOptions
            {
                ExtraParameters = new Dictionary<string, object>
                {
                    { "param1", "new-value" },
                    { "param2", "value2" }
                }
            }
        };

        // Act
        var result = DownstreamApiOptionsMerger.MergeOptions(left, right);

        // Assert
        Assert.NotNull(result.AcquireTokenOptions.ExtraParameters);
        Assert.Equal("original-value", result.AcquireTokenOptions.ExtraParameters["param1"]);
        Assert.Equal("value2", result.AcquireTokenOptions.ExtraParameters["param2"]);
    }

    [Fact]
    public void MergeDownstreamApiOptionsOverrides_WithRightExtraParametersButLeftNull_CreatesNewDictionary()
    {
        // Arrange
        var left = new DownstreamApiOptions
        {
            AcquireTokenOptions = new AcquireTokenOptions
            {
                ExtraParameters = null
            }
        };
        var right = new DownstreamApiOptions
        {
            AcquireTokenOptions = new AcquireTokenOptions
            {
                ExtraParameters = new Dictionary<string, object>
                {
                    { "param1", "value1" }
                }
            }
        };

        // Act
        var result = DownstreamApiOptionsMerger.MergeOptions(left, right);

        // Assert
        Assert.NotNull(result.AcquireTokenOptions.ExtraParameters);
        Assert.Single(result.AcquireTokenOptions.ExtraParameters);
        Assert.Equal("value1", result.AcquireTokenOptions.ExtraParameters["param1"]);
    }

    [Fact]
    public void MergeDownstreamApiOptionsOverrides_WithComplexScenario_MergesAllOverrides()
    {
        // Arrange
        var left = new DownstreamApiOptions
        {
            Scopes = ["user.read"],
            RelativePath = "/original/path",
            AcquireTokenOptions = new AcquireTokenOptions
            {
                Tenant = "original-tenant",
                Claims = "original-claims",
                ForceRefresh = false,
                ExtraParameters = new Dictionary<string, object>
                {
                    { "original-param", "original-value" }
                }
            }
        };

        var right = new DownstreamApiOptions
        {
            Scopes = ["mail.read", "calendars.read"],
            RelativePath = "/new/path",
            AcquireTokenOptions = new AcquireTokenOptions
            {
                Tenant = "new-tenant",
                AuthenticationOptionsName = "new-auth",
                FmiPath = "/new/fmi",
                ForceRefresh = true,
                ExtraParameters = new Dictionary<string, object>
                {
                    { "new-param", "new-value" }
                }
            }
        };

        // Act
        var result = DownstreamApiOptionsMerger.MergeOptions(left, right);

        // Assert
        Assert.Equal(["mail.read", "calendars.read"], result.Scopes);
        Assert.Equal("/new/path", result.RelativePath);
        Assert.Equal("new-tenant", result.AcquireTokenOptions.Tenant);
        Assert.Equal("original-claims", result.AcquireTokenOptions.Claims); // Not overridden
        Assert.Equal("new-auth", result.AcquireTokenOptions.AuthenticationOptionsName);
        Assert.Equal("/new/fmi", result.AcquireTokenOptions.FmiPath);
        Assert.True(result.AcquireTokenOptions.ForceRefresh);
        Assert.NotNull(result.AcquireTokenOptions.ExtraParameters);
        Assert.Equal(2, result.AcquireTokenOptions.ExtraParameters.Count);
        Assert.Equal("original-value", result.AcquireTokenOptions.ExtraParameters["original-param"]);
        Assert.Equal("new-value", result.AcquireTokenOptions.ExtraParameters["new-param"]);
    }

    [Fact]
    public void MergeDownstreamApiOptionsOverrides_WithEmptyStringOverrides_DoesNotOverride()
    {
        // Arrange
        var left = new DownstreamApiOptions
        {
            RelativePath = "/original/path",
            AcquireTokenOptions = new AcquireTokenOptions
            {
                Tenant = "original-tenant",
                Claims = "original-claims"
            }
        };
        var right = new DownstreamApiOptions
        {
            RelativePath = "",
            AcquireTokenOptions = new AcquireTokenOptions
            {
                Tenant = "",
                Claims = ""
            }
        };

        // Act
        var result = DownstreamApiOptionsMerger.MergeOptions(left, right);

        // Assert
        Assert.Equal("/original/path", result.RelativePath);
        Assert.Equal("original-tenant", result.AcquireTokenOptions.Tenant);
        Assert.Equal("original-claims", result.AcquireTokenOptions.Claims);
    }
}

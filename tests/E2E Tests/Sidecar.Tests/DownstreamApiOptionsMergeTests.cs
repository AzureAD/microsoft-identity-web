// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Web.Sidecar;
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

    [Fact]
    public void MergeDownstreamApiOptionsOverrides_WithExtraHeaderParameters_MergesHeaderParameters()
    {
        // Arrange
        var left = new DownstreamApiOptions
        {
            ExtraHeaderParameters = new Dictionary<string, string>
            {
                { "Header1", "Value1" },
                { "Header2", "Value2" }
            }
        };
        var right = new DownstreamApiOptions
        {
            ExtraHeaderParameters = new Dictionary<string, string>
            {
                { "Header3", "Value3" },
                { "Header4", "Value4" }
            }
        };

        // Act
        var result = DownstreamApiOptionsMerger.MergeOptions(left, right);

        // Assert
        Assert.NotNull(result.ExtraHeaderParameters);
        Assert.Equal(4, result.ExtraHeaderParameters.Count);
        Assert.Equal("Value1", result.ExtraHeaderParameters["Header1"]);
        Assert.Equal("Value2", result.ExtraHeaderParameters["Header2"]);
        Assert.Equal("Value3", result.ExtraHeaderParameters["Header3"]);
        Assert.Equal("Value4", result.ExtraHeaderParameters["Header4"]);
    }

    [Fact]
    public void MergeDownstreamApiOptionsOverrides_WithExtraHeaderParametersConflict_OverwritesWithRight()
    {
        // Arrange
        var left = new DownstreamApiOptions
        {
            ExtraHeaderParameters = new Dictionary<string, string>
            {
                { "Header1", "OriginalValue" }
            }
        };
        var right = new DownstreamApiOptions
        {
            ExtraHeaderParameters = new Dictionary<string, string>
            {
                { "Header1", "NewValue" },
                { "Header2", "Value2" }
            }
        };

        // Act
        var result = DownstreamApiOptionsMerger.MergeOptions(left, right);

        // Assert
        Assert.NotNull(result.ExtraHeaderParameters);
        Assert.Equal("NewValue", result.ExtraHeaderParameters["Header1"]);
        Assert.Equal("Value2", result.ExtraHeaderParameters["Header2"]);
    }

    [Fact]
    public void MergeDownstreamApiOptionsOverrides_WithExtraQueryParameters_MergesQueryParameters()
    {
        // Arrange
        var left = new DownstreamApiOptions
        {
            ExtraQueryParameters = new Dictionary<string, string>
            {
                { "param1", "value1" },
                { "param2", "value2" }
            }
        };
        var right = new DownstreamApiOptions
        {
            ExtraQueryParameters = new Dictionary<string, string>
            {
                { "param3", "value3" },
                { "param4", "value4" }
            }
        };

        // Act
        var result = DownstreamApiOptionsMerger.MergeOptions(left, right);

        // Assert
        Assert.NotNull(result.ExtraQueryParameters);
        Assert.Equal(4, result.ExtraQueryParameters.Count);
        Assert.Equal("value1", result.ExtraQueryParameters["param1"]);
        Assert.Equal("value2", result.ExtraQueryParameters["param2"]);
        Assert.Equal("value3", result.ExtraQueryParameters["param3"]);
        Assert.Equal("value4", result.ExtraQueryParameters["param4"]);
    }

    [Fact]
    public void MergeDownstreamApiOptionsOverrides_WithExtraQueryParametersConflict_OverwritesWithRight()
    {
        // Arrange
        var left = new DownstreamApiOptions
        {
            ExtraQueryParameters = new Dictionary<string, string>
            {
                { "param1", "original-value" }
            }
        };
        var right = new DownstreamApiOptions
        {
            ExtraQueryParameters = new Dictionary<string, string>
            {
                { "param1", "new-value" },
                { "param2", "value2" }
            }
        };

        // Act
        var result = DownstreamApiOptionsMerger.MergeOptions(left, right);

        // Assert
        Assert.NotNull(result.ExtraQueryParameters);
        Assert.Equal("new-value", result.ExtraQueryParameters["param1"]);
        Assert.Equal("value2", result.ExtraQueryParameters["param2"]);
    }

    [Fact]
    public void MergeDownstreamApiOptionsOverrides_WithRightExtraHeaderParametersButLeftNull_CreatesNewDictionary()
    {
        // Arrange
        var left = new DownstreamApiOptions
        {
            ExtraHeaderParameters = null
        };
        var right = new DownstreamApiOptions
        {
            ExtraHeaderParameters = new Dictionary<string, string>
            {
                { "Header1", "Value1" }
            }
        };

        // Act
        var result = DownstreamApiOptionsMerger.MergeOptions(left, right);

        // Assert
        Assert.NotNull(result.ExtraHeaderParameters);
        Assert.Single(result.ExtraHeaderParameters);
        Assert.Equal("Value1", result.ExtraHeaderParameters["Header1"]);
    }

    [Fact]
    public void MergeDownstreamApiOptionsOverrides_WithRightExtraQueryParametersButLeftNull_CreatesNewDictionary()
    {
        // Arrange
        var left = new DownstreamApiOptions
        {
            ExtraQueryParameters = null
        };
        var right = new DownstreamApiOptions
        {
            ExtraQueryParameters = new Dictionary<string, string>
            {
                { "param1", "value1" }
            }
        };

        // Act
        var result = DownstreamApiOptionsMerger.MergeOptions(left, right);

        // Assert
        Assert.NotNull(result.ExtraQueryParameters);
        Assert.Single(result.ExtraQueryParameters);
        Assert.Equal("value1", result.ExtraQueryParameters["param1"]);
    }

    [Fact]
    public void MergeDownstreamApiOptionsOverrides_WithRequestAppTokenTrue_OverridesRequestAppToken()
    {
        // Arrange
        var left = new DownstreamApiOptions
        {
            RequestAppToken = false
        };
        var right = new DownstreamApiOptions
        {
            RequestAppToken = true
        };

        // Act
        var result = DownstreamApiOptionsMerger.MergeOptions(left, right);

        // Assert
        Assert.True(result.RequestAppToken);
    }

    [Fact]
    public void MergeDownstreamApiOptionsOverrides_WithRequestAppTokenFalse_DoesNotOverride()
    {
        // Arrange
        var left = new DownstreamApiOptions
        {
            RequestAppToken = true
        };
        var right = new DownstreamApiOptions
        {
            RequestAppToken = false
        };

        // Act
        var result = DownstreamApiOptionsMerger.MergeOptions(left, right);

        // Assert - left value preserved because right is false (default)
        Assert.True(result.RequestAppToken);
    }

    [Fact]
    public void MergeDownstreamApiOptionsOverrides_WithBaseUrlOverride_OverridesBaseUrl()
    {
        // Arrange
        var left = new DownstreamApiOptions
        {
            BaseUrl = "https://original.api.com/"
        };
        var right = new DownstreamApiOptions
        {
            BaseUrl = "https://new.api.com/"
        };

        // Act
        var result = DownstreamApiOptionsMerger.MergeOptions(left, right);

        // Assert
        Assert.Equal("https://new.api.com/", result.BaseUrl);
    }

    [Fact]
    public void MergeDownstreamApiOptionsOverrides_WithHttpMethodOverride_OverridesHttpMethod()
    {
        // Arrange
        var left = new DownstreamApiOptions
        {
            HttpMethod = "GET"
        };
        var right = new DownstreamApiOptions
        {
            HttpMethod = "POST"
        };

        // Act
        var result = DownstreamApiOptionsMerger.MergeOptions(left, right);

        // Assert
        Assert.Equal("POST", result.HttpMethod);
    }

    [Fact]
    public void MergeDownstreamApiOptionsOverrides_WithManagedIdentityOverride_OverridesManagedIdentity()
    {
        // Arrange
        var left = new DownstreamApiOptions
        {
            AcquireTokenOptions = new AcquireTokenOptions
            {
                ManagedIdentity = null
            }
        };
        var right = new DownstreamApiOptions
        {
            AcquireTokenOptions = new AcquireTokenOptions
            {
                ManagedIdentity = new ManagedIdentityOptions
                {
                    UserAssignedClientId = "test-client-id"
                }
            }
        };

        // Act
        var result = DownstreamApiOptionsMerger.MergeOptions(left, right);

        // Assert
        Assert.NotNull(result.AcquireTokenOptions.ManagedIdentity);
        Assert.Equal("test-client-id", result.AcquireTokenOptions.ManagedIdentity.UserAssignedClientId);
    }

    /// <summary>
    /// This test uses reflection to ensure all properties of DownstreamApiOptions are handled by the merger.
    /// If a new property is added to DownstreamApiOptions and not handled, this test will fail.
    /// </summary>
    [Fact]
    public void MergeDownstreamApiOptionsOverrides_AllPropertiesAreCopied()
    {
        // Arrange - Properties that are expected to be merged from DownstreamApiOptions
        var handledProperties = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            // Direct properties
            "Scopes",
            "RequestAppToken",
            "BaseUrl",
            "RelativePath",
            "HttpMethod",
            "ContentType",
            "AcceptHeader",
            "ExtraHeaderParameters",
            "ExtraQueryParameters",
            // AcquireTokenOptions is handled specially (nested object)
            "AcquireTokenOptions",
            // Properties intentionally not merged (they use CustomizeHttpRequestMessage pattern or are clone-only)
            "Clone",
            "CustomizeHttpRequestMessage",
            "Serializer",
            "Deserializer",
            "ProtocolScheme",
        };

        // Act - Get all public instance properties of DownstreamApiOptions
        var allProperties = typeof(DownstreamApiOptions)
            .GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)
            .Where(p => p.CanRead && p.CanWrite)
            .Select(p => p.Name)
            .ToList();

        // Assert - Every property should be in our handled list
        var unhandledProperties = allProperties
            .Where(p => !handledProperties.Contains(p))
            .ToList();

        Assert.True(
            unhandledProperties.Count == 0,
            $"The following properties of DownstreamApiOptions are not handled by the merger: {string.Join(", ", unhandledProperties)}. " +
            "Please add handling for these properties in DownstreamApiOptionsMerger.MergeOptions() and add them to this test's handledProperties list.");
    }

    /// <summary>
    /// This test uses reflection to ensure all properties of AcquireTokenOptions are handled by the merger.
    /// </summary>
    [Fact]
    public void MergeDownstreamApiOptionsOverrides_AllAcquireTokenOptionsPropertiesAreCopied()
    {
        // Arrange - Properties that are expected to be merged from AcquireTokenOptions
        var handledProperties = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Tenant",
            "Claims",
            "AuthenticationOptionsName",
            "FmiPath",
            "LongRunningWebApiSessionKey",
            "PopPublicKey",
            "CorrelationId",
            "ManagedIdentity",
            "ForceRefresh",
            "ExtraQueryParameters",
            "ExtraParameters",
            "ExtraHeadersParameters",
            // Properties intentionally not merged
            "UserFlow",
            "PopCryptoProvider",
            "PoPConfiguration",
        };

        // Act - Get all public instance properties of AcquireTokenOptions
        var allProperties = typeof(AcquireTokenOptions)
            .GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)
            .Where(p => p.CanRead && p.CanWrite)
            .Select(p => p.Name)
            .ToList();

        // Assert - Every property should be in our handled list
        var unhandledProperties = allProperties
            .Where(p => !handledProperties.Contains(p))
            .ToList();

        Assert.True(
            unhandledProperties.Count == 0,
            $"The following properties of AcquireTokenOptions are not handled by the merger: {string.Join(", ", unhandledProperties)}. " +
            "Please add handling for these properties in DownstreamApiOptionsMerger.MergeOptions() and add them to this test's handledProperties list.");
    }
}

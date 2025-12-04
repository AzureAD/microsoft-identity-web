// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Web.OidcFic;
using Xunit;

namespace Microsoft.Identity.Web.Test
{
    public class OidcIdpSignedAssertionProviderTests
    {
        [Theory]
        [InlineData("https://login.microsoftonline.com/my-tenant-id/oauth2/v2.0/token", "https://login.microsoftonline.com/", "my-tenant-id")]
        [InlineData("https://login.microsoftonline.com/contoso.onmicrosoft.com/oauth2/v2.0/token", "https://login.microsoftonline.com", "contoso.onmicrosoft.com")]
        [InlineData("https://login.microsoftonline.com/12345678-1234-1234-1234-123456789abc/oauth2/v2.0/token", "https://login.microsoftonline.com/", "12345678-1234-1234-1234-123456789abc")]
        public void ExtractTenantFromTokenEndpointIfSameInstance_SameInstance_ReturnsTenant(
            string tokenEndpoint,
            string configuredInstance,
            string expectedTenant)
        {
            // Act
            var result = OidcIdpSignedAssertionProvider.ExtractTenantFromTokenEndpointIfSameInstance(
                tokenEndpoint,
                configuredInstance);

            // Assert
            Assert.Equal(expectedTenant, result);
        }

        [Theory]
        [InlineData("https://login.microsoftonline.us/my-tenant-id/oauth2/v2.0/token", "https://login.microsoftonline.com/", null)]
        [InlineData("https://login.microsoftonline.com/my-tenant-id/oauth2/v2.0/token", "https://login.microsoftonline.us/", null)]
        [InlineData("https://login.chinacloudapi.cn/my-tenant-id/oauth2/v2.0/token", "https://login.microsoftonline.com/", null)]
        public void ExtractTenantFromTokenEndpointIfSameInstance_DifferentInstance_ReturnsNull(
            string tokenEndpoint,
            string configuredInstance,
            string? expectedResult)
        {
            // Act
            var result = OidcIdpSignedAssertionProvider.ExtractTenantFromTokenEndpointIfSameInstance(
                tokenEndpoint,
                configuredInstance);

            // Assert
            Assert.Equal(expectedResult, result);
        }

        [Theory]
        [InlineData(null, "https://login.microsoftonline.com/")]
        [InlineData("", "https://login.microsoftonline.com/")]
        [InlineData("https://login.microsoftonline.com/tenant/oauth2/v2.0/token", null)]
        [InlineData("https://login.microsoftonline.com/tenant/oauth2/v2.0/token", "")]
        [InlineData(null, null)]
        [InlineData("", "")]
        public void ExtractTenantFromTokenEndpointIfSameInstance_NullOrEmptyInputs_ReturnsNull(
            string? tokenEndpoint,
            string? configuredInstance)
        {
            // Act
            var result = OidcIdpSignedAssertionProvider.ExtractTenantFromTokenEndpointIfSameInstance(
                tokenEndpoint,
                configuredInstance);

            // Assert
            Assert.Null(result);
        }

        [Theory]
        [InlineData("not-a-valid-uri", "https://login.microsoftonline.com/")]
        [InlineData("https://login.microsoftonline.com/tenant/oauth2/v2.0/token", "not-a-valid-uri")]
        public void ExtractTenantFromTokenEndpointIfSameInstance_InvalidUri_ReturnsNull(
            string tokenEndpoint,
            string configuredInstance)
        {
            // Act
            var result = OidcIdpSignedAssertionProvider.ExtractTenantFromTokenEndpointIfSameInstance(
                tokenEndpoint,
                configuredInstance);

            // Assert
            Assert.Null(result);
        }

        [Theory]
        [InlineData("https://login.microsoftonline.com/oauth2/v2.0/token", "https://login.microsoftonline.com/")]
        [InlineData("https://login.microsoftonline.com/", "https://login.microsoftonline.com/")]
        public void ExtractTenantFromTokenEndpointIfSameInstance_NoTenantInPath_ReturnsNull(
            string tokenEndpoint,
            string configuredInstance)
        {
            // Act
            var result = OidcIdpSignedAssertionProvider.ExtractTenantFromTokenEndpointIfSameInstance(
                tokenEndpoint,
                configuredInstance);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void ExtractTenantFromTokenEndpointIfSameInstance_ValidatesOAuth2Pattern()
        {
            // Arrange
            // This endpoint has a tenant but no oauth2 segment - should return null
            var tokenEndpoint = "https://login.microsoftonline.com/my-tenant/some-other-path";
            var configuredInstance = "https://login.microsoftonline.com/";

            // Act
            var result = OidcIdpSignedAssertionProvider.ExtractTenantFromTokenEndpointIfSameInstance(
                tokenEndpoint,
                configuredInstance);

            // Assert
            Assert.Null(result);
        }
    }
}

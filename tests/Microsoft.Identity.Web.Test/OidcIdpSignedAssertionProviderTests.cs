// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Extensibility;
using Microsoft.Identity.Web.OidcFic;
using NSubstitute;
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

        // Regression tests for the FIC OTel enrichment gap (SEAL bug 3696484): the signed-assertion provider
        // must forward the outer request's OpenTelemetry tags enricher (surfaced by MSAL on
        // AssertionRequestOptions) onto the inner FIC leg's AcquireTokenOptions, using the ExtraParameters
        // channel that TokenAcquisition reads to call WithOtelTagsEnricher. Without this, the inner FIC
        // credential-exchange metrics lack the enrichment tags applied to the outer acquisition.
        [Fact]
        public async Task GetSignedAssertionAsync_ForwardsOtelTagsEnricher_OntoInnerLeg()
        {
            // Arrange
            AcquireTokenOptions? capturedInnerOptions = null;
            var tokenAcquirer = Substitute.For<ITokenAcquirer>();
            tokenAcquirer
                .GetTokenForAppAsync(Arg.Any<string>(), Arg.Any<AcquireTokenOptions?>(), Arg.Any<CancellationToken>())
                .Returns(callInfo =>
                {
                    capturedInnerOptions = callInfo.ArgAt<AcquireTokenOptions?>(1);
                    return Task.FromResult(new AcquireTokenResult(
                        "inner-assertion", DateTimeOffset.UtcNow.AddHours(1), "tenant", null!, new[] { "scope" }, Guid.NewGuid(), "Bearer"));
                });

            var factory = Substitute.For<ITokenAcquirerFactory>();
            factory.GetTokenAcquirer(Arg.Any<IdentityApplicationOptions>()).Returns(tokenAcquirer);

            var provider = new OidcIdpSignedAssertionProvider(
                factory,
                new MicrosoftIdentityApplicationOptions { Instance = "https://login.microsoftonline.com/", TenantId = "t", ClientId = "c" },
                tokenExchangeUrl: null,
                logger: null);

            Action<ExecutionResult, IList<KeyValuePair<string, object>>> enricher = (_, _) => { };
            var assertionRequestOptions = new AssertionRequestOptions { OtelTagsEnricher = enricher };

            // Act
            await provider.GetSignedAssertionAsync(assertionRequestOptions);

            // Assert
            Assert.NotNull(capturedInnerOptions);
            Assert.NotNull(capturedInnerOptions!.ExtraParameters);
            Assert.True(capturedInnerOptions.ExtraParameters!.TryGetValue(Constants.OtelTagsEnricherKey, out object? forwarded));
            Assert.Same(enricher, forwarded);
        }

        [Fact]
        public async Task GetSignedAssertionAsync_NoEnricher_DoesNotSetOtelTagsEnricherKey()
        {
            // Arrange
            AcquireTokenOptions? capturedInnerOptions = null;
            var tokenAcquirer = Substitute.For<ITokenAcquirer>();
            tokenAcquirer
                .GetTokenForAppAsync(Arg.Any<string>(), Arg.Any<AcquireTokenOptions?>(), Arg.Any<CancellationToken>())
                .Returns(callInfo =>
                {
                    capturedInnerOptions = callInfo.ArgAt<AcquireTokenOptions?>(1);
                    return Task.FromResult(new AcquireTokenResult(
                        "inner-assertion", DateTimeOffset.UtcNow.AddHours(1), "tenant", null!, new[] { "scope" }, Guid.NewGuid(), "Bearer"));
                });

            var factory = Substitute.For<ITokenAcquirerFactory>();
            factory.GetTokenAcquirer(Arg.Any<IdentityApplicationOptions>()).Returns(tokenAcquirer);

            var provider = new OidcIdpSignedAssertionProvider(
                factory,
                new MicrosoftIdentityApplicationOptions { Instance = "https://login.microsoftonline.com/", TenantId = "t", ClientId = "c" },
                tokenExchangeUrl: null,
                logger: null);

            // Act: no enricher on the options.
            await provider.GetSignedAssertionAsync(new AssertionRequestOptions());

            // Assert: the enricher key is never set when the outer request carried no enricher.
            Assert.False(capturedInnerOptions?.ExtraParameters?.ContainsKey(Constants.OtelTagsEnricherKey) == true);
        }
    }
}

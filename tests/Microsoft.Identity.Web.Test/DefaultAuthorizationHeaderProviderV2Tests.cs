// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Cache;
using Microsoft.Identity.Web.Test.Common;
using NSubstitute;
using Xunit;

namespace Microsoft.Identity.Web.Test
{
    [Collection(nameof(TokenAcquirerFactorySingletonProtection))]
    public class DefaultAuthorizationHeaderProviderV2Tests
    {
        private readonly ITokenAcquisition _tokenAcquisition;
        private readonly IAuthorizationHeaderProvider2 _provider;

        public DefaultAuthorizationHeaderProviderV2Tests()
        {
            _tokenAcquisition = Substitute.For<ITokenAcquisition>();
            _provider = new DefaultAuthorizationHeaderProvider(_tokenAcquisition);
        }

        private static AuthenticationResult NewAuthResult(string token = "test_access_token")
            => new(
                token,
                isExtendedLifeTimeToken: false,
                uniqueId: null,
                expiresOn: DateTimeOffset.UtcNow.AddHours(1),
                extendedExpiresOn: DateTimeOffset.UtcNow.AddHours(1),
                tenantId: "test_tenant_id",
                account: null,
                idToken: null,
                scopes: new[] { "scope1" },
                correlationId: Guid.NewGuid());

        [Fact]
        public async Task CreateAuthorizationHeaderInformationForUserAsync_ReturnsBearerHeaderAsync()
        {
            // Arrange
            _tokenAcquisition
                .GetAuthenticationResultForUserAsync(
                    Arg.Any<IEnumerable<string>>(), Arg.Any<string>(), Arg.Any<string>(),
                    Arg.Any<string>(), Arg.Any<ClaimsPrincipal>(), Arg.Any<TokenAcquisitionOptions>())
                .Returns(Task.FromResult(NewAuthResult()));

            // Act
            var result = await _provider.CreateAuthorizationHeaderInformationForUserAsync(
                new[] { "scope" }, authorizationHeaderProviderOptions: null, new ClaimsPrincipal(), CancellationToken.None);

            // Assert
            Assert.True(result.Succeeded);
            Assert.Equal("Bearer test_access_token", result.Result!.AuthorizationHeaderValue);
            Assert.Null(result.Result.BindingCertificate);
        }

        [Fact]
        public async Task CreateAuthorizationHeaderInformationForAppAsync_ReturnsBearerHeaderAsync()
        {
            // Arrange
            _tokenAcquisition
                .GetAuthenticationResultForAppAsync(
                    Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<TokenAcquisitionOptions>())
                .Returns(Task.FromResult(NewAuthResult()));

            // Act
            var result = await _provider.CreateAuthorizationHeaderInformationForAppAsync(
                "https://graph.microsoft.com/.default", downstreamApiOptions: null, CancellationToken.None);

            // Assert
            Assert.True(result.Succeeded);
            Assert.Equal("Bearer test_access_token", result.Result!.AuthorizationHeaderValue);
            Assert.Null(result.Result.BindingCertificate);
        }

        [Fact]
        public async Task CreateAuthorizationHeaderInformationAsync_WithRequestAppToken_TakesAppFlowAsync()
        {
            // Arrange
            _tokenAcquisition
                .GetAuthenticationResultForAppAsync(
                    Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<TokenAcquisitionOptions>())
                .Returns(Task.FromResult(NewAuthResult()));

            var options = new AuthorizationHeaderProviderOptions { RequestAppToken = true };

            // Act
            var result = await _provider.CreateAuthorizationHeaderInformationAsync(
                new[] { "https://graph.microsoft.com/.default" }, options, claimsPrincipal: null, CancellationToken.None);

            // Assert
            Assert.True(result.Succeeded);
            Assert.Equal("Bearer test_access_token", result.Result!.AuthorizationHeaderValue);
            await _tokenAcquisition.Received(1).GetAuthenticationResultForAppAsync(
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<TokenAcquisitionOptions>());
        }

        [Fact]
        public async Task CreateAuthorizationHeaderInformationAsync_TokenBindingWithoutAppToken_ThrowsAsync()
        {
            // Arrange
            var options = new DownstreamApiOptions
            {
                Scopes = new[] { "scope" },
                ProtocolScheme = "MTLS_POP",
                RequestAppToken = false
            };

            // Act / Assert
            await Assert.ThrowsAsync<ArgumentException>(() =>
                _provider.CreateAuthorizationHeaderInformationAsync(
                    options.Scopes!, options, claimsPrincipal: null, CancellationToken.None));
        }

        [Fact]
        public async Task CreateAuthorizationHeaderInformationForUserAsync_PropagatesMetadataAsync()
        {
            // Arrange — inject AdditionalResponseParameters via the MSAL ctor; metadata is set via
            // reflection because MSAL's AuthenticationResultMetadata property has no public setter.
            var auth = NewAuthResult();
            var metadata = new AuthenticationResultMetadata(TokenSource.Cache)
            {
                CacheLevel = CacheLevel.L1Cache,
                DurationTotalInMs = 42,
            };
            typeof(AuthenticationResult).GetProperty("AuthenticationResultMetadata")!
                .SetValue(auth, metadata);

            var addl = new Dictionary<string, string> { ["extra"] = "value" };
            typeof(AuthenticationResult).GetProperty("AdditionalResponseParameters")!
                .SetValue(auth, addl);

            _tokenAcquisition
                .GetAuthenticationResultForUserAsync(
                    Arg.Any<IEnumerable<string>>(), Arg.Any<string>(), Arg.Any<string>(),
                    Arg.Any<string>(), Arg.Any<ClaimsPrincipal>(), Arg.Any<TokenAcquisitionOptions>())
                .Returns(Task.FromResult(auth));

            // Act
            var result = await _provider.CreateAuthorizationHeaderInformationForUserAsync(
                new[] { "scope" }, authorizationHeaderProviderOptions: null, new ClaimsPrincipal(), CancellationToken.None);

            // Assert
            Assert.True(result.Succeeded);
            Assert.NotNull(result.Result!.Metadata);
            Assert.Equal(AcquiredTokenSource.Cache, result.Result.Metadata!.TokenSource);
            Assert.Equal(AcquiredTokenCacheLevel.L1Cache, result.Result.Metadata.CacheLevel);
            Assert.Equal(42, result.Result.Metadata.DurationTotalInMs);
            Assert.NotNull(result.Result.AdditionalResponseParameters);
            Assert.Equal("value", result.Result.AdditionalResponseParameters!["extra"]);
        }
    }
}

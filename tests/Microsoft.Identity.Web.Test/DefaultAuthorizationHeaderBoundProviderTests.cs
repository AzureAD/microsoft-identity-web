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
using Microsoft.Identity.Web.Test.Common;
using NSubstitute;
using Xunit;

namespace Microsoft.Identity.Web.Test
{
    [Collection(nameof(TokenAcquirerFactorySingletonProtection))]
    public class DefaultAuthorizationHeaderBoundProviderTests
    {
        private readonly IAuthorizationHeaderProvider _mockAuthorizationHeaderProvider;
        private readonly ITokenAcquisition _mockTokenAcquisition;
        private readonly DefaultAuthorizationHeaderBoundProvider _provider;

        public DefaultAuthorizationHeaderBoundProviderTests()
        {
            _mockAuthorizationHeaderProvider = Substitute.For<IAuthorizationHeaderProvider>();
            _mockTokenAcquisition = Substitute.For<ITokenAcquisition>();
            _provider = new DefaultAuthorizationHeaderBoundProvider(
                _mockAuthorizationHeaderProvider,
                _mockTokenAcquisition);
        }

        [Fact]
        public void Constructor_WithValidParameters_InitializesCorrectly()
        {
            // Arrange & Act
            var provider = new DefaultAuthorizationHeaderBoundProvider(
                _mockAuthorizationHeaderProvider,
                _mockTokenAcquisition);

            // Assert
            Assert.NotNull(provider);
        }

        [Fact]
        public async Task CreateAuthorizationHeaderAsync_BoundProvider_WithValidOptions_ReturnsSuccessResult()
        {
            // Arrange
            var downstreamApiOptions = new DownstreamApiOptions
            {
                Scopes = new[] { "https://graph.microsoft.com/.default" }
            };

            var mockAuthenticationResult = new AuthenticationResult(
                "access_token",
                false,
                null,
                DateTimeOffset.UtcNow.AddHours(1),
                DateTimeOffset.UtcNow.AddHours(1),
                "tenant_id",
                null,
                null,
                new[] { "scope1" },
                Guid.NewGuid());

            _mockTokenAcquisition
                .GetAuthenticationResultForAppAsync(
                    Arg.Any<string>(),
                    Arg.Any<string>(),
                    Arg.Any<string>(),
                    Arg.Any<TokenAcquisitionOptions>())
                .Returns(Task.FromResult(mockAuthenticationResult));

            var claimsPrincipal = new ClaimsPrincipal();
            var cancellationToken = CancellationToken.None;

            // Act
            var result = await _provider.CreateAuthorizationHeaderAsync(
                downstreamApiOptions,
                claimsPrincipal,
                cancellationToken);

            // Assert
            Assert.NotNull(result.Result);
            Assert.Equal("Bearer access_token", result.Result.AuthorizationHeaderValue);

            await _mockTokenAcquisition.Received(1).GetAuthenticationResultForAppAsync(
                "https://graph.microsoft.com/.default",
                null,
                null,
                Arg.Any<TokenAcquisitionOptions>());
        }

        [Fact]
        public async Task CreateAuthorizationHeaderAsync_BoundProvider_WithEmptyScopes_UsesEmptyString()
        {
            // Arrange
            var downstreamApiOptions = new DownstreamApiOptions
            {
                Scopes = new string[0] // Empty scopes
            };

            var mockAuthenticationResult = new AuthenticationResult(
                "access_token",
                false,
                null,
                DateTimeOffset.UtcNow.AddHours(1),
                DateTimeOffset.UtcNow.AddHours(1),
                "tenant_id",
                null,
                null,
                new[] { "scope1" },
                Guid.NewGuid());

            _mockTokenAcquisition
                .GetAuthenticationResultForAppAsync(
                    Arg.Any<string>(),
                    Arg.Any<string>(),
                    Arg.Any<string>(),
                    Arg.Any<TokenAcquisitionOptions>())
                .Returns(Task.FromResult(mockAuthenticationResult));

            // Act
            var result = await _provider.CreateAuthorizationHeaderAsync(
                downstreamApiOptions,
                null,
                CancellationToken.None);

            // Assert
            Assert.NotNull(result.Result);
            await _mockTokenAcquisition.Received(1).GetAuthenticationResultForAppAsync(
                string.Empty, // Should use empty string when no scopes
                null,
                null,
                Arg.Any<TokenAcquisitionOptions>());
        }

        [Fact]
        public async Task CreateAuthorizationHeaderAsync_BoundProvider_WithAcquireTokenOptions_PassesCorrectParameters()
        {
            // Arrange
            var downstreamApiOptions = new DownstreamApiOptions
            {
                Scopes = new[] { "https://graph.microsoft.com/.default" },
                AcquireTokenOptions = new AcquireTokenOptions
                {
                    AuthenticationOptionsName = "TestAuth",
                    Tenant = "test-tenant"
                }
            };

            var mockAuthenticationResult = new AuthenticationResult(
                "access_token",
                false,
                null,
                DateTimeOffset.UtcNow.AddHours(1),
                DateTimeOffset.UtcNow.AddHours(1),
                "tenant_id",
                null,
                null,
                new[] { "scope1" },
                Guid.NewGuid());

            _mockTokenAcquisition
                .GetAuthenticationResultForAppAsync(
                    Arg.Any<string>(),
                    Arg.Any<string>(),
                    Arg.Any<string>(),
                    Arg.Any<TokenAcquisitionOptions>())
                .Returns(Task.FromResult(mockAuthenticationResult));

            // Act
            await _provider.CreateAuthorizationHeaderAsync(
                downstreamApiOptions,
                null,
                CancellationToken.None);

            // Assert
            await _mockTokenAcquisition.Received(1).GetAuthenticationResultForAppAsync(
                "https://graph.microsoft.com/.default",
                "TestAuth",
                "test-tenant",
                Arg.Any<TokenAcquisitionOptions>());
        }

        [Fact]
        public async Task CreateAuthorizationHeaderAsync_DelegatedProvider_CallsUnderlyingProvider()
        {
            // Arrange
            var scopes = new[] { "User.Read", "Mail.Read" };
            var options = new AuthorizationHeaderProviderOptions();
            var claimsPrincipal = new ClaimsPrincipal();
            var cancellationToken = CancellationToken.None;
            var expectedHeader = "Bearer test-token";

            _mockAuthorizationHeaderProvider
                .CreateAuthorizationHeaderAsync(scopes, options, claimsPrincipal, cancellationToken)
                .Returns(Task.FromResult(expectedHeader));

            // Act
            var result = await _provider.CreateAuthorizationHeaderAsync(scopes, options, claimsPrincipal, cancellationToken);

            // Assert
            Assert.Equal(expectedHeader, result);
            await _mockAuthorizationHeaderProvider.Received(1)
                .CreateAuthorizationHeaderAsync(scopes, options, claimsPrincipal, cancellationToken);
        }

        [Fact]
        public async Task CreateAuthorizationHeaderForAppAsync_CallsUnderlyingProvider()
        {
            // Arrange
            var scopes = "https://graph.microsoft.com/.default";
            var options = new AuthorizationHeaderProviderOptions();
            var cancellationToken = CancellationToken.None;
            var expectedHeader = "Bearer app-token";

            _mockAuthorizationHeaderProvider
                .CreateAuthorizationHeaderForAppAsync(scopes, options, cancellationToken)
                .Returns(Task.FromResult(expectedHeader));

            // Act
            var result = await _provider.CreateAuthorizationHeaderForAppAsync(scopes, options, cancellationToken);

            // Assert
            Assert.Equal(expectedHeader, result);
            await _mockAuthorizationHeaderProvider.Received(1)
                .CreateAuthorizationHeaderForAppAsync(scopes, options, cancellationToken);
        }

        [Fact]
        public async Task CreateAuthorizationHeaderForUserAsync_CallsUnderlyingProvider()
        {
            // Arrange
            var scopes = new[] { "User.Read", "Mail.Read" };
            var options = new AuthorizationHeaderProviderOptions();
            var claimsPrincipal = new ClaimsPrincipal();
            var cancellationToken = CancellationToken.None;
            var expectedHeader = "Bearer user-token";

            _mockAuthorizationHeaderProvider
                .CreateAuthorizationHeaderForUserAsync(scopes, options, claimsPrincipal, cancellationToken)
                .Returns(Task.FromResult(expectedHeader));

            // Act
            var result = await _provider.CreateAuthorizationHeaderForUserAsync(scopes, options, claimsPrincipal, cancellationToken);

            // Assert
            Assert.Equal(expectedHeader, result);
            await _mockAuthorizationHeaderProvider.Received(1)
                .CreateAuthorizationHeaderForUserAsync(scopes, options, claimsPrincipal, cancellationToken);
        }

        [Fact]
        public async Task CreateAuthorizationHeaderAsync_DelegatedProvider_WithNullParameters_CallsUnderlyingProvider()
        {
            // Arrange
            var scopes = new[] { "User.Read" };
            var expectedHeader = "Bearer test-token";

            _mockAuthorizationHeaderProvider
                .CreateAuthorizationHeaderAsync(scopes, null, null, CancellationToken.None)
                .Returns(Task.FromResult(expectedHeader));

            // Act
            var result = await _provider.CreateAuthorizationHeaderAsync(scopes, null, null, CancellationToken.None);

            // Assert
            Assert.Equal(expectedHeader, result);
            await _mockAuthorizationHeaderProvider.Received(1)
                .CreateAuthorizationHeaderAsync(scopes, null, null, CancellationToken.None);
        }

        [Fact]
        public async Task CreateAuthorizationHeaderForAppAsync_WithNullOptions_CallsUnderlyingProvider()
        {
            // Arrange
            var scopes = "https://graph.microsoft.com/.default";
            var expectedHeader = "Bearer app-token";

            _mockAuthorizationHeaderProvider
                .CreateAuthorizationHeaderForAppAsync(scopes, null, CancellationToken.None)
                .Returns(Task.FromResult(expectedHeader));

            // Act
            var result = await _provider.CreateAuthorizationHeaderForAppAsync(scopes, null, CancellationToken.None);

            // Assert
            Assert.Equal(expectedHeader, result);
            await _mockAuthorizationHeaderProvider.Received(1)
                .CreateAuthorizationHeaderForAppAsync(scopes, null, CancellationToken.None);
        }

        [Fact]
        public async Task CreateAuthorizationHeaderForUserAsync_WithNullParameters_CallsUnderlyingProvider()
        {
            // Arrange
            var scopes = new[] { "User.Read" };
            var expectedHeader = "Bearer user-token";

            _mockAuthorizationHeaderProvider
                .CreateAuthorizationHeaderForUserAsync(scopes, null, null, CancellationToken.None)
                .Returns(Task.FromResult(expectedHeader));

            // Act
            var result = await _provider.CreateAuthorizationHeaderForUserAsync(scopes, null, null, CancellationToken.None);

            // Assert
            Assert.Equal(expectedHeader, result);
            await _mockAuthorizationHeaderProvider.Received(1)
                .CreateAuthorizationHeaderForUserAsync(scopes, null, null, CancellationToken.None);
        }

        [Fact]
        public async Task CreateAuthorizationHeaderAsync_BoundProvider_TokenAcquisitionThrows_PropagatesException()
        {
            // Arrange
            var downstreamApiOptions = new DownstreamApiOptions
            {
                Scopes = new[] { "https://graph.microsoft.com/.default" }
            };

            var expectedException = new MsalServiceException("test-error", "Test error message");
            _mockTokenAcquisition
                .GetAuthenticationResultForAppAsync(
                    Arg.Any<string>(),
                    Arg.Any<string>(),
                    Arg.Any<string>(),
                    Arg.Any<TokenAcquisitionOptions>())
                .Returns(Task.FromException<AuthenticationResult>(expectedException));

            // Act & Assert
            var actualException = await Assert.ThrowsAsync<MsalServiceException>(
                () => _provider.CreateAuthorizationHeaderAsync(downstreamApiOptions, null, CancellationToken.None));

            Assert.Equal(expectedException.ErrorCode, actualException.ErrorCode);
            Assert.Equal(expectedException.Message, actualException.Message);
        }
    }
}
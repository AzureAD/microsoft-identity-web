// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
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
    public class DefaultAuthorizationHeaderProviderTests
    {
        private readonly ITokenAcquisition _mockTokenAcquisition;
        private readonly DefaultAuthorizationHeaderProvider _provider;

        public DefaultAuthorizationHeaderProviderTests()
        {
            _mockTokenAcquisition = Substitute.For<ITokenAcquisition>();
            _provider = new DefaultAuthorizationHeaderProvider(_mockTokenAcquisition);
        }

        [Fact]
        public void Constructor_WithValidParameters_InitializesCorrectly()
        {
            // Arrange & Act
            var provider = new DefaultAuthorizationHeaderProvider(_mockTokenAcquisition);

            // Assert
            Assert.NotNull(provider);
        }

        [Fact]
        public async Task CreateAuthorizationHeaderAsync_ForBoundHeaderProviderWithNonMtlsProtocolAndUserFlow_ReturnsValidResult()
        {
            // Arrange
            var downstreamApiOptions = new DownstreamApiOptions
            {
                Scopes = new[] { "https://graph.microsoft.com/.default" }
            };

            var mockAuthenticationResult = new AuthenticationResult(
                "test_access_token",
                false,
                null,
                DateTimeOffset.UtcNow.AddHours(1),
                DateTimeOffset.UtcNow.AddHours(1),
                "test_tenant_id",
                null,
                null,
                new[] { "scope1" },
                Guid.NewGuid());

            _mockTokenAcquisition
                .GetAuthenticationResultForUserAsync(
                    Arg.Any<IEnumerable<string>>(),
                    Arg.Any<string>(),
                    Arg.Any<string>(),
                    Arg.Any<string>(),
                    Arg.Any<ClaimsPrincipal>(),
                    Arg.Any<TokenAcquisitionOptions>())
                .Returns(Task.FromResult(mockAuthenticationResult));

            var claimsPrincipal = new ClaimsPrincipal();
            var cancellationToken = CancellationToken.None;

            // Act
            var result = await ((IBoundAuthorizationHeaderProvider)_provider).CreateBoundAuthorizationHeaderAsync(
                downstreamApiOptions,
                claimsPrincipal,
                cancellationToken);

            // Assert
            Assert.NotNull(result.Result);
            Assert.Equal("Bearer test_access_token", result.Result.AuthorizationHeaderValue);
            Assert.Null(result.Result.BindingCertificate);

            await _mockTokenAcquisition.Received(1).GetAuthenticationResultForUserAsync(
                Arg.Any<IEnumerable<string>>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<ClaimsPrincipal>(),
                Arg.Any<TokenAcquisitionOptions>());
        }

        [Fact]
        public async Task CreateAuthorizationHeaderAsync_ForBoundHeaderProviderWithNonMtlsProtocolAndAppFlow_ReturnsValidResult()
        {
            // Arrange
            var downstreamApiOptions = new DownstreamApiOptions
            {
                Scopes = new[] { "https://graph.microsoft.com/.default" },
                RequestAppToken = true
            };

            var mockAuthenticationResult = new AuthenticationResult(
                "test_access_token",
                false,
                null,
                DateTimeOffset.UtcNow.AddHours(1),
                DateTimeOffset.UtcNow.AddHours(1),
                "test_tenant_id",
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
            var result = await ((IBoundAuthorizationHeaderProvider)_provider).CreateBoundAuthorizationHeaderAsync(
                downstreamApiOptions,
                claimsPrincipal,
                cancellationToken);

            // Assert
            Assert.NotNull(result.Result);
            Assert.Equal("Bearer test_access_token", result.Result.AuthorizationHeaderValue);
            Assert.Null(result.Result.BindingCertificate);

            await _mockTokenAcquisition.Received(1).GetAuthenticationResultForAppAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<TokenAcquisitionOptions>());
        }

        [Fact]
        public async Task CreateAuthorizationHeaderAsync_ForBoundHeaderProviderWithMtlsProtocolAndAppFlow_ReturnsValidResult()
        {
            // Arrange
            var downstreamApiOptions = new DownstreamApiOptions
            {
                Scopes = new[] { "https://graph.microsoft.com/.default" },
                ProtocolScheme = "MTLS_POP",
                RequestAppToken = true
            };

            var mockAuthenticationResult = new AuthenticationResult(
                "test_access_token",
                false,
                null,
                DateTimeOffset.UtcNow.AddHours(1),
                DateTimeOffset.UtcNow.AddHours(1),
                "test_tenant_id",
                null,
                null,
                new[] { "scope1" },
                Guid.NewGuid(),
                "MTLS_POP");

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
            var result = await ((IBoundAuthorizationHeaderProvider)_provider).CreateBoundAuthorizationHeaderAsync(
                downstreamApiOptions,
                claimsPrincipal,
                cancellationToken);

            // Assert
            Assert.NotNull(result.Result);
            Assert.Equal("MTLS_POP test_access_token", result.Result.AuthorizationHeaderValue);

            await _mockTokenAcquisition.Received(1).GetAuthenticationResultForAppAsync(
                "https://graph.microsoft.com/.default",
                null,
                null,
                Arg.Is<TokenAcquisitionOptions>(o =>
                    o.ExtraParameters != null &&
                    o.ExtraParameters.ContainsKey("IsTokenBinding") &&
                    o.ExtraParameters["IsTokenBinding"] is bool &&
                    (bool)o.ExtraParameters["IsTokenBinding"] == true));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(null)]
        public async Task CreateAuthorizationHeaderAsync_ForBoundHeaderProviderWithMtlsProtocolForUserFlow_ThrowsArgumentException(bool? requestAppToken)
        {
            // Arrange
            var downstreamApiOptions = new DownstreamApiOptions
            {
                Scopes = new[] { "https://graph.microsoft.com/.default" },
                ProtocolScheme = "MTLS_POP"
            };

            if (requestAppToken.HasValue)
            {
                downstreamApiOptions.RequestAppToken = requestAppToken.Value;
            }

            var claimsPrincipal = new ClaimsPrincipal();
            var cancellationToken = CancellationToken.None;

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(
                () => ((IBoundAuthorizationHeaderProvider)_provider).CreateBoundAuthorizationHeaderAsync(
                    downstreamApiOptions,
                    claimsPrincipal,
                    cancellationToken));

            Assert.Contains("Token binding requires enabled app token acquisition", exception.Message, StringComparison.OrdinalIgnoreCase);
            Assert.Equal("RequestAppToken", exception.ParamName);
        }

        [Fact]
        public async Task CreateAuthorizationHeaderAsync_ForBoundHeaderProviderWithBindingCertificate_ReturnsBindingCertificate()
        {
            // Arrange
            var downstreamApiOptions = new DownstreamApiOptions
            {
                Scopes = new[] { "https://graph.microsoft.com/.default" },
                ProtocolScheme = "MTLS_POP",
                RequestAppToken = true
            };

            // Create test certificate
            var bytes = Convert.FromBase64String(TestConstants.CertificateX5c);
#if NET9_0_OR_GREATER
            var bindingCertificate = X509CertificateLoader.LoadCertificate(bytes);
#else
#pragma warning disable SYSLIB0057 // Type or member is obsolete
            var bindingCertificate = new X509Certificate2(bytes);
#pragma warning restore SYSLIB0057 // Type or member is obsolete
#endif

            var mockAuthenticationResult = new AuthenticationResult(
                "test_access_token",
                false,
                null,
                DateTimeOffset.UtcNow.AddHours(1),
                DateTimeOffset.UtcNow.AddHours(1),
                "test_tenant_id",
                null,
                null,
                new[] { "scope1" },
                Guid.NewGuid(),
                "MTLS_POP")
            {
                BindingCertificate = bindingCertificate
            };

            _mockTokenAcquisition
                .GetAuthenticationResultForAppAsync(
                    Arg.Any<string>(),
                    Arg.Any<string>(),
                    Arg.Any<string>(),
                    Arg.Any<TokenAcquisitionOptions>())
                .Returns(Task.FromResult(mockAuthenticationResult));

            // Act
            var result = await ((IBoundAuthorizationHeaderProvider)_provider).CreateBoundAuthorizationHeaderAsync(
                downstreamApiOptions,
                null,
                CancellationToken.None);

            // Assert
            Assert.NotNull(result.Result);
            Assert.Equal("MTLS_POP test_access_token", result.Result.AuthorizationHeaderValue);
            Assert.Same(bindingCertificate, result.Result.BindingCertificate);
        }

        [Fact]
        public async Task CreateAuthorizationHeaderAsync_ForBoundHeaderProviderWithNullScopes_HandlesGracefully()
        {
            // Arrange
            var downstreamApiOptions = new DownstreamApiOptions
            {
                Scopes = null, // Null scopes
                ProtocolScheme = "MTLS_POP",
                RequestAppToken = true
            };

            var mockAuthenticationResult = new AuthenticationResult(
                "test_access_token",
                false,
                null,
                DateTimeOffset.UtcNow.AddHours(1),
                DateTimeOffset.UtcNow.AddHours(1),
                "test_tenant_id",
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

            // Act & Assert - Should not throw
            var result = await ((IBoundAuthorizationHeaderProvider)_provider).CreateBoundAuthorizationHeaderAsync(
                downstreamApiOptions,
                null,
                CancellationToken.None);

            Assert.NotNull(result.Result);
        }

        [Fact]
        public async Task CreateAuthorizationHeaderAsync_WithUserScopes_AcquiresUserToken()
        {
            // Arrange
            var scopes = new[] { "User.Read", "Mail.Read" };
            var options = new AuthorizationHeaderProviderOptions();
            var claimsPrincipal = new ClaimsPrincipal();
            var cancellationToken = CancellationToken.None;
            var expectedHeader = "Bearer test-token";

            var mockAuthenticationResult = new AuthenticationResult(
                "test-token",
                false,
                null,
                DateTimeOffset.UtcNow.AddHours(1),
                DateTimeOffset.UtcNow.AddHours(1),
                "tenant_id",
                null,
                null,
                scopes,
                Guid.NewGuid());

            _mockTokenAcquisition
                .GetAuthenticationResultForUserAsync(
                    scopes,
                    null,
                    null,
                    null,
                    claimsPrincipal,
                    Arg.Any<TokenAcquisitionOptions>())
                .Returns(Task.FromResult(mockAuthenticationResult));

            // Act
            var result = await ((IAuthorizationHeaderProvider)_provider).CreateAuthorizationHeaderAsync(scopes, options, claimsPrincipal, cancellationToken);

            // Assert
            Assert.Equal(expectedHeader, result);
            await _mockTokenAcquisition.Received(1)
                .GetAuthenticationResultForUserAsync(scopes, null, null, null, claimsPrincipal, Arg.Any<TokenAcquisitionOptions>());
        }

        [Fact]
        public async Task CreateAuthorizationHeaderForAppAsync_AcquiresAppToken()
        {
            // Arrange
            var scopes = "https://graph.microsoft.com/.default";
            var options = new AuthorizationHeaderProviderOptions();
            var cancellationToken = CancellationToken.None;
            var expectedHeader = "Bearer app-token";

            var mockAuthenticationResult = new AuthenticationResult(
                "app-token",
                false,
                null,
                DateTimeOffset.UtcNow.AddHours(1),
                DateTimeOffset.UtcNow.AddHours(1),
                "tenant_id",
                null,
                null,
                new[] { scopes },
                Guid.NewGuid());

            _mockTokenAcquisition
                .GetAuthenticationResultForAppAsync(
                    scopes,
                    null,
                    null,
                    Arg.Any<TokenAcquisitionOptions>())
                .Returns(Task.FromResult(mockAuthenticationResult));

            // Act
            var result = await ((IAuthorizationHeaderProvider)_provider).CreateAuthorizationHeaderForAppAsync(scopes, options, cancellationToken);

            // Assert
            Assert.Equal(expectedHeader, result);
            await _mockTokenAcquisition.Received(1)
                .GetAuthenticationResultForAppAsync(scopes, null, null, Arg.Any<TokenAcquisitionOptions>());
        }

        [Fact]
        public async Task CreateAuthorizationHeaderForUserAsync_AcquiresUserToken()
        {
            // Arrange
            var scopes = new[] { "User.Read", "Mail.Read" };
            var options = new AuthorizationHeaderProviderOptions();
            var claimsPrincipal = new ClaimsPrincipal();
            var cancellationToken = CancellationToken.None;
            var expectedHeader = "Bearer user-token";

            var mockAuthenticationResult = new AuthenticationResult(
                "user-token",
                false,
                null,
                DateTimeOffset.UtcNow.AddHours(1),
                DateTimeOffset.UtcNow.AddHours(1),
                "tenant_id",
                null,
                null,
                scopes,
                Guid.NewGuid());

            _mockTokenAcquisition
                .GetAuthenticationResultForUserAsync(
                    scopes,
                    null,
                    null,
                    null,
                    claimsPrincipal,
                    Arg.Any<TokenAcquisitionOptions>())
                .Returns(Task.FromResult(mockAuthenticationResult));

            // Act
            var result = await ((IAuthorizationHeaderProvider)_provider).CreateAuthorizationHeaderForUserAsync(scopes, options, claimsPrincipal, cancellationToken);

            // Assert
            Assert.Equal(expectedHeader, result);
            await _mockTokenAcquisition.Received(1)
                .GetAuthenticationResultForUserAsync(scopes, null, null, null, claimsPrincipal, Arg.Any<TokenAcquisitionOptions>());
        }

        [Fact]
        public async Task CreateAuthorizationHeaderAsync_WithNullParameters_AcquiresUserToken()
        {
            // Arrange
            var scopes = new[] { "User.Read" };
            var expectedHeader = "Bearer test-token";

            var mockAuthenticationResult = new AuthenticationResult(
                "test-token",
                false,
                null,
                DateTimeOffset.UtcNow.AddHours(1),
                DateTimeOffset.UtcNow.AddHours(1),
                "tenant_id",
                null,
                null,
                scopes,
                Guid.NewGuid());

            _mockTokenAcquisition
                .GetAuthenticationResultForUserAsync(
                    scopes,
                    null,
                    null,
                    null,
                    null,
                    Arg.Any<TokenAcquisitionOptions>())
                .Returns(Task.FromResult(mockAuthenticationResult));

            // Act
            var result = await ((IAuthorizationHeaderProvider)_provider).CreateAuthorizationHeaderAsync(scopes, null, null, CancellationToken.None);

            // Assert
            Assert.Equal(expectedHeader, result);
            await _mockTokenAcquisition.Received(1)
                .GetAuthenticationResultForUserAsync(scopes, null, null, null, null, Arg.Any<TokenAcquisitionOptions>());
        }

        [Fact]
        public async Task CreateAuthorizationHeaderForAppAsync_WithNullOptions_AcquiresAppToken()
        {
            // Arrange
            var scopes = "https://graph.microsoft.com/.default";
            var expectedHeader = "Bearer app-token";

            var mockAuthenticationResult = new AuthenticationResult(
                "app-token",
                false,
                null,
                DateTimeOffset.UtcNow.AddHours(1),
                DateTimeOffset.UtcNow.AddHours(1),
                "tenant_id",
                null,
                null,
                new[] { scopes },
                Guid.NewGuid());

            _mockTokenAcquisition
                .GetAuthenticationResultForAppAsync(
                    scopes,
                    null,
                    null,
                    Arg.Any<TokenAcquisitionOptions>())
                .Returns(Task.FromResult(mockAuthenticationResult));

            // Act
            var result = await ((IAuthorizationHeaderProvider)_provider).CreateAuthorizationHeaderForAppAsync(scopes, null, CancellationToken.None);

            // Assert
            Assert.Equal(expectedHeader, result);
            await _mockTokenAcquisition.Received(1)
                .GetAuthenticationResultForAppAsync(scopes, null, null, Arg.Any<TokenAcquisitionOptions>());
        }

        [Fact]
        public async Task CreateAuthorizationHeaderForUserAsync_WithNullParameters_AcquiresUserToken()
        {
            // Arrange
            var scopes = new[] { "User.Read" };
            var expectedHeader = "Bearer user-token";

            var mockAuthenticationResult = new AuthenticationResult(
                "user-token",
                false,
                null,
                DateTimeOffset.UtcNow.AddHours(1),
                DateTimeOffset.UtcNow.AddHours(1),
                "tenant_id",
                null,
                null,
                scopes,
                Guid.NewGuid());

            _mockTokenAcquisition
                .GetAuthenticationResultForUserAsync(
                    scopes,
                    null,
                    null,
                    null,
                    null,
                    Arg.Any<TokenAcquisitionOptions>())
                .Returns(Task.FromResult(mockAuthenticationResult));

            // Act
            var result = await ((IAuthorizationHeaderProvider)_provider).CreateAuthorizationHeaderForUserAsync(scopes, null, null, CancellationToken.None);

            // Assert
            Assert.Equal(expectedHeader, result);
            await _mockTokenAcquisition.Received(1)
                .GetAuthenticationResultForUserAsync(scopes, null, null, null, null, Arg.Any<TokenAcquisitionOptions>());
        }

        [Fact]
        public async Task CreateAuthorizationHeaderAsync_ForBoundHeaderProvider_TokenAcquisitionThrows_PropagatesException()
        {
            // Arrange
            var downstreamApiOptions = new DownstreamApiOptions
            {
                Scopes = new[] { "https://graph.microsoft.com/.default" },
                ProtocolScheme = "MTLS_POP",
                RequestAppToken = true
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
                () => ((IBoundAuthorizationHeaderProvider)_provider).CreateBoundAuthorizationHeaderAsync(downstreamApiOptions, null, CancellationToken.None));

            Assert.Equal(expectedException.ErrorCode, actualException.ErrorCode);
            Assert.Equal(expectedException.Message, actualException.Message);
        }

        [Fact]
        public async Task CreateAuthorizationHeaderAsync_ForBoundHeaderProvider_WithExistingExtraParameters_MergesExtraParameters()
        {
            // Arrange
            var existingParameters = new Dictionary<string, object>
            {
                { "custom_param", "custom_value" }
            };

            var downstreamApiOptions = new DownstreamApiOptions
            {
                Scopes = new[] { "https://graph.microsoft.com/.default" },
                ProtocolScheme = "MTLS_POP",
                RequestAppToken = true,
                AcquireTokenOptions = new AcquireTokenOptions
                {
                    ExtraParameters = existingParameters
                }
            };

            var mockAuthenticationResult = new AuthenticationResult(
                "test_access_token",
                false,
                null,
                DateTimeOffset.UtcNow.AddHours(1),
                DateTimeOffset.UtcNow.AddHours(1),
                "test_tenant_id",
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
            await ((IBoundAuthorizationHeaderProvider)_provider).CreateBoundAuthorizationHeaderAsync(
                downstreamApiOptions,
                null,
                CancellationToken.None);

            // Assert
            await _mockTokenAcquisition.Received(1).GetAuthenticationResultForAppAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Is<TokenAcquisitionOptions>(o =>
                    o.ExtraParameters != null &&
                    o.ExtraParameters.ContainsKey("IsTokenBinding") &&
                    o.ExtraParameters.ContainsKey("custom_param") &&
                    (bool)o.ExtraParameters["IsTokenBinding"] == true &&
                    (string)o.ExtraParameters["custom_param"] == "custom_value"));
        }
    }
}

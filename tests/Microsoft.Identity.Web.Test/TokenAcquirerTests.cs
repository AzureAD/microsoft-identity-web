// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Reflection;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Client;
using NSubstitute;
using Xunit;

namespace Microsoft.Identity.Web.Test
{
    public class TokenAcquirerTests
    {
        private readonly ITokenAcquisition _tokenAcquisition;
        private readonly string _scope = "https://graph.microsoft.com/.default";
        private readonly string _accessToken = "test_access_token";
        private readonly string _tenantId = "test_tenant_id";
        private readonly string _idToken = "test_id_token";
        private readonly DateTimeOffset _expiresOn = DateTimeOffset.UtcNow.AddHours(1);
        private readonly Guid _correlationId = Guid.NewGuid();
        private readonly string _tokenType = "Bearer";
        private readonly string _authenticationScheme = "TestScheme";
        private readonly X509Certificate2 _bindingCertificate;

        public TokenAcquirerTests()
        {
            _tokenAcquisition = Substitute.For<ITokenAcquisition>();

            // Create a test certificate for BindingCertificate scenarios
            using var rsa = RSA.Create();
            var request = new CertificateRequest(new X500DistinguishedName("CN=Test"), rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            _bindingCertificate = request.CreateSelfSigned(DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddYears(1));
        }

        [Fact]
        public async Task GetTokenForAppAsync_WithoutBindingCertificate_ReturnsCorrectAcquireTokenResult()
        {
            // Arrange
            var authResult = CreateMockAuthenticationResult();
            _tokenAcquisition.GetAuthenticationResultForAppAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<TokenAcquisitionOptions>())
                .Returns(authResult);

            var tokenAcquirer = new TokenAcquirer(_tokenAcquisition, _authenticationScheme);

            // Act
            var result = await ((ITokenAcquirer)tokenAcquirer).GetTokenForAppAsync(
                _scope,
                null,
                CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(_accessToken, result.AccessToken);
            Assert.Equal(_expiresOn, result.ExpiresOn);
            Assert.Equal(_tenantId, result.TenantId);
            Assert.Equal(_idToken, result.IdToken);
            Assert.Equal(new[] { _scope }, result.Scopes);
            Assert.Equal(_correlationId, result.CorrelationId);
            Assert.Equal(_tokenType, result.TokenType);
            Assert.Null(result.BindingCertificate);
        }

        [Fact]
        public async Task GetTokenForAppAsync_WithBindingCertificate_ReturnsAcquireTokenResultWithBindingCertificate()
        {
            // Arrange
            var authResult = CreateMockAuthenticationResult(bindingCertificate: _bindingCertificate);
            _tokenAcquisition.GetAuthenticationResultForAppAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<TokenAcquisitionOptions>())
                .Returns(authResult);

            var tokenAcquirer = new TokenAcquirer(_tokenAcquisition, _authenticationScheme);

            // Act
            var result = await ((ITokenAcquirer)tokenAcquirer).GetTokenForAppAsync(
                _scope,
                null,
                CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(_accessToken, result.AccessToken);
            Assert.Equal(_expiresOn, result.ExpiresOn);
            Assert.Equal(_tenantId, result.TenantId);
            Assert.Equal(_idToken, result.IdToken);
            Assert.Equal(new[] { _scope }, result.Scopes);
            Assert.Equal(_correlationId, result.CorrelationId);
            Assert.Equal(_tokenType, result.TokenType);
            Assert.NotNull(result.BindingCertificate);
            Assert.Equal(_bindingCertificate.Thumbprint, result.BindingCertificate.Thumbprint);
        }

        private AuthenticationResult CreateMockAuthenticationResult(X509Certificate2? bindingCertificate = null)
        {
            var authResult = new AuthenticationResult(
                _accessToken,
                false,
                null,
                _expiresOn,
                _expiresOn,
                _tenantId,
                null,
                _idToken,
                new[] { _scope },
                _correlationId);

            // Unfortunately, MSAL's AuthenticationResult.BindingCertificate is not settable,
            // and we can't mock it, so we'll use a custom AuthenticationResult wrapper
            // or test the functionality through integration tests
            if (bindingCertificate != null)
            {
                // Use reflection to set the BindingCertificate property since it's not exposed in the constructor
                var bindingCertificateProperty = typeof(AuthenticationResult).GetProperty("BindingCertificate");
                bindingCertificateProperty?.SetValue(authResult, bindingCertificate);
            }

            return authResult;
        }
    }
}

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Azure.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Abstractions;
using NSubstitute;
using Xunit;

namespace Microsoft.Identity.Web.Test
{
    public class MicrosoftIdentityTokenCredentialTests
    {
        private readonly ITokenAcquirer _tokenAcquirer;
        private readonly ITokenAcquirerFactory _tokenAcquirerFactory;
        private readonly Abstractions.IAuthenticationSchemeInformationProvider _authenticationSchemeInformationProvider;
        private readonly string[] _scopes = new[] { "https://graph.microsoft.com/.default" };
        private readonly string _accessToken = "mock_access_token";
        private readonly DateTimeOffset _expiresOn = DateTimeOffset.UtcNow.AddHours(1);
        private readonly ClaimsPrincipal _claimsPrincipal = new ClaimsPrincipal();
        private readonly AcquireTokenResult _tokenResult;

        public MicrosoftIdentityTokenCredentialTests()
        {
            // Create a token result to be returned
            _tokenResult = new AcquireTokenResult(
                _accessToken,
                _expiresOn,
                tenantId: "tenant-id",
                idToken: "eY..",
                scopes: _scopes,
                Guid.Empty,
                "bearer");
            
            // Setup mock objects
            _tokenAcquirer = Substitute.For<ITokenAcquirer>();
            _tokenAcquirerFactory = Substitute.For<ITokenAcquirerFactory>();
            _authenticationSchemeInformationProvider = Substitute.For<Microsoft.Identity.Abstractions.IAuthenticationSchemeInformationProvider>();

            // Configure mocks
            _authenticationSchemeInformationProvider.GetEffectiveAuthenticationScheme(Arg.Any<string>())
                .Returns("Default");

            _tokenAcquirerFactory.GetTokenAcquirer(Arg.Any<string>())
                .Returns(_tokenAcquirer);
            
            // Setup token results for user token
            _tokenAcquirer.GetTokenForUserAsync(
                Arg.Any<IEnumerable<string>>(),
                Arg.Any<AcquireTokenOptions>(),
                Arg.Any<ClaimsPrincipal>(),
                Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(_tokenResult));

            // Setup token results for app token
            _tokenAcquirer.GetTokenForAppAsync(
                Arg.Any<string>(),
                Arg.Any<AcquireTokenOptions>(),
                Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(_tokenResult));
        }

        [Fact]
        public void GetToken_ForUser_ReturnsValidToken()
        {
            // Arrange
            var credential = new MicrosoftIdentityTokenCredential(_tokenAcquirerFactory, _authenticationSchemeInformationProvider);
            var context = new TokenRequestContext(_scopes);

            // Act
            var token = credential.GetToken(context, CancellationToken.None);

            // Assert
            Assert.Equal(_accessToken, token.Token);
            Assert.Equal(_expiresOn, token.ExpiresOn);
            
            // Verify the call was made - need to store the return value to avoid warning
            _ = _tokenAcquirer.Received().GetTokenForUserAsync(
                Arg.Is<IEnumerable<string>>(s => s == _scopes),
                Arg.Any<AcquireTokenOptions>(),
                Arg.Any<ClaimsPrincipal>(),
                Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task GetTokenAsync_ForUser_ReturnsValidToken()
        {
            // Arrange
            var credential = new MicrosoftIdentityTokenCredential(_tokenAcquirerFactory, _authenticationSchemeInformationProvider);
            var context = new TokenRequestContext(_scopes);

            // Act
            var token = await credential.GetTokenAsync(context, CancellationToken.None);

            // Assert
            Assert.Equal(_accessToken, token.Token);
            Assert.Equal(_expiresOn, token.ExpiresOn);
            
            // Verify the call was made - need to store the return value to avoid warning
            _ = await _tokenAcquirer.Received().GetTokenForUserAsync(
                Arg.Is<IEnumerable<string>>(s => s == _scopes),
                Arg.Any<AcquireTokenOptions>(),
                Arg.Any<ClaimsPrincipal>(),
                Arg.Any<CancellationToken>());
        }

        [Fact]
        public void GetToken_ForApp_ReturnsValidToken()
        {
            // Arrange
            var credential = new MicrosoftIdentityTokenCredential(_tokenAcquirerFactory, _authenticationSchemeInformationProvider);
            credential.Options.RequestAppToken = true;
            var context = new TokenRequestContext(_scopes);

            // Act
            var token = credential.GetToken(context, CancellationToken.None);

            // Assert
            Assert.Equal(_accessToken, token.Token);
            Assert.Equal(_expiresOn, token.ExpiresOn);
            
            // Verify the call was made - need to store the return value to avoid warning
            _ = _tokenAcquirer.Received().GetTokenForAppAsync(
                Arg.Is<string>(s => s == _scopes[0]),
                Arg.Any<AcquireTokenOptions>(),
                Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task GetTokenAsync_ForApp_ReturnsValidToken()
        {
            // Arrange
            var credential = new MicrosoftIdentityTokenCredential(_tokenAcquirerFactory, _authenticationSchemeInformationProvider);
            credential.Options.RequestAppToken = true;
            var context = new TokenRequestContext(_scopes);

            // Act
            var token = await credential.GetTokenAsync(context, CancellationToken.None);

            // Assert
            Assert.Equal(_accessToken, token.Token);
            Assert.Equal(_expiresOn, token.ExpiresOn);
            
            // Verify the call was made - need to store the return value to avoid warning
            _ = await _tokenAcquirer.Received().GetTokenForAppAsync(
                Arg.Is<string>(s => s == _scopes[0]),
                Arg.Any<AcquireTokenOptions>(),
                Arg.Any<CancellationToken>());
        }

        [Fact]
        public void Constructor_WithNullTokenAcquirerFactory_ThrowsArgumentNullException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() => 
                new MicrosoftIdentityTokenCredential(null!, _authenticationSchemeInformationProvider));
            
            Assert.Equal("tokenAcquirerFactory", exception.ParamName);
        }

        [Fact]
        public void Constructor_WithNullAuthenticationSchemeProvider_ThrowsArgumentNullException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() => 
                new MicrosoftIdentityTokenCredential(_tokenAcquirerFactory, null!));
            
            Assert.Equal("authenticationSchemeInformationProvider", exception.ParamName);
        }
    }
}

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Memory;
using Microsoft.Identity.Client;
using Microsoft.IdentityModel.Tokens;
using NSubstitute;
using Xunit;

namespace Microsoft.Identity.Web.Test.Blazor
{
    public class BlazorAuthenticationChallengeHandlerTests
    {
        private readonly NavigationManager _mockNavigationManager;
        private readonly AuthenticationStateProvider _mockAuthStateProvider;
        private readonly IConfiguration _configuration;

        public BlazorAuthenticationChallengeHandlerTests()
        {
            _mockNavigationManager = Substitute.For<NavigationManager>();
            _mockAuthStateProvider = Substitute.For<AuthenticationStateProvider>();

            var configData = new System.Collections.Generic.Dictionary<string, string?>
            {
                { "WeatherApi:Scopes:0", "api://test-api/access_as_user" }
            };

            _configuration = new ConfigurationBuilder()
                .Add(new MemoryConfigurationSource { InitialData = configData })
                .Build();
        }

        [Fact]
        public async Task GetUserAsync_ReturnsClaimsPrincipal()
        {
            // Arrange
            var expectedUser = new ClaimsPrincipal(new CaseSensitiveClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Name, "test@example.com")
            }, "TestAuth"));

            var authState = new AuthenticationState(expectedUser);
            _mockAuthStateProvider.GetAuthenticationStateAsync().Returns(authState);

            var handler = new BlazorAuthenticationChallengeHandler(
                _mockNavigationManager,
                _mockAuthStateProvider,
                _configuration);

            // Act
            var user = await handler.GetUserAsync();

            // Assert
            Assert.NotNull(user);
            Assert.Equal(expectedUser, user);
        }

        [Fact]
        public async Task IsAuthenticatedAsync_ReturnsTrueForAuthenticatedUser()
        {
            // Arrange
            var authenticatedUser = new ClaimsPrincipal(new CaseSensitiveClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Name, "test@example.com")
            }, "TestAuth"));

            var authState = new AuthenticationState(authenticatedUser);
            _mockAuthStateProvider.GetAuthenticationStateAsync().Returns(authState);

            var handler = new BlazorAuthenticationChallengeHandler(
                _mockNavigationManager,
                _mockAuthStateProvider,
                _configuration);

            // Act
            var isAuthenticated = await handler.IsAuthenticatedAsync();

            // Assert
            Assert.True(isAuthenticated);
        }

        [Fact]
        public async Task IsAuthenticatedAsync_ReturnsFalseForUnauthenticatedUser()
        {
            // Arrange
            var unauthenticatedUser = new ClaimsPrincipal(new CaseSensitiveClaimsIdentity());

            var authState = new AuthenticationState(unauthenticatedUser);
            _mockAuthStateProvider.GetAuthenticationStateAsync().Returns(authState);

            var handler = new BlazorAuthenticationChallengeHandler(
                _mockNavigationManager,
                _mockAuthStateProvider,
                _configuration);

            // Act
            var isAuthenticated = await handler.IsAuthenticatedAsync();

            // Assert
            Assert.False(isAuthenticated);
        }

        [Fact(Skip = "NavigationManager.Uri and NavigationManager.NavigateTo cannot be mocked. Integration tests verify this behavior.")]
        public async Task HandleExceptionAsync_DetectsMicrosoftIdentityWebChallengeUserException()
        {
            // Arrange
            var user = new ClaimsPrincipal(new CaseSensitiveClaimsIdentity(new[]
            {
                new Claim("preferred_username", "test@example.com"),
                new Claim("tid", "test-tenant-id")
            }, "TestAuth"));

            var authState = new AuthenticationState(user);
            _mockAuthStateProvider.GetAuthenticationStateAsync().Returns(authState);

            var handler = new BlazorAuthenticationChallengeHandler(
                _mockNavigationManager,
                _mockAuthStateProvider,
                _configuration);

            var scopes = new[] { "user.read" };
            var msalException = new MsalUiRequiredException("error_code", "error_message");
            var challengeException = new MicrosoftIdentityWebChallengeUserException(msalException, scopes);

            // Act & Assert
            // Note: Since NavigationManager.NavigateTo is not virtual, actual navigation behavior
            // is tested in integration tests. Here we verify exception detection logic.
            var handled = await handler.HandleExceptionAsync(challengeException);
            Assert.True(handled);
        }

        [Fact(Skip = "NavigationManager.Uri and NavigationManager.NavigateTo cannot be mocked. Integration tests verify this behavior.")]
        public async Task HandleExceptionAsync_DetectsMicrosoftIdentityWebChallengeUserExceptionAsInnerException()
        {
            // Arrange
            var user = new ClaimsPrincipal(new CaseSensitiveClaimsIdentity(new[]
            {
                new Claim("preferred_username", "test@example.com"),
                new Claim("tid", "test-tenant-id")
            }, "TestAuth"));

            var authState = new AuthenticationState(user);
            _mockAuthStateProvider.GetAuthenticationStateAsync().Returns(authState);

            var handler = new BlazorAuthenticationChallengeHandler(
                _mockNavigationManager,
                _mockAuthStateProvider,
                _configuration);

            var scopes = new[] { "user.read" };
            var msalException = new MsalUiRequiredException("error_code", "error_message");
            var challengeException = new MicrosoftIdentityWebChallengeUserException(msalException, scopes);
            var outerException = new InvalidOperationException("Outer exception", challengeException);

            // Act & Assert
            var handled = await handler.HandleExceptionAsync(outerException);
            Assert.True(handled);
        }

        [Fact]
        public async Task HandleExceptionAsync_ReturnsFalseForNonChallengeException()
        {
            // Arrange
            var user = new ClaimsPrincipal(new CaseSensitiveClaimsIdentity());
            var authState = new AuthenticationState(user);
            _mockAuthStateProvider.GetAuthenticationStateAsync().Returns(authState);

            var handler = new BlazorAuthenticationChallengeHandler(
                _mockNavigationManager,
                _mockAuthStateProvider,
                _configuration);

            var regularException = new InvalidOperationException("Regular exception");

            // Act
            var handled = await handler.HandleExceptionAsync(regularException);

            // Assert
            Assert.False(handled);
        }

        // Note: Additional tests for ChallengeUser, GetLoginHint, and GetDomainHint
        // behavior are covered in integration tests since NavigationManager.NavigateTo()
        // and NavigationManager.Uri are not virtual and cannot be mocked.
        // These tests validate URL construction and parameter passing through real Blazor components.
    }
}

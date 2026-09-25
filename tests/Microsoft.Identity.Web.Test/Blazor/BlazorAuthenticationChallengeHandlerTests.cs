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
        /// <summary>
        /// Concrete <see cref="NavigationManager"/> for unit tests. <c>Uri</c> is driven by
        /// <see cref="NavigationManager.Initialize(string, string)"/> and navigation is captured
        /// by overriding <see cref="NavigationManager.NavigateToCore(string, NavigationOptions)"/> —
        /// the same pattern the ASP.NET Core repo uses to test NavigationManager consumers.
        /// </summary>
        private sealed class TestNavigationManager : NavigationManager
        {
            public string? LastNavigatedTo { get; private set; }
            public bool LastForceLoad { get; private set; }

            public TestNavigationManager(string baseUri, string uri)
            {
                Initialize(baseUri, uri);
            }

            protected override void NavigateToCore(string uri, NavigationOptions options)
            {
                LastNavigatedTo = uri;
                LastForceLoad = options.ForceLoad;
            }
        }

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

        [Fact]
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

            var navigation = new TestNavigationManager(
                "https://app.contoso.com/",
                "https://app.contoso.com/weather?day=2");

            var handler = new BlazorAuthenticationChallengeHandler(
                navigation,
                _mockAuthStateProvider,
                _configuration);

            var scopes = new[] { "user.read" };
            var msalException = new MsalUiRequiredException("error_code", "error_message");
            var challengeException = new MicrosoftIdentityWebChallengeUserException(msalException, scopes);

            // Act
            var handled = await handler.HandleExceptionAsync(challengeException);

            // Assert
            Assert.True(handled);
            Assert.NotNull(navigation.LastNavigatedTo);
            Assert.True(navigation.LastForceLoad);
            Assert.Contains("scope=", navigation.LastNavigatedTo, StringComparison.Ordinal);
            Assert.Contains(Uri.EscapeDataString("user.read"), navigation.LastNavigatedTo, StringComparison.Ordinal);
        }

        [Fact]
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

            var navigation = new TestNavigationManager(
                "https://app.contoso.com/",
                "https://app.contoso.com/weather?day=2");

            var handler = new BlazorAuthenticationChallengeHandler(
                navigation,
                _mockAuthStateProvider,
                _configuration);

            var scopes = new[] { "user.read" };
            var msalException = new MsalUiRequiredException("error_code", "error_message");
            var challengeException = new MicrosoftIdentityWebChallengeUserException(msalException, scopes);
            var outerException = new InvalidOperationException("Outer exception", challengeException);

            // Act
            var handled = await handler.HandleExceptionAsync(outerException);

            // Assert
            Assert.True(handled);
            Assert.NotNull(navigation.LastNavigatedTo);
        }

        // -----------------------------------------------------------------------------
        // returnUrl shape (issue #3895): the /login endpoint mapped by MapLoginAndLogout
        // validates returnUrl with RedirectUriHelper.IsLocalUrl, which rejects absolute
        // URLs and falls back to "/". ChallengeUser must therefore send the app-local
        // PathAndQuery of the current page — not NavigationManager.Uri verbatim — or the
        // user loses their page after the consent round-trip.
        // -----------------------------------------------------------------------------

        [Fact]
        public void ChallengeUser_SendsLocalReturnUrl_PreservingPathAndQuery()
        {
            // Arrange
            var navigation = new TestNavigationManager(
                "https://app.contoso.com/",
                "https://app.contoso.com/admin/reports?tab=2");

            var handler = new BlazorAuthenticationChallengeHandler(
                navigation,
                _mockAuthStateProvider,
                _configuration);

            // Act
            handler.ChallengeUser(new ClaimsPrincipal(new CaseSensitiveClaimsIdentity()), new[] { "user.read" });

            // Assert
            Assert.NotNull(navigation.LastNavigatedTo);
            Assert.StartsWith(
                $"/authentication/login?returnUrl={Uri.EscapeDataString("/admin/reports?tab=2")}",
                navigation.LastNavigatedTo,
                StringComparison.Ordinal);
            Assert.True(navigation.LastForceLoad);
        }

        [Fact]
        public void ChallengeUser_LocalReturnUrl_PreservesPathBase()
        {
            // Arrange — app hosted under a path base ("/app"). PathAndQuery keeps it;
            // NavigationManager.ToBaseRelativePath would lose it.
            var navigation = new TestNavigationManager(
                "https://host.contoso.com/app/",
                "https://host.contoso.com/app/page?x=1");

            var handler = new BlazorAuthenticationChallengeHandler(
                navigation,
                _mockAuthStateProvider,
                _configuration);

            // Act
            handler.ChallengeUser(new ClaimsPrincipal(new CaseSensitiveClaimsIdentity()));

            // Assert
            Assert.NotNull(navigation.LastNavigatedTo);
            Assert.StartsWith(
                $"/authentication/login?returnUrl={Uri.EscapeDataString("/app/page?x=1")}",
                navigation.LastNavigatedTo,
                StringComparison.Ordinal);
        }

        [Fact]
        public void ChallengeUser_ProtocolRelativePathShape_CoercedToRoot()
        {
            // Arrange — PathAndQuery of "https://host//evil.example/x" is "//evil.example/x":
            // a protocol-relative shape that a downstream Location header would follow off-origin.
            // The handler must re-check IsLocalUrl on the coerced value and fall back to "/".
            var navigation = new TestNavigationManager(
                "https://host.contoso.com/",
                "https://host.contoso.com//evil.example/x");

            var handler = new BlazorAuthenticationChallengeHandler(
                navigation,
                _mockAuthStateProvider,
                _configuration);

            // Act
            handler.ChallengeUser(new ClaimsPrincipal(new CaseSensitiveClaimsIdentity()));

            // Assert
            Assert.NotNull(navigation.LastNavigatedTo);
            Assert.StartsWith(
                $"/authentication/login?returnUrl={Uri.EscapeDataString("/")}",
                navigation.LastNavigatedTo,
                StringComparison.Ordinal);
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

        // Note: NavigationManager.Uri and NavigateTo ARE unit-testable via a concrete
        // subclass that calls Initialize() and overrides NavigateToCore (see
        // TestNavigationManager above) — the same pattern the ASP.NET Core repo uses.
        // End-to-end URL construction through real Blazor components remains covered
        // by integration tests.
    }
}

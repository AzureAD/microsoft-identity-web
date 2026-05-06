// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Web.UI.Areas.MicrosoftIdentity.Controllers;
using NSubstitute;
using Xunit;

namespace Microsoft.Identity.Web.UI.Test.Areas.MicrosoftIdentity.Controllers
{
    public class AccountControllerTests
    {
        private readonly IOptionsMonitor<MicrosoftIdentityOptions> _optionsMonitorMock;
        private readonly AccountController _accountController;

        public AccountControllerTests()
        {
            _optionsMonitorMock = Substitute.For<IOptionsMonitor<MicrosoftIdentityOptions>>();
            _optionsMonitorMock.Get(Arg.Any<string>()).Returns(new MicrosoftIdentityOptions());
            _accountController = new AccountController(_optionsMonitorMock);
            var urlHelperMock = Substitute.For<IUrlHelper>();
            urlHelperMock.IsLocalUrl(Arg.Any<string>()).Returns(true);
            urlHelperMock.Content("~/").Returns("/");
            _accountController.Url = urlHelperMock;
            _accountController.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };
        }

        [Fact]
        public void SignIn_WithLoginHintAndDomainHint_AddsToAuthProperties()
        {
            // Arrange
            string scheme = OpenIdConnectDefaults.AuthenticationScheme;
            string redirectUri = "https://localhost/redirect";
            string loginHint = "user@example.com";
            string domainHint = "contoso.com";

            // Act
            var result = _accountController.SignIn(scheme, redirectUri, loginHint, domainHint);

            // Assert
            var challengeResult = Assert.IsType<ChallengeResult>(result);
            Assert.Single(challengeResult.AuthenticationSchemes);
            Assert.Equal(scheme, challengeResult.AuthenticationSchemes[0]);

            var authProps = challengeResult.Properties;
            Assert.NotNull(authProps);
            Assert.Equal(redirectUri, authProps.RedirectUri);
            Assert.True(authProps.Parameters.ContainsKey(Constants.LoginHint));
            Assert.Equal(loginHint, authProps.Parameters[Constants.LoginHint]);
            Assert.True(authProps.Parameters.ContainsKey(Constants.DomainHint));
            Assert.Equal(domainHint, authProps.Parameters[Constants.DomainHint]);
        }

        [Fact]
        public void SignIn_WithoutLoginHintAndDomainHint_DoesNotAddToAuthProperties()
        {
            // Arrange
            string scheme = OpenIdConnectDefaults.AuthenticationScheme;
            string redirectUri = "https://localhost/redirect";

            // Act
            var result = _accountController.SignIn(scheme, redirectUri);

            // Assert
            var challengeResult = Assert.IsType<ChallengeResult>(result);
            Assert.Single(challengeResult.AuthenticationSchemes);
            Assert.Equal(scheme, challengeResult.AuthenticationSchemes[0]);

            var authProps = challengeResult.Properties;
            Assert.NotNull(authProps);
            Assert.Equal(redirectUri, authProps.RedirectUri);
            Assert.False(authProps.Parameters.ContainsKey(Constants.LoginHint));
            Assert.False(authProps.Parameters.ContainsKey(Constants.DomainHint));
        }

        [Fact]
        public void SignIn_WithInvalidRedirectUri_UsesDefaultRedirectUri()
        {
            // Arrange
            string scheme = OpenIdConnectDefaults.AuthenticationScheme;
            string redirectUri = "https://external-site.com";
            string loginHint = "user@example.com";
            string domainHint = "contoso.com";

            var urlHelperMock = Substitute.For<IUrlHelper>();
            urlHelperMock.IsLocalUrl(redirectUri).Returns(false);
            urlHelperMock.Content("~/").Returns("/");
            _accountController.Url = urlHelperMock;

            var result = _accountController.SignIn(scheme, redirectUri, loginHint, domainHint);
            var challengeResult = Assert.IsType<ChallengeResult>(result);
            var authProps = challengeResult.Properties;
            Assert.NotNull(authProps); // Ensure authProps is not null
            Assert.Equal("/", authProps.RedirectUri);
        }

        // --- Challenge action tests (F1 regression guard) -------------------------------
        //
        // The Challenge action historically assigned the raw `redirectUri` query parameter
        // to OAuthChallengeProperties.RedirectUri without any local-URL validation. Because
        // the ASP.NET Core cookie handler follows AuthenticationProperties.RedirectUri as-is
        // after sign-in, this allowed an open redirect / phishing chain post-Azure-AD auth.
        // These tests pin the IsLocalUrl gate.

        [Fact]
        public void Challenge_WithLocalRedirectUri_UsesProvidedRedirectUri()
        {
            // Arrange: IsLocalUrl returns true for local paths (default from constructor).
            string scheme = OpenIdConnectDefaults.AuthenticationScheme;
            string redirectUri = "/app/home";

            // Act
            var result = _accountController.Challenge(
                redirectUri,
                scope: "User.Read",
                loginHint: "user@example.com",
                domainHint: "contoso.com",
                claims: null!,
                policy: null!,
                scheme: scheme);

            // Assert
            var challengeResult = Assert.IsType<ChallengeResult>(result);
            Assert.Equal(redirectUri, challengeResult.Properties!.RedirectUri);
        }

        [Fact]
        public void Challenge_WithExternalRedirectUri_UsesDefaultRedirectUri()
        {
            // Arrange: simulate IUrlHelper.IsLocalUrl rejecting an external URL.
            string scheme = OpenIdConnectDefaults.AuthenticationScheme;
            string redirectUri = "https://attacker.example.com/harvest";

            var urlHelperMock = Substitute.For<IUrlHelper>();
            urlHelperMock.IsLocalUrl(redirectUri).Returns(false);
            urlHelperMock.Content("~/").Returns("/");
            _accountController.Url = urlHelperMock;

            // Act
            var result = _accountController.Challenge(
                redirectUri,
                scope: null!,
                loginHint: null!,
                domainHint: null!,
                claims: null!,
                policy: null!,
                scheme: scheme);

            // Assert
            var challengeResult = Assert.IsType<ChallengeResult>(result);
            Assert.Equal("/", challengeResult.Properties!.RedirectUri);
        }

        [Fact]
        public void Challenge_WithNullRedirectUri_UsesDefaultRedirectUri()
        {
            // Arrange
            string scheme = OpenIdConnectDefaults.AuthenticationScheme;

            var urlHelperMock = Substitute.For<IUrlHelper>();
            urlHelperMock.IsLocalUrl(Arg.Any<string>()).Returns(false);
            urlHelperMock.Content("~/").Returns("/");
            _accountController.Url = urlHelperMock;

            // Act
            var result = _accountController.Challenge(
                redirectUri: null!,
                scope: null!,
                loginHint: null!,
                domainHint: null!,
                claims: null!,
                policy: null!,
                scheme: scheme);

            // Assert
            var challengeResult = Assert.IsType<ChallengeResult>(result);
            Assert.Equal("/", challengeResult.Properties!.RedirectUri);
        }

        [Fact]
        public void Challenge_WithEmptyRedirectUri_UsesDefaultRedirectUri()
        {
            // Arrange
            string scheme = OpenIdConnectDefaults.AuthenticationScheme;

            var urlHelperMock = Substitute.For<IUrlHelper>();
            urlHelperMock.IsLocalUrl(Arg.Any<string>()).Returns(true);
            urlHelperMock.Content("~/").Returns("/");
            _accountController.Url = urlHelperMock;

            // Act
            var result = _accountController.Challenge(
                redirectUri: string.Empty,
                scope: null!,
                loginHint: null!,
                domainHint: null!,
                claims: null!,
                policy: null!,
                scheme: scheme);

            // Assert
            var challengeResult = Assert.IsType<ChallengeResult>(result);
            Assert.Equal("/", challengeResult.Properties!.RedirectUri);
        }

        [Fact]
        public void Challenge_WithProtocolRelativeRedirectUri_UsesDefaultRedirectUri()
        {
            // Arrange
            string scheme = OpenIdConnectDefaults.AuthenticationScheme;
            string redirectUri = "//attacker.example.com/";

            var urlHelperMock = Substitute.For<IUrlHelper>();
            urlHelperMock.IsLocalUrl(redirectUri).Returns(false);
            urlHelperMock.Content("~/").Returns("/");
            _accountController.Url = urlHelperMock;

            // Act
            var result = _accountController.Challenge(
                redirectUri,
                scope: null!,
                loginHint: null!,
                domainHint: null!,
                claims: null!,
                policy: null!,
                scheme: scheme);

            // Assert
            var challengeResult = Assert.IsType<ChallengeResult>(result);
            Assert.Equal("/", challengeResult.Properties!.RedirectUri);
        }

        // -----------------------------------------------------------------------------
        // Challenge action — same-origin absolute URL acceptance.
        // -----------------------------------------------------------------------------

        private void ConfigureRequest(string scheme, string host, int? port)
        {
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Scheme = scheme;
            httpContext.Request.Host = port.HasValue ? new HostString(host, port.Value) : new HostString(host);
            _accountController.ControllerContext = new ControllerContext { HttpContext = httpContext };
        }

        private void UseRealisticIsLocalUrl()
        {
            var urlHelperMock = Substitute.For<IUrlHelper>();
            urlHelperMock.IsLocalUrl(Arg.Any<string>()).Returns(ci =>
            {
                string? s = ci.Arg<string>();
                if (string.IsNullOrEmpty(s)) return false;
                if (s[0] != '/') return false;
                if (s.Length == 1) return true;
                return s[1] != '/' && s[1] != '\\';
            });
            urlHelperMock.Content("~/").Returns("/");
            _accountController.Url = urlHelperMock;
        }

        [Fact]
        public void Challenge_WithSameOriginAbsoluteRedirectUri_CoercesToPathAndQuery()
        {
            // Arrange
            ConfigureRequest("https", "myapp.example.com", port: null);
            UseRealisticIsLocalUrl();

            string redirectUri = "https://myapp.example.com/weather?city=Seattle";

            // Act
            var result = _accountController.Challenge(
                redirectUri,
                scope: "User.Read",
                loginHint: null!, domainHint: null!, claims: null!, policy: null!,
                scheme: OpenIdConnectDefaults.AuthenticationScheme);

            // Assert
            var challengeResult = Assert.IsType<ChallengeResult>(result);
            Assert.Equal("/weather?city=Seattle", challengeResult.Properties!.RedirectUri);
        }

        [Fact]
        public void Challenge_WithSameOriginAbsoluteRootRedirectUri_CoercesToSlash()
        {
            // Arrange
            ConfigureRequest("https", "myapp.example.com", port: null);
            UseRealisticIsLocalUrl();

            // Act
            var result = _accountController.Challenge(
                redirectUri: "https://myapp.example.com/",
                scope: null!, loginHint: null!, domainHint: null!, claims: null!, policy: null!,
                scheme: OpenIdConnectDefaults.AuthenticationScheme);

            // Assert
            var challengeResult = Assert.IsType<ChallengeResult>(result);
            Assert.Equal("/", challengeResult.Properties!.RedirectUri);
        }

        [Fact]
        public void Challenge_WithDifferentHostAbsoluteRedirectUri_UsesDefaultRedirectUri()
        {
            // Arrange
            ConfigureRequest("https", "myapp.example.com", port: null);
            UseRealisticIsLocalUrl();

            // Act
            var result = _accountController.Challenge(
                redirectUri: "https://attacker.example.com/harvest",
                scope: null!, loginHint: null!, domainHint: null!, claims: null!, policy: null!,
                scheme: OpenIdConnectDefaults.AuthenticationScheme);

            // Assert
            var challengeResult = Assert.IsType<ChallengeResult>(result);
            Assert.Equal("/", challengeResult.Properties!.RedirectUri);
        }

        [Fact]
        public void Challenge_WithDifferentSchemeAbsoluteRedirectUri_UsesDefaultRedirectUri()
        {
            // Arrange
            ConfigureRequest("https", "myapp.example.com", port: null);
            UseRealisticIsLocalUrl();

            // Act
            var result = _accountController.Challenge(
                redirectUri: "http://myapp.example.com/page",
                scope: null!, loginHint: null!, domainHint: null!, claims: null!, policy: null!,
                scheme: OpenIdConnectDefaults.AuthenticationScheme);

            // Assert
            var challengeResult = Assert.IsType<ChallengeResult>(result);
            Assert.Equal("/", challengeResult.Properties!.RedirectUri);
        }

        [Fact]
        public void Challenge_WithDifferentPortAbsoluteRedirectUri_UsesDefaultRedirectUri()
        {
            // Arrange
            ConfigureRequest("https", "myapp.example.com", port: null);
            UseRealisticIsLocalUrl();

            // Act
            var result = _accountController.Challenge(
                redirectUri: "https://myapp.example.com:1234/page",
                scope: null!, loginHint: null!, domainHint: null!, claims: null!, policy: null!,
                scheme: OpenIdConnectDefaults.AuthenticationScheme);

            // Assert
            var challengeResult = Assert.IsType<ChallengeResult>(result);
            Assert.Equal("/", challengeResult.Properties!.RedirectUri);
        }

        [Fact]
        public void Challenge_WithSameOriginExplicitPortAbsoluteRedirectUri_CoercesToPathAndQuery()
        {
            // Arrange
            ConfigureRequest("https", "localhost", port: 5001);
            UseRealisticIsLocalUrl();

            // Act
            var result = _accountController.Challenge(
                redirectUri: "https://localhost:5001/counter",
                scope: null!, loginHint: null!, domainHint: null!, claims: null!, policy: null!,
                scheme: OpenIdConnectDefaults.AuthenticationScheme);

            // Assert
            var challengeResult = Assert.IsType<ChallengeResult>(result);
            Assert.Equal("/counter", challengeResult.Properties!.RedirectUri);
        }

        [Fact]
        public void Challenge_WithSameOriginAbsoluteButProtocolRelativePathAndQuery_UsesDefaultRedirectUri()
        {
            // Arrange
            ConfigureRequest("https", "victim.app", port: null);
            UseRealisticIsLocalUrl();

            // Act
            var result = _accountController.Challenge(
                redirectUri: "https://victim.app//evil.com/x",
                scope: null!, loginHint: null!, domainHint: null!, claims: null!, policy: null!,
                scheme: OpenIdConnectDefaults.AuthenticationScheme);

            // Assert
            var challengeResult = Assert.IsType<ChallengeResult>(result);
            Assert.Equal("/", challengeResult.Properties!.RedirectUri);
        }

        [Fact]
        public void Challenge_WithSameOriginAbsoluteButSlashBackslashPathAndQuery_UsesDefaultRedirectUri()
        {
            // Arrange
            ConfigureRequest("https", "victim.app", port: null);
            UseRealisticIsLocalUrl();

            // Act
            var result = _accountController.Challenge(
                redirectUri: @"https://victim.app/\evil.com",
                scope: null!, loginHint: null!, domainHint: null!, claims: null!, policy: null!,
                scheme: OpenIdConnectDefaults.AuthenticationScheme);

            // Assert
            var challengeResult = Assert.IsType<ChallengeResult>(result);
            Assert.Equal("/", challengeResult.Properties!.RedirectUri);
        }
    }
}

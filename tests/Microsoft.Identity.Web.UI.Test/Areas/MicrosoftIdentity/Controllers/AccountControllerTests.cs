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
            // Arrange: "" — the `string.IsNullOrEmpty` short-circuit path (sibling of null).
            // IUrlHelper.IsLocalUrl("") actually returns false in ASP.NET Core, but the
            // short-circuit means our gate must not depend on the helper for this case.
            string scheme = OpenIdConnectDefaults.AuthenticationScheme;

            var urlHelperMock = Substitute.For<IUrlHelper>();
            urlHelperMock.IsLocalUrl(Arg.Any<string>()).Returns(true); // deliberately permissive
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
            // Arrange: "//attacker.example.com" — framework IsLocalUrl rejects this.
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
        //
        // F1 regression coverage. `MicrosoftIdentityConsentAndConditionalAccessHandler.ChallengeUser()`
        // (the canonical step-up consent flow for [AuthorizeForScopes]) passes `NavigationManager.Uri`
        // for Blazor Server and `{CreateBaseUri}/{path}` for Razor Pages / MVC — both absolute.
        // `Url.IsLocalUrl` rejects absolute URLs even for same-origin, so we must accept them via
        // an explicit origin check (scheme + host + port) and coerce to PathAndQuery. These tests pin
        // that same-origin allowance and reject any host/scheme/port mismatch.
        // -----------------------------------------------------------------------------

        private void ConfigureRequest(string scheme, string host, int? port)
        {
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Scheme = scheme;
            httpContext.Request.Host = port.HasValue ? new HostString(host, port.Value) : new HostString(host);
            _accountController.ControllerContext = new ControllerContext { HttpContext = httpContext };
        }

        /// <summary>
        /// Installs an <see cref="IUrlHelper"/> mock whose <c>IsLocalUrl</c> mirrors the real framework
        /// semantics (rejects null/empty, <c>//host</c>, <c>/\host</c>, and anything non-slash-prefixed;
        /// accepts a single-leading-slash path). This matches the behavior the <c>Challenge</c> action
        /// relies on both for the initial validation and for the re-check on a coerced same-origin path.
        /// </summary>
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
            // Arrange: Blazor Server step-up consent flow — NavigationManager.Uri is absolute.
            ConfigureRequest("https", "myapp.example.com", port: null);
            UseRealisticIsLocalUrl();

            string redirectUri = "https://myapp.example.com/weather?city=Seattle";

            // Act
            var result = _accountController.Challenge(
                redirectUri,
                scope: "User.Read",
                loginHint: null!, domainHint: null!, claims: null!, policy: null!,
                scheme: OpenIdConnectDefaults.AuthenticationScheme);

            // Assert: origin accepted, RedirectUri reduced to path+query.
            var challengeResult = Assert.IsType<ChallengeResult>(result);
            Assert.Equal("/weather?city=Seattle", challengeResult.Properties!.RedirectUri);
        }

        [Fact]
        public void Challenge_WithSameOriginAbsoluteRootRedirectUri_CoercesToSlash()
        {
            // Arrange: the "/" edge case — Uri.PathAndQuery yields "/" for a host-only absolute URL.
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
            // Arrange: request is for myapp.example.com but redirect points at attacker.example.com.
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
            // Arrange: request is HTTPS but redirect is HTTP — scheme downgrade rejected.
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
            // Arrange: request is on the default HTTPS port (443) but redirect targets port 1234.
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
            // Arrange: non-default port on both sides (localhost:5001 dev scenario).
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

        // -----------------------------------------------------------------------------
        // Same-origin absolute URL — protocol-relative-shape bypass via PathAndQuery.
        //
        // Uri.TryCreate("http://real.com//evil.com/x") yields Host="real.com" and
        // PathAndQuery="//evil.com/x". Without a re-check, the same-origin branch would emit a
        // protocol-relative URL that CookieAuthenticationHandler would faithfully return in its
        // Location header — the browser then navigates to https://evil.com/x. These tests pin the
        // IsLocalUrl re-check on the coerced value.
        // -----------------------------------------------------------------------------

        [Fact]
        public void Challenge_WithSameOriginAbsoluteButProtocolRelativePathAndQuery_UsesDefaultRedirectUri()
        {
            // Arrange
            ConfigureRequest("https", "victim.app", port: null);
            UseRealisticIsLocalUrl();

            // Act: "http://victim.app//evil.com/x" → Uri.Host="victim.app" (matches request host, but
            // request scheme is https, so same-origin returns false). We deliberately match the scheme
            // too (https) to force the same-origin branch to be taken, then check the IsLocalUrl re-check.
            var result = _accountController.Challenge(
                redirectUri: "https://victim.app//evil.com/x",
                scope: null!, loginHint: null!, domainHint: null!, claims: null!, policy: null!,
                scheme: OpenIdConnectDefaults.AuthenticationScheme);

            // Assert: the coerced PathAndQuery ("//evil.com/x") is a protocol-relative URL; re-check rejects.
            var challengeResult = Assert.IsType<ChallengeResult>(result);
            Assert.Equal("/", challengeResult.Properties!.RedirectUri);
        }

        [Fact]
        public void Challenge_WithSameOriginAbsoluteButSlashBackslashPathAndQuery_UsesDefaultRedirectUri()
        {
            // Arrange
            ConfigureRequest("https", "victim.app", port: null);
            UseRealisticIsLocalUrl();

            // Act: "https://victim.app/\evil.com" → Uri normalizes to PathAndQuery="//evil.com" — same bypass shape.
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

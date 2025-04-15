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
    }
}

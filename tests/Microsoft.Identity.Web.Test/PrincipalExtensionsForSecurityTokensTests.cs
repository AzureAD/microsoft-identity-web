// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Principal;
using Xunit;

namespace Microsoft.Identity.Web.Tests
{
    public class PrincipalExtensionsForSecurityTokensTests
    {
        [Fact]
        public void GetBootstrapToken_Returns_Null_When_ClaimsPrincipal_Is_Null()
        {
            // Arrange
            IPrincipal? claimsPrincipal = null;

            // Act
            var result = claimsPrincipal?.GetBootstrapToken();

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void GetBootstrapToken_Returns_Null_When_BootstrapContext_Is_Null()
        {
            // Arrange
            IPrincipal claimsPrincipal = new GenericPrincipal(new GenericIdentity(""), null);

            // Act
            var result = claimsPrincipal.GetBootstrapToken();

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void GetBootstrapToken_Returns_SecurityToken_When_BootstrapContext_Is_SecurityToken()
        {
            // Arrange
            var securityToken = new JwtSecurityToken();
            IPrincipal claimsPrincipal = new ClaimsPrincipal(new CaseSensitiveClaimsIdentity(new[] { new Claim(ClaimTypes.Name, "TestUser") })
            {
                BootstrapContext = securityToken
            });

            // Act
            var result = claimsPrincipal.GetBootstrapToken();

            // Assert
            Assert.Equal(securityToken, result);
        }

        [Fact]
        public void GetBootstrapToken_Returns_JsonWebToken_When_BootstrapContext_Is_String()
        {
            // Arrange
            const string jwtString = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiaWF0IjoxNTE2MjM5MDIyfQ.SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c";
            IPrincipal claimsPrincipal = new ClaimsPrincipal(new CaseSensitiveClaimsIdentity(new[] { new Claim(ClaimTypes.Name, "TestUser") })
            {
                BootstrapContext = jwtString
            });

            // Act
            var result = claimsPrincipal.GetBootstrapToken() as JsonWebToken;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(jwtString, result.EncodedToken);
        }
    }
}

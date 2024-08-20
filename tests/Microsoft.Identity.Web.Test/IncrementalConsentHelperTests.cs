// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Identity.Client;
using Microsoft.Identity.Web.Test.Common;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace Microsoft.Identity.Web.Test
{
    public class IncrementalConsentHelperTests
    {
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void ReSignInOfUser_ReturnsBoolOnErrorTest(bool isUserNullError)
        {
            MsalUiRequiredException msalUiEx;
            Assert.Throws<ArgumentNullException>(() => IncrementalConsentAndConditionalAccessHelper.CanBeSolvedByReSignInOfUser(null!));
            if (isUserNullError)
            {
                msalUiEx = new(MsalError.UserNullError, MsalError.InvalidGrantError);
                Assert.True(IncrementalConsentAndConditionalAccessHelper.CanBeSolvedByReSignInOfUser(msalUiEx));
            }
            else
            {
                msalUiEx = new(ErrorCodes.AccessDenied, "AADB2C90118: The user has forgotten their password");
                Assert.False(IncrementalConsentAndConditionalAccessHelper.CanBeSolvedByReSignInOfUser(msalUiEx));
            }
        }

        [Fact]
        public void ReSignInOfUser_ThrowsOnNullMsalUiRequiredExceptionTest()
        {
            Assert.Throws<ArgumentNullException>(() => IncrementalConsentAndConditionalAccessHelper.CanBeSolvedByReSignInOfUser(null!));
        }

        [Fact]
        public void BuildAuthenticationPropertiesTest()
        {
            string tid = "tid";
            AuthenticationProperties authProperties = IncrementalConsentAndConditionalAccessHelper.BuildAuthenticationProperties(
                null,
                new MsalUiRequiredException(MsalError.UserNullError, MsalError.InvalidGrantError),
                new ClaimsPrincipal(new CaseSensitiveClaimsIdentity(
                    new Claim[]
                    {
                        new Claim(ClaimsIdentity.DefaultNameClaimType, "cat"),
                        new Claim(tid, "catparadise")
                    }, "Basic")),
                TestConstants.B2CSignUpSignInUserFlow);
            Assert.NotNull(authProperties);
            Assert.Equal(3, authProperties.Parameters.Count);
            Assert.True(authProperties.Parameters.TryGetValue(Constants.LoginHint, out object? name));
            Assert.Equal("cat", name);
            Assert.True(authProperties.Parameters.TryGetValue(Constants.DomainHint, out object? domain));
            Assert.Equal(Constants.Organizations, domain);
            Assert.Single(authProperties.Items);
            Assert.True(authProperties.Items.TryGetValue(OidcConstants.PolicyKey, out string? b2cUserflow));
            Assert.Equal(TestConstants.B2CSignUpSignInUserFlow, b2cUserflow);
            Assert.False(authProperties.Parameters.TryGetValue(OidcConstants.AdditionalClaims, out object? t));
            Assert.Null(t);
        }

        [Fact]
        public void BuildAuthenticationProperties_ThrowsOnNullMsalUiRequiredExceptionTest()
        {
            Assert.Throws<ArgumentNullException>(() => IncrementalConsentAndConditionalAccessHelper.BuildAuthenticationProperties(
                null,
                null!,
                new ClaimsPrincipal(new CaseSensitiveClaimsIdentity(null, "Basic")),
                null));
        }
    }
}

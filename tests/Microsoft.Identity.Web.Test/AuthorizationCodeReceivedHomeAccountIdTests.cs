// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Security.Authentication;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Http;
using Microsoft.Identity.Web.Test.Common.TestHelpers;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace Microsoft.Identity.Web.Test
{
    /// <summary>
    /// Tests home account identifier handling during authorization code redemption.
    /// </summary>
    public class AuthorizationCodeReceivedHomeAccountIdTests
    {
        private const string Uid = "redeemed-uid";
        private const string Utid = "redeemed-utid";

        private static AuthorizationCodeReceivedContext CreateContext(ClaimsPrincipal? principal)
        {
            HttpContext httpContext = HttpContextUtilities.CreateHttpContext();
            var scheme = new AuthenticationScheme(
                OpenIdConnectDefaults.AuthenticationScheme,
                OpenIdConnectDefaults.AuthenticationScheme,
                typeof(OpenIdConnectHandler));

            return new AuthorizationCodeReceivedContext(httpContext, scheme, new OpenIdConnectOptions(), new AuthenticationProperties())
            {
                Principal = principal,
            };
        }

        [Fact]
        public void Hybrid_PrincipalPresent_StampsClaimsOnPrincipalAndDoesNotStash()
        {
            var context = CreateContext(new ClaimsPrincipal(new CaseSensitiveClaimsIdentity()));

            TokenAcquisitionAspNetCore.ApplyRedeemedHomeAccountId(context, Uid, Utid);

            Assert.Equal(Uid, context.Principal!.FindFirst(ClaimConstants.UniqueObjectIdentifier)?.Value);
            Assert.Equal(Utid, context.Principal!.FindFirst(ClaimConstants.UniqueTenantIdentifier)?.Value);
            Assert.False(context.HttpContext.Items.ContainsKey(Constants.HomeAccountObjectIdCacheKey));
            Assert.False(context.HttpContext.Items.ContainsKey(Constants.HomeAccountTenantIdCacheKey));
        }

        [Fact]
        public void CodeOnly_NoPrincipal_StashesOnHttpContextItems()
        {
            var context = CreateContext(principal: null);

            TokenAcquisitionAspNetCore.ApplyRedeemedHomeAccountId(context, Uid, Utid);

            Assert.Equal(Uid, context.HttpContext.Items[Constants.HomeAccountObjectIdCacheKey]);
            Assert.Equal(Utid, context.HttpContext.Items[Constants.HomeAccountTenantIdCacheKey]);
        }

        [Fact]
        public void Hybrid_ConflictingInjectedReservedClaim_Throws()
        {
            var identity = new CaseSensitiveClaimsIdentity(new[]
            {
                new Claim(ClaimConstants.UniqueObjectIdentifier, "app-injected-uid"),
            });
            var context = CreateContext(new ClaimsPrincipal(identity));

            Assert.Throws<AuthenticationException>(
                () => TokenAcquisitionAspNetCore.ApplyRedeemedHomeAccountId(context, Uid, Utid));
        }

        [Theory]
        [InlineData(null, Utid)]
        [InlineData(Uid, null)]
        [InlineData(null, null)]
        public void IncompleteRedemption_StampsNothingAndStashesNothing(string? uid, string? utid)
        {
            var context = CreateContext(new ClaimsPrincipal(new CaseSensitiveClaimsIdentity()));

            TokenAcquisitionAspNetCore.ApplyRedeemedHomeAccountId(context, uid, utid);

            Assert.False(context.Principal!.HasClaim(c => c.Type == ClaimConstants.UniqueObjectIdentifier));
            Assert.False(context.HttpContext.Items.ContainsKey(Constants.HomeAccountObjectIdCacheKey));
        }
    }
}

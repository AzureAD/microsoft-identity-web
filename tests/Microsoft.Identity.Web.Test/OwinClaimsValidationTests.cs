// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Owin;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.OAuth;
using Xunit;

namespace Microsoft.Identity.Web.Test
{
    public class OwinClaimsValidationTests
    {
        [Fact]
        public async Task MissingScopesAndRoles_AuthenticationFailsAsync()
        {
            OAuthBearerAuthenticationOptions options = CreateOptions();
            AppBuilderExtension.ConfigureClaimsValidation(options, allowWebApiToBeAuthorizedByACL: false);
            OAuthValidateIdentityContext context = CreateContext(options);

            await options.Provider.ValidateIdentity(context);

            Assert.False(context.IsValidated);
        }

        [Theory]
        [InlineData(ClaimConstants.Scope)]
        [InlineData(ClaimConstants.Scp)]
        [InlineData(ClaimConstants.Roles)]
        [InlineData(ClaimTypes.Role)]
        public async Task ScopeOrRoleClaim_AuthenticationSucceedsAsync(string claimType)
        {
            OAuthBearerAuthenticationOptions options = CreateOptions();
            AppBuilderExtension.ConfigureClaimsValidation(options, allowWebApiToBeAuthorizedByACL: false);
            OAuthValidateIdentityContext context = CreateContext(options, claimType);

            await options.Provider.ValidateIdentity(context);

            Assert.True(context.IsValidated);
        }

        [Fact]
        public async Task MissingScopesAndRoles_WithAclAuthorization_AuthenticationSucceedsAsync()
        {
            OAuthBearerAuthenticationOptions options = CreateOptions();
            AppBuilderExtension.ConfigureClaimsValidation(options, allowWebApiToBeAuthorizedByACL: true);
            OAuthValidateIdentityContext context = CreateContext(options);

            await options.Provider.ValidateIdentity(context);

            Assert.True(context.IsValidated);
        }

        private static OAuthBearerAuthenticationOptions CreateOptions()
        {
            return new OAuthBearerAuthenticationOptions
            {
                Provider = new OAuthBearerAuthenticationProvider(),
            };
        }

        private static OAuthValidateIdentityContext CreateContext(
            OAuthBearerAuthenticationOptions options,
            string? claimType = null)
        {
            ClaimsIdentity identity = claimType is null
                ? new CaseSensitiveClaimsIdentity(authenticationType: "Bearer")
                : new CaseSensitiveClaimsIdentity(new[] { new Claim(claimType, "value") }, "Bearer");
            AuthenticationTicket ticket = new(identity, new AuthenticationProperties());
            OAuthValidateIdentityContext context = new(new OwinContext(), options, ticket);
            context.Validated();
            return context;
        }

    }
}

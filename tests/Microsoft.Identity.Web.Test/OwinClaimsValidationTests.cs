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
        [InlineData(ClaimConstants.Role)]
        public async Task ScopeOrRoleClaim_AuthenticationSucceedsAsync(string claimType)
        {
            OAuthBearerAuthenticationOptions options = CreateOptions();
            AppBuilderExtension.ConfigureClaimsValidation(options, allowWebApiToBeAuthorizedByACL: false);
            OAuthValidateIdentityContext context = CreateContext(options, claimType);

            await options.Provider.ValidateIdentity(context);

            Assert.True(context.IsValidated);
        }

        [Theory]
        [InlineData(ClaimConstants.Scope, "")]
        [InlineData(ClaimConstants.Scope, " \t")]
        [InlineData(ClaimConstants.Scp, "")]
        [InlineData(ClaimConstants.Scp, " \t")]
        [InlineData(ClaimConstants.Roles, "")]
        [InlineData(ClaimConstants.Roles, " \t")]
        [InlineData(ClaimConstants.Role, "")]
        [InlineData(ClaimConstants.Role, " \t")]
        public async Task EmptyScopeOrRoleClaim_AuthenticationFailsAsync(string claimType, string claimValue)
        {
            OAuthBearerAuthenticationOptions options = CreateOptions();
            AppBuilderExtension.ConfigureClaimsValidation(options, allowWebApiToBeAuthorizedByACL: false);
            OAuthValidateIdentityContext context = CreateContext(options, claimType, claimValue);

            await options.Provider.ValidateIdentity(context);

            Assert.False(context.IsValidated);
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

        [Fact]
        public async Task ConsumerValidation_RunsOnceButCannotRescueOriginallyInvalidIdentityAsync()
        {
            int callbackCount = 0;
            OAuthBearerAuthenticationOptions options = new()
            {
                Provider = new OAuthBearerAuthenticationProvider
                {
                    OnValidateIdentity = context =>
                    {
                        callbackCount++;
                        context.Ticket.Identity.AddClaim(new Claim(ClaimConstants.Scp, "consumer-added"));
                        context.Validated();
                        return Task.CompletedTask;
                    },
                },
            };
            AppBuilderExtension.ConfigureClaimsValidation(options, allowWebApiToBeAuthorizedByACL: false);
            OAuthValidateIdentityContext context = CreateContext(options);

            await options.Provider.ValidateIdentity(context);

            Assert.Equal(1, callbackCount);
            Assert.False(context.IsValidated);
        }

        [Fact]
        public async Task ConsumerRejection_RemainsRejectedAsync()
        {
            OAuthBearerAuthenticationOptions options = new()
            {
                Provider = new OAuthBearerAuthenticationProvider
                {
                    OnValidateIdentity = context =>
                    {
                        context.Rejected();
                        return Task.CompletedTask;
                    },
                },
            };
            AppBuilderExtension.ConfigureClaimsValidation(options, allowWebApiToBeAuthorizedByACL: false);
            OAuthValidateIdentityContext context = CreateContext(options, ClaimConstants.Scp);

            await options.Provider.ValidateIdentity(context);

            Assert.False(context.IsValidated);
        }

        [Fact]
        public async Task RequestTokenAndApplyChallenge_AreForwardedAsync()
        {
            int requestTokenCount = 0;
            int applyChallengeCount = 0;
            OAuthBearerAuthenticationOptions options = new()
            {
                Provider = new OAuthBearerAuthenticationProvider
                {
                    OnRequestToken = context =>
                    {
                        requestTokenCount++;
                        return Task.CompletedTask;
                    },
                    OnApplyChallenge = context =>
                    {
                        applyChallengeCount++;
                        return Task.CompletedTask;
                    },
                },
            };
            AppBuilderExtension.ConfigureClaimsValidation(options, allowWebApiToBeAuthorizedByACL: false);

            await options.Provider.RequestToken(new OAuthRequestTokenContext(new OwinContext(), "token"));
            await options.Provider.ApplyChallenge(new OAuthChallengeContext(new OwinContext(), "Bearer"));

            Assert.Equal(1, requestTokenCount);
            Assert.Equal(1, applyChallengeCount);
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
            string? claimType = null,
            string claimValue = "value")
        {
            ClaimsIdentity identity = claimType is null
                ? new CaseSensitiveClaimsIdentity(authenticationType: "Bearer")
                : new CaseSensitiveClaimsIdentity(new[] { new Claim(claimType, claimValue) }, "Bearer");
            AuthenticationTicket ticket = new(identity, new AuthenticationProperties());
            OAuthValidateIdentityContext context = new(new OwinContext(), options, ticket);
            context.Validated();
            return context;
        }

    }
}

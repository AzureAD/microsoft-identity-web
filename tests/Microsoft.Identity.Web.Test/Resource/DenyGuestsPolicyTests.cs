// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Identity.Web.Test.Common;
using Xunit;

namespace Microsoft.Identity.Web.Test.Resource
{
    /*
     * The purpose of this test class is to verify the following scenarios:
     * V1 and V2 Azure Tokens
     * Check optional acct claim if its present
     * Application Tokens should succeed
     * Tenant Members should succeed
     * Guest Users should fail
     */
    public class DenyGuestsPolicyTests
    {
        private readonly DenyGuestsAuthorizationsHandler _handler = new();

        private const string IdentityProvider = "http://schemas.microsoft.com/identity/claims/identityprovider";
        private const string Idp = "idp";
        private const string Iss = "iss";
        private const string Acct = "acct";

        private const string GuestIdentityProvider = $"https://sts.windows.net/{TestConstants.UsGovTenantId}/";

        [Fact]
        public async Task DenyGuestsPolicy_TenantMembersWithAcctClaim_SucceedsAsync()
        {
            // Arrange
            var user = CreateClaimsPrincipal(new Claim[] { new Claim(Acct, "0") });

            var context = CreateAuhtorizationHandlerContext(user);

            // Act
            await _handler.HandleAsync(context);

            // Assert
            Assert.True(context.HasSucceeded);
        }

        [Fact]
        public async Task DenyGuestsPolicy_GuestUsersWithAcctClaim_FailsAsync()
        {
            // Arrange
            var user = CreateClaimsPrincipal(new Claim[] { new Claim(Acct, "1") });

            var context = CreateAuhtorizationHandlerContext(user);

            // Act
            await _handler.HandleAsync(context);

            // Assert
            Assert.False(context.HasSucceeded);
        }

        // V1 Application Tokens
        // Test both with and without idp transformation
        [Theory]
        [InlineData(Idp)]
        [InlineData(IdentityProvider)]
        public async Task DenyGuestPolicy_V1ApplicationTokens_SucceedsAsync(string idpClaimName)
        {
            // Arrange 
            // V1 Application Tokens includes both IDP and Issuer with the same value
            var user = CreateClaimsPrincipal(new Claim[] 
            { 
                new Claim(Iss, TestConstants.V1Issuer),
                new Claim(idpClaimName, TestConstants.V1Issuer)
            });

            var context = CreateAuhtorizationHandlerContext(user);

            // Act
            await _handler.HandleAsync(context);

            // Assert
            Assert.True(context.HasSucceeded);
        }

        // V2 Application Tokens
        [Fact]
        public async Task DenyGuestPolicy_V2ApplicationTokens_SucceedsAsync()
        {
            // Arrange 
            // V2 Application Tokens does not contain the IDP claim
            var user = CreateClaimsPrincipal(new Claim[]
            {
                new Claim(Iss, TestConstants.AadIssuer)
            });

            var context = CreateAuhtorizationHandlerContext(user);

            // Act
            await _handler.HandleAsync(context);

            // Assert
            Assert.True(context.HasSucceeded);
        }

        // Tenant Members
        // This should work with both v1 and v2 issuer
        [Theory]
        [InlineData(TestConstants.AadIssuer)]
        [InlineData(TestConstants.V1Issuer)]
        public async Task DenyGuestPolicy_TenantMemberTokens_SucceedsAsync(string issuer)
        {
            // Arrange 
            // Tenant Member Tokens does not contain the IDP claim in either version
            var user = CreateClaimsPrincipal(new Claim[]
            {
                new Claim(Iss, issuer)
            });

            var context = CreateAuhtorizationHandlerContext(user);

            // Act
            await _handler.HandleAsync(context);

            // Assert
            Assert.True(context.HasSucceeded);
        }

        // Guest Users
        // Test both token versions with and without idp transformation
        [Theory]
        [InlineData(TestConstants.AadIssuer, Idp)]
        [InlineData(TestConstants.AadIssuer, IdentityProvider)]
        [InlineData(TestConstants.V1Issuer, Idp)]
        [InlineData(TestConstants.V1Issuer, IdentityProvider)]
        public async Task DenyGuestPolicy_GuestUserTokens_FailsAsync(string issuer, string idpClaimName)
        {
            // Arrange 
            var user = CreateClaimsPrincipal(new Claim[]
            {
                new Claim(Iss, issuer),
                // The guest IDP is the same in both versions
                new Claim(idpClaimName, GuestIdentityProvider)
            });

            var context = CreateAuhtorizationHandlerContext(user);

            // Act
            await _handler.HandleAsync(context);

            // Assert
            Assert.False(context.HasSucceeded);
        }

        private static ClaimsPrincipal CreateClaimsPrincipal(IEnumerable<Claim> claims)
        {
            return new ClaimsPrincipal(new ClaimsIdentity(claims));
        }

        private AuthorizationHandlerContext CreateAuhtorizationHandlerContext(ClaimsPrincipal user)
        {
            var requirement = new DenyGuestsAuthorizationRequirement();
            return new AuthorizationHandlerContext(new[] { requirement }, user, null);
        }
    }
}

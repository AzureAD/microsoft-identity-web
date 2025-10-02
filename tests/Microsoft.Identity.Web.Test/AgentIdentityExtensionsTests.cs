// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Security.Claims;
using Microsoft.Identity.Web.Test.Common;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace Microsoft.Identity.Web.Test
{
    public class AgentIdentityExtensionsTests
    {
        private const string ParentAgentBlueprintClaim = "xms_par_app_azp";
        private const string ParentAgentBlueprintValue = "agent-blueprint-123";

        private const string SubjectFunctionClaim = "xms_sub_fct";

        [Fact]
        public void GetParentAgentBlueprint_FromClaimsPrincipal_WithClaim_ReturnsValue()
        {
            var principal = new ClaimsPrincipal(
                new CaseSensitiveClaimsIdentity(new[]
                {
                    new Claim(ParentAgentBlueprintClaim, ParentAgentBlueprintValue),
                }));

            Assert.Equal(ParentAgentBlueprintValue, principal.GetParentAgentBlueprint());
        }

        [Fact]
        public void GetParentAgentBlueprint_FromClaimsPrincipal_NoClaim_ReturnsNull()
        {
            var principal = new ClaimsPrincipal();
            Assert.Null(principal.GetParentAgentBlueprint());
        }

        [Fact]
        public void GetParentAgentBlueprint_FromClaimsIdentity_WithClaim_ReturnsValue()
        {
            var identity = new CaseSensitiveClaimsIdentity(new[]
            {
                new Claim(ParentAgentBlueprintClaim, ParentAgentBlueprintValue),
            });

            Assert.Equal(ParentAgentBlueprintValue, identity.GetParentAgentBlueprint());
        }

        [Fact]
        public void GetParentAgentBlueprint_FromClaimsIdentity_NoClaim_ReturnsNull()
        {
            var identity = new CaseSensitiveClaimsIdentity();
            Assert.Null(identity.GetParentAgentBlueprint());
        }

        [Fact]
        public void IsAgentUserIdentity_Principal_SpaceSeparated_Contains13_ReturnsTrue()
        {
            var principal = new ClaimsPrincipal(
                new CaseSensitiveClaimsIdentity(new[]
                {
                    new Claim(SubjectFunctionClaim, "1 2 13 15"),
                }));

            Assert.True(principal.IsAgentUserIdentity());
        }

        [Fact]
        public void IsAgentUserIdentity_Principal_SpaceSeparated_No13_ReturnsFalse()
        {
            var principal = new ClaimsPrincipal(
                new CaseSensitiveClaimsIdentity(new[]
                {
                    new Claim(SubjectFunctionClaim, "1 2 3 4"),
                }));

            Assert.False(principal.IsAgentUserIdentity());
        }

        [Fact]
        public void IsAgentUserIdentity_Principal_ExtraWhitespace_Contains13_ReturnsTrue()
        {
            var principal = new ClaimsPrincipal(
                new CaseSensitiveClaimsIdentity(new[]
                {
                    new Claim(SubjectFunctionClaim, "   9   13   21  "),
                }));

            Assert.True(principal.IsAgentUserIdentity());
        }

        [Fact]
        public void IsAgentUserIdentity_Principal_InvalidToken_ReturnsFalse()
        {
            var principal = new ClaimsPrincipal(
                new CaseSensitiveClaimsIdentity(new[]
                {
                    new Claim(SubjectFunctionClaim, "12 a 13"),
                }));

            Assert.False(principal.IsAgentUserIdentity());
        }

        [Fact]
        public void IsAgentUserIdentity_Principal_NoClaim_ReturnsFalse()
        {
            var principal = new ClaimsPrincipal();
            Assert.False(principal.IsAgentUserIdentity());
        }

        [Fact]
        public void IsAgentUserIdentity_Identity_SpaceSeparated_Contains13_ReturnsTrue()
        {
            var identity = new CaseSensitiveClaimsIdentity(new[]
            {
                new Claim(SubjectFunctionClaim, "7 8 13 21"),
            });

            Assert.True(identity.IsAgentUserIdentity());
        }

        [Fact]
        public void IsAgentUserIdentity_Identity_EmptyString_ReturnsFalse()
        {
            var identity = new CaseSensitiveClaimsIdentity(new[]
            {
                new Claim(SubjectFunctionClaim, "   "),
            });

            Assert.False(identity.IsAgentUserIdentity());
        }

        [Fact]
        public void IsAgentUserIdentity_Identity_NoClaim_ReturnsFalse()
        {
            var identity = new CaseSensitiveClaimsIdentity();
            Assert.False(identity.IsAgentUserIdentity());
        }
    }
}

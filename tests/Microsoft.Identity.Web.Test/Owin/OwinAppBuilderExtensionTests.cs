// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Security.Authentication;
using System.Security.Claims;
using System.Web;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Owin;
using NSubstitute;
using Xunit;

namespace Microsoft.Identity.Web.Test.Owin
{
    /// <summary>
    /// Tests for the OWIN web app sign-in adapter, covering the home account identifier (uid/utid)
    /// handling that is sourced from the token that was actually redeemed. Both the no-session
    /// (OWIN context) and session storage branches are exercised.
    /// </summary>
    public class OwinAppBuilderExtensionTests
    {
        private const string HomeObjectId = "home-object-id";
        private const string HomeTenantId = "home-tenant-id";

        [Fact]
        public void HomeAccountIdentifier_NoSession_StampsRedeemedValuesOnIdentity()
        {
            var owinContext = new OwinContext();
            var httpContext = Substitute.For<HttpContextBase>();
            httpContext.Session.Returns((HttpSessionStateBase)null!);
            var identity = new CaseSensitiveClaimsIdentity();

            // AuthorizationCodeReceived stashes the redeemed token's home account id, and
            // SecurityTokenValidated reads it back and stamps the account-identifier claims.
            AppBuilderExtension.StoreHomeAccountIdentifier(owinContext, httpContext, HomeObjectId, HomeTenantId);
            (string? homeObjectId, string? homeTenantId) = AppBuilderExtension.GetAndRemoveHomeAccountIdentifier(owinContext, httpContext);
            AppBuilderExtension.AddHomeAccountIdentifierClaims(identity, homeObjectId, homeTenantId);

            Assert.Equal(HomeObjectId, identity.FindFirst(ClaimConstants.UniqueObjectIdentifier)?.Value);
            Assert.Equal(HomeTenantId, identity.FindFirst(ClaimConstants.UniqueTenantIdentifier)?.Value);
        }

        [Fact]
        public void HomeAccountIdentifier_WithSession_StampsRedeemedValuesOnIdentity()
        {
            var owinContext = new OwinContext();
            var session = new FakeHttpSessionState();
            var httpContext = Substitute.For<HttpContextBase>();
            httpContext.Session.Returns(session);
            var identity = new CaseSensitiveClaimsIdentity();

            AppBuilderExtension.StoreHomeAccountIdentifier(owinContext, httpContext, HomeObjectId, HomeTenantId);
            (string? homeObjectId, string? homeTenantId) = AppBuilderExtension.GetAndRemoveHomeAccountIdentifier(owinContext, httpContext);
            AppBuilderExtension.AddHomeAccountIdentifierClaims(identity, homeObjectId, homeTenantId);

            Assert.Equal(HomeObjectId, identity.FindFirst(ClaimConstants.UniqueObjectIdentifier)?.Value);
            Assert.Equal(HomeTenantId, identity.FindFirst(ClaimConstants.UniqueTenantIdentifier)?.Value);
        }

        [Fact]
        public void GetAndRemoveHomeAccountIdentifier_NoSession_RemovesValuesAfterRead()
        {
            var owinContext = new OwinContext();
            var httpContext = Substitute.For<HttpContextBase>();
            httpContext.Session.Returns((HttpSessionStateBase)null!);

            AppBuilderExtension.StoreHomeAccountIdentifier(owinContext, httpContext, HomeObjectId, HomeTenantId);
            AppBuilderExtension.GetAndRemoveHomeAccountIdentifier(owinContext, httpContext);

            (string? homeObjectId, string? homeTenantId) = AppBuilderExtension.GetAndRemoveHomeAccountIdentifier(owinContext, httpContext);

            Assert.Null(homeObjectId);
            Assert.Null(homeTenantId);
        }

        [Fact]
        public void GetAndRemoveHomeAccountIdentifier_WithSession_RemovesValuesAfterRead()
        {
            var owinContext = new OwinContext();
            var session = new FakeHttpSessionState();
            var httpContext = Substitute.For<HttpContextBase>();
            httpContext.Session.Returns(session);

            AppBuilderExtension.StoreHomeAccountIdentifier(owinContext, httpContext, HomeObjectId, HomeTenantId);
            AppBuilderExtension.GetAndRemoveHomeAccountIdentifier(owinContext, httpContext);

            (string? homeObjectId, string? homeTenantId) = AppBuilderExtension.GetAndRemoveHomeAccountIdentifier(owinContext, httpContext);

            Assert.Null(homeObjectId);
            Assert.Null(homeTenantId);
        }

        [Fact]
        public void StoreHomeAccountIdentifier_WithMissingValue_StoresNothing()
        {
            var owinContext = new OwinContext();
            var httpContext = Substitute.For<HttpContextBase>();
            httpContext.Session.Returns((HttpSessionStateBase)null!);

            AppBuilderExtension.StoreHomeAccountIdentifier(owinContext, httpContext, HomeObjectId, homeTenantId: null);

            (string? homeObjectId, string? homeTenantId) = AppBuilderExtension.GetAndRemoveHomeAccountIdentifier(owinContext, httpContext);

            Assert.Null(homeObjectId);
            Assert.Null(homeTenantId);
        }

        [Fact]
        public void AddHomeAccountIdentifierClaims_B2CHomeObjectId_StampsClaims()
        {
            var identity = new CaseSensitiveClaimsIdentity();
            string b2cHomeObjectId = $"{HomeObjectId}-b2c_1_susi";

            AppBuilderExtension.AddHomeAccountIdentifierClaims(identity, b2cHomeObjectId, HomeTenantId);

            Assert.Equal(b2cHomeObjectId, identity.FindFirst(ClaimConstants.UniqueObjectIdentifier)?.Value);
            Assert.Equal(HomeTenantId, identity.FindFirst(ClaimConstants.UniqueTenantIdentifier)?.Value);
        }

        [Fact]
        public void AddHomeAccountIdentifierClaims_NullValues_AddsNothing()
        {
            var identity = new CaseSensitiveClaimsIdentity();

            AppBuilderExtension.AddHomeAccountIdentifierClaims(identity, homeObjectId: null, homeTenantId: null);

            Assert.False(identity.HasClaim(c => c.Type == ClaimConstants.UniqueObjectIdentifier));
            Assert.False(identity.HasClaim(c => c.Type == ClaimConstants.UniqueTenantIdentifier));
        }

        [Fact]
        public void AddHomeAccountIdentifierClaims_ConflictingInjectedObjectIdClaim_Throws()
        {
            var identity = new CaseSensitiveClaimsIdentity(new[] { new Claim(ClaimConstants.UniqueObjectIdentifier, "app-injected-uid") });

            Assert.Throws<AuthenticationException>(() =>
                AppBuilderExtension.AddHomeAccountIdentifierClaims(identity, HomeObjectId, HomeTenantId));
        }

        [Fact]
        public void AddHomeAccountIdentifierClaims_ConflictingInjectedTenantIdClaim_Throws()
        {
            var identity = new CaseSensitiveClaimsIdentity(new[] { new Claim(ClaimConstants.UniqueTenantIdentifier, "app-injected-utid") });

            Assert.Throws<AuthenticationException>(() =>
                AppBuilderExtension.AddHomeAccountIdentifierClaims(identity, HomeObjectId, HomeTenantId));
        }

        private sealed class FakeHttpSessionState : HttpSessionStateBase
        {
            private readonly Dictionary<string, object> _store = new Dictionary<string, object>();

            public override object this[string name]
            {
                get => _store.TryGetValue(name, out var value) ? value : null!;
                set => _store[name] = value;
            }

            public override void Add(string name, object value) => _store[name] = value;

            public override void Remove(string name) => _store.Remove(name);
        }
    }
}

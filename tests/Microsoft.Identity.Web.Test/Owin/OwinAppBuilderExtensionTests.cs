// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Security.Authentication;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Client;
using Microsoft.Identity.Web.OWIN;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Owin;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Notifications;
using Microsoft.Owin.Security.OpenIdConnect;
using NSubstitute;
using Owin;
using Xunit;

namespace Microsoft.Identity.Web.Test.Owin
{
    /// <summary>
    /// Tests for the OWIN web app sign-in adapter, covering the home account identifier (uid/utid)
    /// handling for code-only and hybrid sign-in.
    /// </summary>
    public class OwinAppBuilderExtensionTests
    {
        private const string HomeObjectId = "home-object-id";
        private const string HomeTenantId = "home-tenant-id";

        [Fact]
        public void CodeOnly_StampsCurrentRequestRedeemedValuesOnIdentity()
        {
            var owinContext = new OwinContext();
            var identity = new CaseSensitiveClaimsIdentity();

            AppBuilderExtension.ApplyRedeemedHomeAccountId(owinContext, identity: null, HomeObjectId, HomeTenantId);
            (string? homeObjectId, string? homeTenantId) = AppBuilderExtension.GetAndRemoveHomeAccountIdentifier(owinContext);
            AppBuilderExtension.AddHomeAccountIdentifierClaims(identity, homeObjectId, homeTenantId);

            Assert.Equal(HomeObjectId, identity.FindFirst(ClaimConstants.UniqueObjectIdentifier)?.Value);
            Assert.Equal(HomeTenantId, identity.FindFirst(ClaimConstants.UniqueTenantIdentifier)?.Value);
        }

        [Fact]
        public void Hybrid_StampsCurrentTicketWithoutStashingIdentifiers()
        {
            var owinContext = new OwinContext();
            var identity = new CaseSensitiveClaimsIdentity();

            AppBuilderExtension.ApplyRedeemedHomeAccountId(owinContext, identity, HomeObjectId, HomeTenantId);

            Assert.Equal(HomeObjectId, identity.FindFirst(ClaimConstants.UniqueObjectIdentifier)?.Value);
            Assert.Equal(HomeTenantId, identity.FindFirst(ClaimConstants.UniqueTenantIdentifier)?.Value);
            Assert.False(owinContext.Environment.ContainsKey(Constants.HomeAccountObjectIdCacheKey));
            Assert.False(owinContext.Environment.ContainsKey(Constants.HomeAccountTenantIdCacheKey));
        }

        [Fact]
        public void GetAndRemoveHomeAccountIdentifier_RemovesRequestValuesAfterRead()
        {
            var owinContext = new OwinContext();

            AppBuilderExtension.ApplyRedeemedHomeAccountId(owinContext, identity: null, HomeObjectId, HomeTenantId);
            AppBuilderExtension.GetAndRemoveHomeAccountIdentifier(owinContext);

            (string? homeObjectId, string? homeTenantId) = AppBuilderExtension.GetAndRemoveHomeAccountIdentifier(owinContext);

            Assert.Null(homeObjectId);
            Assert.Null(homeTenantId);
        }

        [Fact]
        public void Hybrid_AccountSwitchWithSharedSession_DoesNotInheritPreviousAccount()
        {
            var session = new FakeHttpSessionState();
            session[Constants.HomeAccountObjectIdCacheKey] = "previous-user";
            session[Constants.HomeAccountTenantIdCacheKey] = "previous-tenant";
            var httpContext = Substitute.For<HttpContextBase>();
            httpContext.Session.Returns(session);
            var firstContext = new OwinContext();
            firstContext.Set(typeof(HttpContextBase).FullName, httpContext);
            var firstIdentity = new CaseSensitiveClaimsIdentity();

            (string? firstObjectId, string? firstTenantId) = AppBuilderExtension.GetAndRemoveHomeAccountIdentifier(firstContext);
            AppBuilderExtension.AddHomeAccountIdentifierClaims(firstIdentity, firstObjectId, firstTenantId);
            AppBuilderExtension.ApplyRedeemedHomeAccountId(firstContext, firstIdentity, HomeObjectId, HomeTenantId);

            var secondContext = new OwinContext();
            secondContext.Set(typeof(HttpContextBase).FullName, httpContext);
            var secondIdentity = new CaseSensitiveClaimsIdentity();
            (string? secondObjectId, string? secondTenantId) = AppBuilderExtension.GetAndRemoveHomeAccountIdentifier(secondContext);
            AppBuilderExtension.AddHomeAccountIdentifierClaims(secondIdentity, secondObjectId, secondTenantId);
            Assert.Null(secondIdentity.FindFirst(ClaimConstants.UniqueObjectIdentifier));
            AppBuilderExtension.ApplyRedeemedHomeAccountId(secondContext, secondIdentity, "second-user", "second-tenant");

            Assert.Equal(HomeObjectId, firstIdentity.FindFirst(ClaimConstants.UniqueObjectIdentifier)?.Value);
            Assert.Equal(HomeTenantId, firstIdentity.FindFirst(ClaimConstants.UniqueTenantIdentifier)?.Value);
            Assert.Equal("second-user", secondIdentity.FindFirst(ClaimConstants.UniqueObjectIdentifier)?.Value);
            Assert.Equal("second-tenant", secondIdentity.FindFirst(ClaimConstants.UniqueTenantIdentifier)?.Value);
            Assert.Equal("previous-user", session[Constants.HomeAccountObjectIdCacheKey]);
            Assert.Equal("previous-tenant", session[Constants.HomeAccountTenantIdCacheKey]);
        }

        [Theory]
        [InlineData(null, HomeTenantId)]
        [InlineData(HomeObjectId, null)]
        [InlineData("", HomeTenantId)]
        [InlineData(HomeObjectId, "")]
        public void ApplyRedeemedHomeAccountId_WithMissingValue_StampsAndStoresNothing(string? homeObjectId, string? homeTenantId)
        {
            var owinContext = new OwinContext();
            var identity = new CaseSensitiveClaimsIdentity();

            AppBuilderExtension.ApplyRedeemedHomeAccountId(owinContext, identity, homeObjectId, homeTenantId);
            AppBuilderExtension.ApplyRedeemedHomeAccountId(owinContext, identity: null, homeObjectId, homeTenantId);

            Assert.Null(identity.FindFirst(ClaimConstants.UniqueObjectIdentifier));
            Assert.Null(identity.FindFirst(ClaimConstants.UniqueTenantIdentifier));
            Assert.False(owinContext.Environment.ContainsKey(Constants.HomeAccountObjectIdCacheKey));
            Assert.False(owinContext.Environment.ContainsKey(Constants.HomeAccountTenantIdCacheKey));
        }

        [Fact]
        public void Hybrid_ConflictingReservedClaim_Throws()
        {
            var identity = new CaseSensitiveClaimsIdentity(new[]
            {
                new Claim(ClaimConstants.UniqueObjectIdentifier, "conflicting-user"),
            });

            Assert.Throws<AuthenticationException>(() =>
                AppBuilderExtension.ApplyRedeemedHomeAccountId(new OwinContext(), identity, HomeObjectId, HomeTenantId));
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

        [Fact]
        public async Task CodeOnlyNotifications_BindRedeemedAccountAfterValidation()
        {
            using var fixture = new SignInNotificationsFixture();
            var owinContext = new OwinContext();
            var redemption = fixture.CreateRedemption(owinContext, ticket: null);

            await fixture.Options.Notifications.AuthorizationCodeReceived(redemption);

            Assert.True(redemption.HandledCodeRedemption);
            Assert.Null(redemption.AuthenticationTicket);
            var ticket = CreateTicket();
            var validation = fixture.CreateValidation(owinContext, ticket);
            await fixture.Options.Notifications.SecurityTokenValidated(validation);

            Assert.Equal($"{HomeObjectId}.{HomeTenantId}", new ClaimsPrincipal(validation.AuthenticationTicket.Identity).GetMsalAccountId());
            Assert.False(owinContext.Environment.ContainsKey(Constants.HomeAccountObjectIdCacheKey));
            Assert.False(owinContext.Environment.ContainsKey(Constants.HomeAccountTenantIdCacheKey));
        }

        [Fact]
        public async Task HybridNotifications_BindCurrentTicketAndPreserveIdentity()
        {
            using var fixture = new SignInNotificationsFixture(hybrid: true);
            var owinContext = new OwinContext();
            var ticket = CreateTicket();
            var validation = fixture.CreateValidation(owinContext, ticket);
            await fixture.Options.Notifications.SecurityTokenValidated(validation);
            var redemption = fixture.CreateRedemption(owinContext, validation.AuthenticationTicket);

            await fixture.Options.Notifications.AuthorizationCodeReceived(redemption);

            Assert.True(redemption.HandledCodeRedemption);
            Assert.Same(ticket, redemption.AuthenticationTicket);
            Assert.Same(ticket.Identity, redemption.AuthenticationTicket.Identity);
            Assert.Equal("Cookies", ticket.Identity.AuthenticationType);
            Assert.Equal("retained", ticket.Identity.FindFirst("application-claim")?.Value);
            Assert.Equal($"{HomeObjectId}.{HomeTenantId}", new ClaimsPrincipal(ticket.Identity).GetMsalAccountId());
            Assert.False(owinContext.Environment.ContainsKey(Constants.HomeAccountObjectIdCacheKey));
            Assert.False(owinContext.Environment.ContainsKey(Constants.HomeAccountTenantIdCacheKey));
        }

        [Fact]
        public async Task HybridNotifications_SharedSessionKeepsAccountsBoundToTheirOwnTickets()
        {
            using var fixture = new SignInNotificationsFixture(hybrid: true);
            fixture.Acquisition.AddAccountToCacheFromAuthorizationCodeAndGetAccountAsync(Arg.Any<AuthCodeRedemptionParameters>())
                .Returns(
                    Task.FromResult(CreateRedemptionResult(HomeObjectId, HomeTenantId)),
                    Task.FromResult(CreateRedemptionResult("second-user", "second-tenant")));
            var session = new FakeHttpSessionState();
            session[Constants.HomeAccountObjectIdCacheKey] = "stale-user";
            session[Constants.HomeAccountTenantIdCacheKey] = "stale-tenant";
            var httpContext = Substitute.For<HttpContextBase>();
            httpContext.Session.Returns(session);
            var tickets = new List<AuthenticationTicket>();

            for (int i = 0; i < 2; i++)
            {
                var owinContext = new OwinContext();
                owinContext.Set(typeof(HttpContextBase).FullName, httpContext);
                var validation = fixture.CreateValidation(owinContext, CreateTicket());
                await fixture.Options.Notifications.SecurityTokenValidated(validation);
                Assert.Null(new ClaimsPrincipal(validation.AuthenticationTicket.Identity).GetMsalAccountId());
                var redemption = fixture.CreateRedemption(owinContext, validation.AuthenticationTicket);
                await fixture.Options.Notifications.AuthorizationCodeReceived(redemption);
                Assert.True(redemption.HandledCodeRedemption);
                tickets.Add(redemption.AuthenticationTicket);
            }

            Assert.Equal($"{HomeObjectId}.{HomeTenantId}", new ClaimsPrincipal(tickets[0].Identity).GetMsalAccountId());
            Assert.Equal("second-user.second-tenant", new ClaimsPrincipal(tickets[1].Identity).GetMsalAccountId());
            Assert.Equal("stale-user", session[Constants.HomeAccountObjectIdCacheKey]);
            Assert.Equal("stale-tenant", session[Constants.HomeAccountTenantIdCacheKey]);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task RedemptionNotification_FailedAcquisitionDoesNotCompleteRedemption(bool hybrid)
        {
            using var fixture = new SignInNotificationsFixture(hybrid);
            fixture.Acquisition.AddAccountToCacheFromAuthorizationCodeAndGetAccountAsync(Arg.Any<AuthCodeRedemptionParameters>())
                .Returns(Task.FromException<AuthCodeRedemptionResult>(new MsalServiceException("invalid_grant", "Synthetic redemption failure.")));
            var owinContext = new OwinContext();
            var ticket = hybrid ? CreateTicket() : null;
            if (ticket != null)
            {
                await fixture.Options.Notifications.SecurityTokenValidated(fixture.CreateValidation(owinContext, ticket));
            }
            var redemption = fixture.CreateRedemption(owinContext, ticket);

            await Assert.ThrowsAsync<MsalServiceException>(() =>
                fixture.Options.Notifications.AuthorizationCodeReceived(redemption));

            Assert.False(redemption.HandledCodeRedemption);
            Assert.Null(redemption.TokenEndpointResponse);
            Assert.Null(ticket?.Identity.FindFirst(ClaimConstants.UniqueObjectIdentifier));
            Assert.False(owinContext.Environment.ContainsKey(Constants.HomeAccountObjectIdCacheKey));
            Assert.False(owinContext.Environment.ContainsKey(Constants.HomeAccountTenantIdCacheKey));
        }

        [Theory]
        [InlineData(ClaimConstants.UniqueObjectIdentifier)]
        [InlineData(ClaimConstants.UniqueTenantIdentifier)]
        public async Task HybridNotifications_ConflictingReservedClaimDoesNotCompleteRedemption(string claimType)
        {
            using var fixture = new SignInNotificationsFixture(hybrid: true);
            var owinContext = new OwinContext();
            var ticket = CreateTicket();
            ticket.Identity.AddClaim(new Claim(claimType, "conflicting-value"));
            await fixture.Options.Notifications.SecurityTokenValidated(fixture.CreateValidation(owinContext, ticket));
            var redemption = fixture.CreateRedemption(owinContext, ticket);

            await Assert.ThrowsAsync<AuthenticationException>(() =>
                fixture.Options.Notifications.AuthorizationCodeReceived(redemption));

            Assert.False(redemption.HandledCodeRedemption);
            Assert.Null(redemption.TokenEndpointResponse);
        }

        private static AuthenticationTicket CreateTicket() => new AuthenticationTicket(
            new CaseSensitiveClaimsIdentity(new[] { new Claim("application-claim", "retained") }, "Cookies"),
            new AuthenticationProperties());

        private static AuthCodeRedemptionResult CreateRedemptionResult(string homeObjectId, string homeTenantId) => new AuthCodeRedemptionResult(
            new AcquireTokenResult(
                "synthetic-access-token",
                DateTimeOffset.UtcNow.AddMinutes(5),
                "resource-tenant",
                "synthetic-id-token",
                new[] { "user.read" },
                Guid.Empty,
                "Bearer"),
            homeObjectId,
            homeTenantId);

        private sealed class SignInNotificationsFixture : IDisposable
        {
            private readonly ServiceProvider _provider;

            public SignInNotificationsFixture(bool hybrid = false)
            {
                Acquisition = Substitute.For<ITokenAcquisitionInternal>();
                Acquisition.AddAccountToCacheFromAuthorizationCodeAndGetAccountAsync(Arg.Any<AuthCodeRedemptionParameters>())
                    .Returns(Task.FromResult(CreateRedemptionResult(HomeObjectId, HomeTenantId)));
                var factory = new TestOwinTokenAcquirerFactory();
                factory.Services.AddSingleton(Acquisition);
                factory.Services.AddOptions<MicrosoftIdentityOptions>();
                var app = Substitute.For<IAppBuilder>();
                app.Properties.Returns(new Dictionary<string, object>());
                app.Use(Arg.Any<object>(), Arg.Any<object[]>()).Returns(app);
                OpenIdConnectAuthenticationOptions? options = null;
                app.AddMicrosoftIdentityWebApp(factory, updateOptions: configured =>
                {
                    if (hybrid)
                    {
                        configured.ResponseType = "code id_token";
                    }
                    options = configured;
                });
                Options = options!;
                _provider = (ServiceProvider)factory.Build();
            }

            public ITokenAcquisitionInternal Acquisition { get; }

            public OpenIdConnectAuthenticationOptions Options { get; }

            public AuthorizationCodeReceivedNotification CreateRedemption(IOwinContext context, AuthenticationTicket? ticket) => new AuthorizationCodeReceivedNotification(context, Options)
            {
                AuthenticationTicket = ticket,
                Code = "synthetic-code",
                ProtocolMessage = new OpenIdConnectMessage(),
                TokenEndpointRequest = new OpenIdConnectMessage(),
            };

            public SecurityTokenValidatedNotification<OpenIdConnectMessage, OpenIdConnectAuthenticationOptions> CreateValidation(IOwinContext context, AuthenticationTicket ticket) =>
                new SecurityTokenValidatedNotification<OpenIdConnectMessage, OpenIdConnectAuthenticationOptions>(context, Options)
                {
                    AuthenticationTicket = ticket,
                    ProtocolMessage = new OpenIdConnectMessage(),
                };

            public void Dispose() => _provider.Dispose();
        }

        private sealed class TestOwinTokenAcquirerFactory : OwinTokenAcquirerFactory
        {
            public TestOwinTokenAcquirerFactory()
            {
                Configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["AzureAd:Instance"] = "https://login.microsoftonline.com/",
                    ["AzureAd:TenantId"] = "00000000-0000-0000-0000-000000000001",
                    ["AzureAd:ClientId"] = "00000000-0000-0000-0000-000000000002",
                }).Build();
            }
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

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Client;
using Microsoft.Identity.Web.Hosts;
using Microsoft.Identity.Web.Test.Common.Mocks;
using Microsoft.Identity.Web.TokenCacheProviders.InMemory;
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
        public async Task CompatibilityRepair_OwinNativeUserFic_PreservesIdentityOptionsPrecedence()
        {
            // Arrange
            const string selectedName = "BlueprintB";
            string identityClientId = Guid.NewGuid().ToString();
            string applicationClientId = Guid.NewGuid().ToString();
            string agentId = Guid.NewGuid().ToString();
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddHttpClient();
            services.AddAgentIdentities();
            services.AddInMemoryTokenCaches();
            services.AddSingleton<ITokenAcquisitionHost, OwinTokenAcquisitionHost>();
            var http = new MockHttpClientFactory();
            services.AddSingleton<IMsalHttpClientFactory>(http);
            services.Configure<MicrosoftIdentityOptions>(selectedName, options =>
            {
                options.Instance = "https://login.microsoftonline.com/";
                options.TenantId = "10c419d4-4a50-45b2-aa4e-919fb84df24f";
                options.ClientId = identityClientId;
                options.ClientSecret = "test-only-identity-secret";
            });
            int configurations = 0;
            int postConfigurations = 0;
            services.Configure<MicrosoftIdentityApplicationOptions>(selectedName, options =>
            {
                configurations++;
                options.Instance = "https://login.microsoftonline.com/";
                options.TenantId = "10c419d4-4a50-45b2-aa4e-919fb84df24f";
                options.ClientId = applicationClientId;
                options.Name = "configured-owin-blueprint";
                options.ClientCredentials = new[] { new CredentialDescription
                {
                    SourceType = CredentialSource.ClientSecret,
                    ClientSecret = "test-only-application-secret",
                }};
            });
            services.PostConfigure<MicrosoftIdentityApplicationOptions>(selectedName, _ => postConfigurations++);
            services.AddOptions<MicrosoftIdentityApplicationOptions>(selectedName).Validate(
                options => !string.IsNullOrEmpty(options.ClientId) && options.Name == "configured-owin-blueprint",
                "A completed OWIN blueprint is required.");
            var provider = services.BuildServiceProvider();
            Assert.IsType<OwinTokenAcquisitionHost>(provider.GetRequiredService<ITokenAcquisitionHost>());
            var acquisition = (TokenAcquisition)provider.GetRequiredService<ITokenAcquisition>();
            var blueprint = CreateAgentTokenHandler("owin-blueprint-assertion");
            blueprint.ExpectedPostData = new Dictionary<string, string>
            {
                ["client_id"] = identityClientId,
                ["client_secret"] = "test-only-identity-secret",
                ["fmi_path"] = agentId,
            };
            http.AddMockHandler(blueprint);
            var instance = CreateAgentTokenHandler("owin-instance-token");
            instance.ExpectedPostData = new Dictionary<string, string>
            {
                ["client_id"] = agentId,
                ["client_assertion"] = "owin-blueprint-assertion",
            };
            http.AddMockHandler(instance);
            var userToken = CreateAgentTokenHandler("owin-user-token");
            userToken.ExpectedPostData = new Dictionary<string, string> { ["client_id"] = agentId };
            string userId = Guid.NewGuid().ToString();
            string clientInfo = Base64UrlEncoder.Encode("{\"uid\":\"" + userId + "\",\"utid\":\"test-only-home-tenant\"}");
            string idToken = MockHttpCreator.CreateIdToken(userId, "owin-unit@contoso.com");
            userToken.ResponseMessage = MockHttpCreator.CreateSuccessResponseMessage(
                "{\"token_type\":\"Bearer\",\"expires_in\":3600," +
                "\"scope\":\"https://graph.microsoft.com/.default openid profile offline_access\"," +
                "\"access_token\":\"owin-user-token\",\"refresh_token\":\"test-only-refresh-token\"," +
                "\"client_info\":\"" + clientInfo + "\",\"id_token\":\"" + idToken + "\"}");
            http.AddMockHandler(userToken);
            var headerOptions = new AuthorizationHeaderProviderOptions().WithAgentUserIdentity(agentId, "owin-unit@contoso.com");
            var tokenOptions = new TokenAcquisitionOptions
            {
                AuthenticationOptionsName = "StaleParent",
                ExtraParameters = headerOptions.AcquireTokenOptions.ExtraParameters,
            };
            tokenOptions.ExtraParameters!.Remove(Constants.MicrosoftIdentityOptionsParameter);
            var principal = new ClaimsPrincipal(new CaseSensitiveClaimsIdentity("Bearer"));

            // Act
            var result = await acquisition.GetAuthenticationResultForUserAsync(
                new[] { "https://graph.microsoft.com/.default" }, authenticationScheme: selectedName,
                user: principal, tokenAcquisitionOptions: tokenOptions);

            // Assert
            Assert.Equal("owin-user-token", result.AccessToken);
            Assert.Equal(identityClientId, blueprint.ActualRequestPostData["client_id"]);
            Assert.Equal("test-only-identity-secret", blueprint.ActualRequestPostData["client_secret"]);
            string partition = identityClientId.ToLowerInvariant();
            string wrongPartition = applicationClientId.ToLowerInvariant();
            Assert.Contains(acquisition._applicationsByAuthorityClientId.Keys, key => key.Contains(":agent-blueprint:" + partition));
            Assert.DoesNotContain(acquisition._applicationsByAuthorityClientId.Keys, key => key.Contains(":agent-blueprint:" + wrongPartition));
            Assert.Equal("StaleParent", tokenOptions.AuthenticationOptionsName);
            Assert.Equal(1, configurations);
            Assert.Equal(1, postConfigurations);
            http.Dispose();
            await provider.DisposeAsync();
        }

        private static MockHttpMessageHandler CreateAgentTokenHandler(string accessToken) => new()
        {
            ExpectedMethod = HttpMethod.Post,
            ResponseMessage = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"token_type\":\"Bearer\",\"expires_in\":3600,\"access_token\":\"" + accessToken + "\"}"),
            },
        };

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

        [Theory]
        [InlineData(null, "value")]
        [InlineData(ClaimConstants.Scp, "")]
        [InlineData(ClaimConstants.Roles, " \t")]
        public async Task ConsumerValidation_DoesNotRunForInvalidIdentityAsync(
            string? claimType,
            string claimValue)
        {
            int callbackCount = 0;
            OAuthBearerAuthenticationOptions options = new()
            {
                Provider = new OAuthBearerAuthenticationProvider
                {
                    OnValidateIdentity = context =>
                    {
                        callbackCount++;
                        context.Validated();
                        return Task.CompletedTask;
                    },
                },
            };
            AppBuilderExtension.ConfigureClaimsValidation(options, allowWebApiToBeAuthorizedByACL: false);
            OAuthValidateIdentityContext context = CreateContext(options, claimType, claimValue);

            await options.Provider.ValidateIdentity(context);

            Assert.Equal(0, callbackCount);
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

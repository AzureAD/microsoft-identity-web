// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Identity.Web.Resource;
using Microsoft.Identity.Web.Test.Common;
using Microsoft.IdentityModel.Tokens;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Identity.Web.Test.Resource
{
    [RequiredScope("access_as_user", RequiredScopesConfigurationKey = "AzureAd:Scopes")]
    public class RequiredScopePolicyTests
    {
        private IServiceProvider? _provider;
        private const string ConfigSectionName = "AzureAd";
        private const string MultipleScopes = "access_as_user user.read";
        private const string SingleScope = "access_as_user";
        private const string UserRead = "user.read";
        private const string PolicyName = "foo";

        [Theory]
        [InlineData(PolicyName, ClaimConstants.Scp)]
        [InlineData(PolicyName, ClaimConstants.Scp, true)]
        [InlineData(PolicyName, ClaimConstants.Scope)]
        [InlineData(PolicyName, ClaimConstants.Scope, true)]
        [RequiredScope(RequiredScopesConfigurationKey = "AzureAd:Scopes")]
        public async Task VerifyUserHasAnyAcceptedScope_TestAsync(
            string policyName,
            string claimType,
            bool withConfig = false)
        {
            // Arrange
            var authorizationService = BuildAuthorizationService(
                policyName,
                SingleScope,
                withConfig);

            var user = new ClaimsPrincipal(
                new CaseSensitiveClaimsIdentity(new Claim[] { new Claim(claimType, SingleScope) }));

            // Act
            var allowed = await authorizationService.AuthorizeAsync(user, policyName);

            // Assert
            Assert.True(allowed.Succeeded);
        }

        [Theory]
        [InlineData(PolicyName, SingleScope, ClaimConstants.Scp)]
        [InlineData(PolicyName, SingleScope, ClaimConstants.Scp, true)]
        [InlineData(PolicyName, SingleScope, ClaimConstants.Scope)]
        [InlineData(PolicyName, SingleScope, ClaimConstants.Scope, true)]
        public async Task VerifyUserHasAnyAcceptedScope_OneScopeMatches_TestAsync(
            string policyName,
            string scopes,
            string claimType,
            bool withConfig = false)
        {
            // Arrange
            var authorizationService = BuildAuthorizationService(
                policyName,
                scopes,
                withConfig);

            var user = new ClaimsPrincipal(
                new CaseSensitiveClaimsIdentity(new Claim[] { new Claim(claimType, MultipleScopes) }));

            // Act
            var allowed = await authorizationService.AuthorizeAsync(user, policyName);

            // Assert
            Assert.True(allowed.Succeeded);
        }

        [Fact]
        public async Task VerifyUserHasAnyAcceptedScope_WithMismatchScopeTest_FailsAsync()
        {
            // Arrange
            var authorizationService = BuildAuthorizationService(
                PolicyName,
                SingleScope);

            var user = new ClaimsPrincipal(
                new CaseSensitiveClaimsIdentity(new Claim[] { new Claim(ClaimConstants.Scp, UserRead) }));

            // Act
            var allowed = await authorizationService.AuthorizeAsync(user, PolicyName);

            // Assert
            Assert.False(allowed.Succeeded);
        }

        [Fact]
        public async Task VerifyUserHasAnyAcceptedScope_RequiredScopesMissingAsync()
        {
            // Arrange
            var authorizationService = BuildAuthorizationService(
                PolicyName,
                null);

            var user = new ClaimsPrincipal(
                new CaseSensitiveClaimsIdentity(new Claim[] { new Claim(ClaimConstants.Scp, UserRead) }));

            // Act
            var allowed = await authorizationService.AuthorizeAsync(user, PolicyName);

            // Assert
            Assert.True(allowed.Succeeded);
        }

        [Fact]
        public async Task IncorrectPolicyName_FailsAsync()
        {
            // Arrange
            var authorizationService = BuildAuthorizationService(
                "foobar",
                SingleScope);

            var user = new ClaimsPrincipal(
                new CaseSensitiveClaimsIdentity(new Claim[] { new Claim(ClaimConstants.Scp, SingleScope) }));

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => authorizationService.AuthorizeAsync(user, PolicyName));
            Assert.Equal("No policy found: foo.", exception.Message);
        }

        private IAuthorizationService BuildAuthorizationService(
        string policy,
        string? scopes,
        bool withConfig = false)
        {
            var configAsDictionary = new Dictionary<string, string?>()
            {
                { ConfigSectionName, null },
                { $"{ConfigSectionName}:Instance", TestConstants.AadInstance },
                { $"{ConfigSectionName}:TenantId", TestConstants.TenantIdAsGuid },
                { $"{ConfigSectionName}:ClientId", TestConstants.ClientId },
                { $"{ConfigSectionName}:Scope", scopes },
            };
            var memoryConfigSource = new MemoryConfigurationSource { InitialData = configAsDictionary };

            IHostBuilder hostBuilder = Host.CreateDefaultBuilder()
                .ConfigureHostConfiguration(configBuilder =>
                {
                    configBuilder.Add(memoryConfigSource);
                    configBuilder.Build().GetSection(ConfigSectionName);
                })
            .ConfigureServices(services =>
            {
                services.AddAuthorization(options =>
                {
                    options.AddPolicy(policy, policyBuilder =>
                    {
                        if (withConfig)
                        {
                            policyBuilder.Requirements.Add(new ScopeAuthorizationRequirement() { RequiredScopesConfigurationKey = $"{ConfigSectionName}:Scope" });
                        }
                        else
                        {
                            policyBuilder.RequireScope(scopes?.Split(' ')!);
                        }
                    });
                });
                services.AddLogging();
                services.AddOptions();
                services.AddSingleton<IAuthorizationHandler, ScopeAuthorizationHandler>();
                services.AddRequiredScopeAuthorization();
            });
            _provider = hostBuilder.Build().Services;
            return _provider.GetRequiredService<IAuthorizationService>();
        }
    }
}

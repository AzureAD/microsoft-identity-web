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
using Xunit;

namespace Microsoft.Identity.Web.Test.Resource
{
    [RequiredScope("access_as_user", RequiredScopesConfigurationKey ="AzureAd:Scopes")]
    public class RequiredScopePolicyTests
    {
        private IServiceProvider _provider;
        private const string ConfigSectionName = "AzureAd";
        private const string MultipleScopes = "access_as_user user.read";
        private const string SingleScope = "access_as_user";
        private const string UserRead = "user.read";
        private const string PolicyName = "foo";
        private const string AttributePolicyName = "RequiredScope(access_as_user|)";
        private const string AttributeSectionPolicyName = "RequiredScope(access_as_user|AzureAd)";

        [Theory]
        [InlineData(PolicyName, ClaimConstants.Scp)]
        [InlineData(PolicyName, ClaimConstants.Scope)]
        [InlineData(AttributePolicyName, ClaimConstants.Scp, true)]
        [InlineData(AttributePolicyName, ClaimConstants.Scope, true)]
        [InlineData(AttributeSectionPolicyName, ClaimConstants.Scope, true)]
        public async void VerifyUserHasAnyAcceptedScope_TestAsync(
            string policyName,
            string claimType,
            bool isRequiredScopePolicy = false)
        {
            // Arrange
            var authorizationService = BuildAuthorizationService(
                policyName,
                SingleScope,
                isRequiredScopePolicy);

            var user = new ClaimsPrincipal(
                new ClaimsIdentity(new Claim[] { new Claim(claimType, SingleScope) }));

            // Act
            var allowed = await authorizationService.AuthorizeAsync(user, policyName).ConfigureAwait(false);

            // Assert
            Assert.True(allowed.Succeeded);
        }

        [Theory]
        [InlineData(PolicyName, SingleScope, ClaimConstants.Scp)]
        [InlineData(PolicyName, SingleScope, ClaimConstants.Scope)]
        [InlineData(AttributePolicyName, SingleScope, ClaimConstants.Scp, true)]
        [InlineData(AttributePolicyName, SingleScope, ClaimConstants.Scope, true)]
        [InlineData(AttributeSectionPolicyName, SingleScope, ClaimConstants.Scope, true)]
        public async void VerifyUserHasAnyAcceptedScope_OneScopeMatches_TestAsync(
            string policyName,
            string scopes,
            string claimType,
            bool isRequiredScopePolicy = false)
        {
            // Arrange
            var authorizationService = BuildAuthorizationService(
                policyName,
                scopes,
                isRequiredScopePolicy);

            var user = new ClaimsPrincipal(
                new ClaimsIdentity(new Claim[] { new Claim(claimType, MultipleScopes) }));

            // Act
            var allowed = await authorizationService.AuthorizeAsync(user, policyName).ConfigureAwait(false);

            // Assert
            Assert.True(allowed.Succeeded);
        }

        [Fact]
        public async void VerifyUserHasAnyAcceptedScope_WithMismatchScopeTest_FailsAsync()
        {
            // Arrange
            var authorizationService = BuildAuthorizationService(
                PolicyName,
                SingleScope);

            var user = new ClaimsPrincipal(
                new ClaimsIdentity(new Claim[] { new Claim(ClaimConstants.Scp, UserRead) }));

            // Act
            var allowed = await authorizationService.AuthorizeAsync(user, PolicyName).ConfigureAwait(false);

            // Assert
            Assert.False(allowed.Succeeded);
        }

        [Fact]
        public async void VerifyUserHasAnyAcceptedScope_RequiredScopesMissing_FailsAsync()
        {
            // Arrange
            var authorizationService = BuildAuthorizationService(
                PolicyName,
                null);

            var user = new ClaimsPrincipal(
                new ClaimsIdentity(new Claim[] { new Claim(ClaimConstants.Scp, UserRead) }));

            // Act
            var allowed = await authorizationService.AuthorizeAsync(user, PolicyName).ConfigureAwait(false);

            // Assert
            Assert.False(allowed.Succeeded);
        }

        [Fact]
        public async void IncorrectPolicyName_FailsAsync()
        {
            // Arrange
            var authorizationService = BuildAuthorizationService(
                "foobar",
                SingleScope);

            var user = new ClaimsPrincipal(
                new ClaimsIdentity(new Claim[] { new Claim(ClaimConstants.Scp, SingleScope) }));

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => authorizationService.AuthorizeAsync(user, PolicyName)).ConfigureAwait(false);
            Assert.Equal("No policy found: foo.", exception.Message);
        }

        private IAuthorizationService BuildAuthorizationService(
        string policy,
        string scopes,
        bool isRequiredScopePolicy = false)
        {
            var configAsDictionary = new Dictionary<string, string>()
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
                        policyBuilder.RequireScope(scopes?.Split(' '));
                    });
                });
                services.AddLogging();
                services.AddOptions();
                services.AddSingleton<IAuthorizationHandler, HasScopeHandler>();
                if (isRequiredScopePolicy)
                {
                    services.AddSingleton<IAuthorizationPolicyProvider, RequiredScopePolicyProvider>();
                }
            });
            _provider = hostBuilder.Build().Services;
            return _provider.GetRequiredService<IAuthorizationService>();
        }
    }
}

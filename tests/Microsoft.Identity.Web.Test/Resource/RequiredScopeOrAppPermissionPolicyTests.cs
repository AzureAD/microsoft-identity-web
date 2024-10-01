// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Identity.Web.Resource;
using Microsoft.Identity.Web.Test.Common;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace Microsoft.Identity.Web.Test.Resource
{
    [RequiredScopeOrAppPermission(new string[] { "access_as_user" }, new string[] { "access_as_app" })]
    public class RequiredScopeOrAppPermissionPolicyTests
    {
        private IServiceProvider? _provider;
        private const string ConfigSectionName = "AzureAd";
        private const string MultipleAppPermissions = "access_as_app_read_only access_as_app";
        private const string AppPermission = "access_as_app";
        private const string Scope = "user.read";
        private const string PolicyName = "foo";

        [Theory]
        [InlineData(ClaimConstants.Role)]
        [InlineData(ClaimConstants.Role, true)]
        [InlineData(ClaimConstants.Roles)]
        [InlineData(ClaimConstants.Roles, true)]
        [RequiredScopeOrAppPermission(RequiredAppPermissionsConfigurationKey = "AzureAd:AppPermission")]
        public async Task VerifyAppHasAnyAcceptedAppPermission_TestAsync(
            string claimType,
            bool withConfig = false)
        {
            // Arrange
            var authorizationService = BuildAuthorizationService(
                PolicyName,
                AppPermission,
                null,
                withConfig);

            var user = new ClaimsPrincipal(
                new CaseSensitiveClaimsIdentity(new Claim[] { new Claim(claimType, AppPermission) }));

            // Act
            var allowed = await authorizationService.AuthorizeAsync(user, PolicyName);

            // Assert
            Assert.True(allowed.Succeeded);
        }

        [Theory]
        [InlineData(ClaimConstants.Role)]
        [InlineData(ClaimConstants.Role, true)]
        [InlineData(ClaimConstants.Roles)]
        [InlineData(ClaimConstants.Roles, true)]
        public async Task VerifyAppHasAnyAcceptedAppPermission_OneAppPermissionMatches_TestAsync(
            string claimType,
            bool withConfig = false)
        {
            // Arrange
            var authorizationService = BuildAuthorizationService(
                PolicyName,
                AppPermission,
                null,
                withConfig);

            var user = new ClaimsPrincipal(
                new CaseSensitiveClaimsIdentity(new Claim[] { new Claim(claimType, MultipleAppPermissions) }));

            // Act
            var allowed = await authorizationService.AuthorizeAsync(user, PolicyName);

            // Assert
            Assert.True(allowed.Succeeded);
        }

        [Fact]
        public async Task VerifyAppHasAnyAcceptedAppPermission_WithMismatchAppPermissionTest_FailsAsync()
        {
            // Arrange
            var authorizationService = BuildAuthorizationService(
                PolicyName,
                AppPermission,
                null);

            var user = new ClaimsPrincipal(
                new CaseSensitiveClaimsIdentity(new Claim[] { new Claim(ClaimConstants.Role, "access_as_app2") }));

            // Act
            var allowed = await authorizationService.AuthorizeAsync(user, PolicyName);

            // Assert
            Assert.False(allowed.Succeeded);
        }

        [Fact]
        public async Task VerifyAppHasAnyAcceptedAppPermission_RequiredAppPermissionMissingAsync()
        {
            // Arrange
            var authorizationService = BuildAuthorizationService(
                PolicyName,
                null,
                null);

            var user = new ClaimsPrincipal(
                new CaseSensitiveClaimsIdentity(new Claim[] { new Claim(ClaimConstants.Role, AppPermission) }));

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
                AppPermission,
                null);

            var user = new ClaimsPrincipal(
                new CaseSensitiveClaimsIdentity(new Claim[] { new Claim(ClaimConstants.Role, AppPermission) }));

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => authorizationService.AuthorizeAsync(user, PolicyName));
            Assert.Equal("No policy found: foo.", exception.Message);
        }

        [Fact]
        public async Task VerifyAppHasAnyAcceptedScopeOrAppPermission_TestAsync()
        {
            // Arrange
            var authorizationService = BuildAuthorizationService(
                PolicyName,
                AppPermission,
                Scope);

            var user = new ClaimsPrincipal(
                new CaseSensitiveClaimsIdentity(new Claim[] { new Claim(ClaimConstants.Role, AppPermission), new Claim(ClaimConstants.Scp, Scope) }));

            // Act
            var allowed = await authorizationService.AuthorizeAsync(user, PolicyName);

            // Assert
            Assert.True(allowed.Succeeded);
        }

        private IAuthorizationService BuildAuthorizationService(
        string policy,
        string? appPermission,
        string? scope,
        bool withConfig = false)
        {
            var configAsDictionary = new Dictionary<string, string?>()
            {
                { ConfigSectionName, null },
                { $"{ConfigSectionName}:Instance", TestConstants.AadInstance },
                { $"{ConfigSectionName}:TenantId", TestConstants.TenantIdAsGuid },
                { $"{ConfigSectionName}:ClientId", TestConstants.ClientId },
                { $"{ConfigSectionName}:AppPermission", appPermission },
                { $"{ConfigSectionName}:Scope", scope },
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
                            policyBuilder.Requirements.Add(new ScopeOrAppPermissionAuthorizationRequirement() { RequiredAppPermissionsConfigurationKey = $"{ConfigSectionName}:AppPermission" });
                        }
                        else
                        {
                            // Considering that the scopes and app permissions are not null (!), in order to test
                            // for the null argument exceptions
                            policyBuilder.RequireScopeOrAppPermission(scope?.Split(' ')!, appPermission?.Split(' ')!);
                        }
                    });
                });
                services.AddLogging();
                services.AddOptions();
                services.AddSingleton<IAuthorizationHandler, ScopeOrAppPermissionAuthorizationHandler>();
                services.AddRequiredScopeOrAppPermissionAuthorization();
            });
            _provider = hostBuilder.Build().Services;
            return _provider.GetRequiredService<IAuthorizationService>();
        }
    }
}

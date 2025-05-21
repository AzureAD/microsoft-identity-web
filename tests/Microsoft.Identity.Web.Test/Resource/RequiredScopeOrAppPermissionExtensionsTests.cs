// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;

using Xunit;

namespace Microsoft.Identity.Web.Test.Resource
{
    public class RequiredScopeOrAppPermissionExtensionsTests
    {
        private const string PolicyName = "foo";
        private const string AppPermission = "access_as_app";
        private const string Scope = "user.read";

        [Fact]
        public async Task RequireScopeOrAppPermission_WithAppPermission_SucceedsAsync()
        {
            // Arrange
            var authorizationService = BuildAuthorizationService(PolicyName, null, Scope);

            var user = new ClaimsPrincipal(
                new CaseSensitiveClaimsIdentity([new Claim(ClaimConstants.Role, AppPermission)]));

            var testBuilder = new TestEndpointConventionBuilder()
                .RequireScopeOrAppPermission([], [AppPermission]);

            var convention = Assert.Single(testBuilder.Conventions);

            var endpointModel = new RouteEndpointBuilder((context) => Task.CompletedTask, RoutePatternFactory.Parse("/"), 0);
            convention(endpointModel);
            var endpoint = endpointModel.Build();

            // Act
            var allowed = await authorizationService.AuthorizeAsync(user, endpoint, PolicyName);

            // Assert
            Assert.True(allowed.Succeeded);
        }

        private static IAuthorizationService BuildAuthorizationService(string policy, string? appPermission, string? scope)
        {
            IHostBuilder hostBuilder = Host.CreateDefaultBuilder()
                .ConfigureServices(services =>
                {
                    services.AddAuthorization(options =>
                    {
                        options.AddPolicy(policy, policyBuilder =>
                        {
                            policyBuilder.RequireScopeOrAppPermission(scope?.Split(' ')!, appPermission?.Split(' ')!);
                        });
                    });
                    services.AddLogging();
                    services.AddOptions();
                    services.AddRequiredScopeOrAppPermissionAuthorization();
                });

            var provider = hostBuilder.Build().Services;
            return provider.GetRequiredService<IAuthorizationService>();
        }

        private class TestEndpointConventionBuilder : IEndpointConventionBuilder
        {
            public List<Action<EndpointBuilder>> Conventions { get; } = [];

            public void Add(Action<EndpointBuilder> convention)
            {
                Conventions.Add(convention);
            }
        }
    }
}

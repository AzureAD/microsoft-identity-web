// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
#if !FROM_GITHUB_ACTION

using System.IdentityModel.Tokens.Jwt;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Graph;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Client;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.Test.Common;
using Microsoft.Identity.Web.TokenCacheProviders.InMemory;
using Microsoft.IdentityModel.Tokens;
using static AgentApplicationsTests.ServiceCollectionExtensionsForAgentIdentitiesTests;

namespace AgentApplicationsTests
{
    [Collection(nameof(TokenAcquirerFactorySingletonProtection))]
    public class AutonomousAgentTests
    {
        const string overriddenTenantId = "10c419d4-4a50-45b2-aa4e-919fb84df24f";

        [Fact]
        public async Task AutonomousAgentBlueprintCacheRejectsUnauthorizedParentAsync()
        {
            const string blueprintName = "Blueprint";
            const string unauthorizedParentName = "OtherBlueprint";
            const string blueprintId = "aab5089d-e764-47e3-9f28-cc11c2513821";
            const string agentId = "ab18ca07-d139-4840-8b3b-4be9610c6ed5";
            const string otherBlueprintId = "80757962-12c9-4913-b362-adb8cdb612df";
            const string otherAgentId = "a89203cd-4b6b-43b5-8f20-e8c51e50d858";
            const string scope = "https://graph.microsoft.com/.default";

            // Arrange: B2 owns A2, but does not own A1.
            IServiceCollection services = new ServiceCollection();
            services.ConfigureAgentApplication(blueprintName, blueprintId, overriddenTenantId);
            services.ConfigureAgentApplication(unauthorizedParentName, otherBlueprintId, overriddenTenantId);
            using var serviceProvider = (ServiceProvider)services.ConfigureServicesForAgentIdentitiesTests();
            var headerProvider = serviceProvider.GetRequiredService<IAuthorizationHeaderProvider>();
            var blueprintOptions = new AuthorizationHeaderProviderOptions
            {
                AcquireTokenOptions = new AcquireTokenOptions { AuthenticationOptionsName = blueprintName }
            }.WithAgentIdentity(agentId);
            var unauthorizedOptions = new AuthorizationHeaderProviderOptions
            {
                AcquireTokenOptions = new AcquireTokenOptions { AuthenticationOptionsName = unauthorizedParentName }
            }.WithAgentIdentity(agentId);
            var otherBlueprintOptions = new AuthorizationHeaderProviderOptions
            {
                AcquireTokenOptions = new AcquireTokenOptions { AuthenticationOptionsName = unauthorizedParentName }
            }.WithAgentIdentity(otherAgentId);

            // Act: capture all four outcomes before asserting, retaining the same provider and cache.
            string blueprintToken = string.Empty;
            var blueprintError = await Record.ExceptionAsync(async () =>
            {
                blueprintToken = await headerProvider.CreateAuthorizationHeaderForAppAsync(scope, blueprintOptions);
            });
            var firstDenial = await Record.ExceptionAsync(
                () => headerProvider.CreateAuthorizationHeaderForAppAsync(scope, unauthorizedOptions));
            string otherBlueprintToken = string.Empty;
            var otherBlueprintError = await Record.ExceptionAsync(async () =>
            {
                otherBlueprintToken = await headerProvider.CreateAuthorizationHeaderForAppAsync(scope, otherBlueprintOptions);
            });
            var repeatedDenial = await Record.ExceptionAsync(
                () => headerProvider.CreateAuthorizationHeaderForAppAsync(scope, unauthorizedOptions));

            Assert.True(blueprintError is null, $"Step 1 (A1/B1) failed: {blueprintError}");
            Assert.True(otherBlueprintError is null, $"Step 3 (A2/B2) failed: {otherBlueprintError}");
            AssertTokenIdentity(blueprintToken, overriddenTenantId, agentId, blueprintId);
            AssertTokenIdentity(otherBlueprintToken, overriddenTenantId, otherAgentId, otherBlueprintId);
            Assert.False(string.Equals(blueprintToken, otherBlueprintToken, StringComparison.Ordinal),
                "Different agents must not receive the same token.");
            Assert.True(firstDenial is MsalServiceException && repeatedDenial is MsalServiceException,
                "A1/B2 must be rejected by Entra at steps 2 and 4. " +
                $"Step 2: {firstDenial?.GetType().Name ?? "unexpected success"}; " +
                $"step 4: {repeatedDenial?.GetType().Name ?? "unexpected success"}.");
            AssertEntraAuthenticationDenied((MsalServiceException)firstDenial!);
            AssertEntraAuthenticationDenied((MsalServiceException)repeatedDenial!);
        }

        [Theory]
        [InlineData("organizations")]
        [InlineData("10c419d4-4a50-45b2-aa4e-919fb84df24f")]
        public async Task AutonomousAgentGetsAppTokenForAgentIdentityToCallGraphAsync(string configuredTenantId)
        {
            IServiceCollection services = new ServiceCollection();
            IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection().Build();

            configuration["AzureAd:Instance"] = "https://login.microsoftonline.com/";
            configuration["AzureAd:TenantId"] = configuredTenantId; // Set to the GUID or organizations
            configuration["AzureAd:ClientId"] = "aab5089d-e764-47e3-9f28-cc11c2513821"; // Agent application.
            configuration["AzureAd:ClientCredentials:0:SourceType"] = "StoreWithDistinguishedName";
            configuration["AzureAd:ClientCredentials:0:CertificateStorePath"] = "LocalMachine/My";
            configuration["AzureAd:ClientCredentials:0:CertificateDistinguishedName"] = "CN=LabAuth.MSIDLab.com";
            //configuration["AzureAd:ExtraQueryParameters:dc"] = "ESTS-PUB-SCUS-FD000-TEST1-100";

            services.AddSingleton(configuration);
            services.AddTokenAcquisition(true);
            services.AddHttpClient();
            services.AddInMemoryTokenCaches();
            services.Configure<MicrosoftIdentityApplicationOptions>(configuration.GetSection("AzureAd"));
            services.AddAgentIdentities();
            services.AddMicrosoftGraph(); // If you want to call Microsoft Graph
            var serviceProvider = services.BuildServiceProvider();

            string agentIdentity = "ab18ca07-d139-4840-8b3b-4be9610c6ed5"; // Replace with the actual agent identity

            //// Get an authorization header and handle the call to the downstream API yoursel
            IAuthorizationHeaderProvider authorizationHeaderProvider = serviceProvider.GetService<IAuthorizationHeaderProvider>()!;
            AuthorizationHeaderProviderOptions options = new AuthorizationHeaderProviderOptions().WithAgentIdentity(agentIdentity);
            if (configuredTenantId == "organizations")
            {
                options.AcquireTokenOptions.Tenant = overriddenTenantId;
            }

            //// Request user tokens in autonomous agents.
            string authorizationHeaderWithAppToken = await authorizationHeaderProvider.CreateAuthorizationHeaderForAppAsync("https://graph.microsoft.com/.default", options);

            // Extract token from authorization header and validate claims using extension methods
            string token = authorizationHeaderWithAppToken.Substring("Bearer ".Length);
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);
            var claimsIdentity = new CaseSensitiveClaimsIdentity(jwtToken.Claims);

            // Verify the token does not represent an agent user identity using the extension method
            Assert.False(claimsIdentity.IsAgentUserIdentity());

            // Verify we can retrieve the parent agent blueprint if present
            string? parentBlueprint = claimsIdentity.GetParentAgentBlueprint();
            string agentApplication = configuration["AzureAd:ClientId"]!;
            Assert.Equal(agentApplication, parentBlueprint);

            //// If you want to call Microsoft Graph, just inject and use the Microsoft Graph SDK with the agent identity.
            GraphServiceClient graphServiceClient = serviceProvider.GetRequiredService<GraphServiceClient>();
            var apps = await graphServiceClient.Applications.GetAsync(r => r.Options.WithAuthenticationOptions(options =>
            {
                options.WithAgentIdentity(agentIdentity);
                options.RequestAppToken = true;
                options.AcquireTokenOptions.Tenant = configuredTenantId == "organizations" ? overriddenTenantId : null;
            }));
            Assert.NotNull(apps);

            //// If you want to call downstream APIs letting IdWeb handle authentication.
            //IDownstreamApi downstream = serviceProvider.GetService<IDownstreamApi>()!;
            //string? response = await downstream.GetForAppAsync<string>("api", options => options.WithAgentIdentity("your-agent-identity-here"));
            //response = await downstream.GetForUserAsync<string>("api", options => options.WithAgentIdentity("your-agent-identity-here"));


            Assert.NotNull(authorizationHeaderWithAppToken);
        }
    }
}
#endif // !FROM_GITHUB_ACTION

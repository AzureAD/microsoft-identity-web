// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
#if !FROM_GITHUB_ACTION

using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Graph;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.TokenCacheProviders.Distributed;
using Microsoft.Identity.Web.TokenCacheProviders.InMemory;
using Microsoft.IdentityModel.Tokens;

namespace AgentApplicationsTests
{
    public class AutonomousAgentTests
    {
        [Theory]
        [InlineData("organizations")]
        [InlineData("31a58c3b-ae9c-4448-9e8f-e9e143e800df")]
        public async Task AutonomousAgentGetsAppTokenForAgentIdentityToCallGraphAsync(string configuredTenantId)
        {
            IServiceCollection services = new ServiceCollection();
            IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection().Build();

            string overriddenTenantId = "31a58c3b-ae9c-4448-9e8f-e9e143e800df";
            
            configuration["AzureAd:Instance"] = "https://login.microsoftonline.com/";
            configuration["AzureAd:TenantId"] = configuredTenantId; // Set to common or organizations
            configuration["AzureAd:ClientId"] = "d05619c9-dbf2-4e60-95fd-cc75dd0db451"; // Agent application.
            configuration["AzureAd:ClientCredentials:0:SourceType"] = "StoreWithDistinguishedName";
            configuration["AzureAd:ClientCredentials:0:CertificateStorePath"] = "LocalMachine/My";
            configuration["AzureAd:ClientCredentials:0:CertificateDistinguishedName"] = "CN=LabAuth.MSIDLab.com";

            services.AddSingleton(configuration);
            services.AddTokenAcquisition(true);
            services.AddHttpClient();
            services.AddInMemoryTokenCaches();
            services.Configure<MicrosoftIdentityApplicationOptions>(configuration.GetSection("AzureAd"));
            services.AddAgentIdentities();
            services.AddMicrosoftGraph();
            var serviceProvider = services.BuildServiceProvider();

            string agentIdentity = "edbfbbe7-d240-40dd-aee2-435201dbaa9c";

            // Get an authorization header with tenant override
            IAuthorizationHeaderProvider authorizationHeaderProvider = serviceProvider.GetService<IAuthorizationHeaderProvider>()!;
            AuthorizationHeaderProviderOptions options = new AuthorizationHeaderProviderOptions().WithAgentIdentity(agentIdentity);

            if (configuredTenantId == "organizations")
            {
                options.AcquireTokenOptions.Tenant = overriddenTenantId;
            }

            // Request app token with tenant override
            string authorizationHeaderWithAppToken = await authorizationHeaderProvider.CreateAuthorizationHeaderForAppAsync(
                "https://graph.microsoft.com/.default", 
                options);

            // Extract and validate the token
            string token = authorizationHeaderWithAppToken.Substring("Bearer ".Length);
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);
            var claimsIdentity = new CaseSensitiveClaimsIdentity(jwtToken.Claims);

            // Verify the token does not represent an agent user identity
            Assert.False(claimsIdentity.IsAgentUserIdentity());
            
            // Verify we can retrieve the parent agent blueprint
            string? parentBlueprint = claimsIdentity.GetParentAgentBlueprint();
            string agentApplication = configuration["AzureAd:ClientId"]!;
            Assert.Equal(agentApplication, parentBlueprint);

            // Verify the token was issued for the overridden tenant
            var tidClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "tid");
            Assert.NotNull(tidClaim);
            Assert.Equal(overriddenTenantId, tidClaim.Value);

            Assert.NotNull(authorizationHeaderWithAppToken);
        }
    }
}
#endif // !FROM_GITHUB_ACTION

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
#if !FROM_GITHUB_ACTION

using System.IdentityModel.Tokens.Jwt;
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
        [Fact]
        public async Task AutonomousAgentGetsAppTokenForAgentIdentityToCallGraphAsync()
        {
            IServiceCollection services = new ServiceCollection();
            IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection().Build();

            configuration["AzureAd:Instance"] = "https://login.microsoftonline.com/";
            //configuration["AzureAd:TenantId"] = "24e9ca3e-b997-4d54-9980-f2494ac3f784";
            configuration["AzureAd:TenantId"] = "72f988bf-86f1-41af-91ab-2d7cd011db47"; // I expect this to be overridden when acquiring tokens for agent identities.
            configuration["AzureAd:ClientId"] = "a0fc0f59-e688-458e-be27-6016b8db3308"; // Agent application.
            configuration["AzureAd:ClientCredentials:0:SourceType"] = "ClientSecret";
            configuration["AzureAd:ClientCredentials:0:ClientSecret"] = "SECRET_GOES_HERE";
            //configuration["AzureAd:ExtraQueryParameters:dc"] = "ESTS-PUB-SCUS-FD000-TEST1-100";

            services.AddSingleton(configuration);
            services.AddTokenAcquisition(true);
            services.AddHttpClient();
            services.AddInMemoryTokenCaches();
            services.Configure<MicrosoftIdentityApplicationOptions>(configuration.GetSection("AzureAd"));
            services.AddAgentIdentities();
            services.AddMicrosoftGraph(); // If you want to call Microsoft Graph
            var serviceProvider = services.BuildServiceProvider();

            string agentIdentity = "a18a37cf-3437-4e1f-a9c8-5ef0fe958659"; // Replace with the actual agent identity

            //// Get an authorization header and handle the call to the downstream API yoursel
            IAuthorizationHeaderProvider authorizationHeaderProvider = serviceProvider.GetService<IAuthorizationHeaderProvider>()!;
            string customerTenantId = "24e9ca3e-b997-4d54-9980-f2494ac3f784";
            AuthorizationHeaderProviderOptions options = new AuthorizationHeaderProviderOptions().WithAgentIdentity(agentIdentity, customerTenantId);

            options.AcquireTokenOptions.Tenant = customerTenantId;

            //// Request user tokens in autonomous agents.
            string authorizationHeaderWithAppToken = await authorizationHeaderProvider.CreateAuthorizationHeaderForAppAsync("https://graph.microsoft.com/.default", options);

            // Extract token from authorization header and validate claims using extension methods
            string token = authorizationHeaderWithAppToken.Substring("Bearer ".Length);
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);
            var claimsIdentity = new CaseSensitiveClaimsIdentity(jwtToken.Claims);

            // Verify the token does not represent an agent user identity using the extension method
            Assert.False(claimsIdentity.IsAgentUserIdentity());
            
            //// Verify we can retrieve the parent agent blueprint if present
            //string? parentBlueprint = claimsIdentity.GetParentAgentBlueprint();
            //string agentApplication = configuration["AzureAd:ClientId"]!;
            //Assert.Equal(agentApplication, parentBlueprint);

            ////// If you want to call Microsoft Graph, just inject and use the Microsoft Graph SDK with the agent identity.
            //GraphServiceClient graphServiceClient = serviceProvider.GetRequiredService<GraphServiceClient>();
            //var apps = await graphServiceClient.Applications.GetAsync(r => r.Options.WithAuthenticationOptions(options =>
            //    {
            //        options.WithAgentIdentity(agentIdentity);
            //        options.RequestAppToken = true;
            //    }));
            //Assert.NotNull(apps);

            //// If you want to call downstream APIs letting IdWeb handle authentication.
            //IDownstreamApi downstream = serviceProvider.GetService<IDownstreamApi>()!;
            //string? response = await downstream.GetForAppAsync<string>("api", options => options.WithAgentIdentity("your-agent-identity-here"));
            //response = await downstream.GetForUserAsync<string>("api", options => options.WithAgentIdentity("your-agent-identity-here"));


            Assert.NotNull(authorizationHeaderWithAppToken);
        }
    }
}
#endif // !FROM_GITHUB_ACTION

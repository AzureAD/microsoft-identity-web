// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#if !FROM_GITHUB_ACTION

using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Graph;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Web;
using Microsoft.IdentityModel.Tokens;

namespace AgentApplicationsTests
{
    public class AgentUserIdentityTests
    {
        string instance = "https://login.microsoftonline.com/";
        string tenantId = "10c419d4-4a50-45b2-aa4e-919fb84df24f";         // Replace with your tenant ID
        string agentApplication = "aab5089d-e764-47e3-9f28-cc11c2513821"; // Replace with the actual agent application client ID
        string agentIdentity = "ab18ca07-d139-4840-8b3b-4be9610c6ed5";    // Replace with the actual agent identity
        string userUpn = "agentuser1@id4slab1.onmicrosoft.com";       // Replace with the actual user upn.

        [Fact]
        public async Task AgentUserIdentityGetsTokenForGraphAsync()
        {

            IServiceCollection services = new ServiceCollection();

            // Configure the information about the agent application
            services.Configure<MicrosoftIdentityApplicationOptions>(
                options =>
                {
                    options.Instance = instance;
                    options.TenantId = tenantId; // Replace with your tenant ID
                    options.ClientId = agentApplication; // Agent application.
                    options.ClientCredentials = [
                        CertificateDescription.FromStoreWithDistinguishedName(
                            "CN=LabAuth.MSIDLab.com", StoreLocation.LocalMachine, StoreName.My)
                    ];
                });
            IServiceProvider serviceProvider = services.ConfigureServicesForAgentIdentitiesTests();

            // Get an authorization header and handle the call to the downstream API yourself
            IAuthorizationHeaderProvider authorizationHeaderProvider = serviceProvider.GetService<IAuthorizationHeaderProvider>()!;
            AuthorizationHeaderProviderOptions options = new AuthorizationHeaderProviderOptions().WithAgentUserIdentity(
                agentApplicationId: agentIdentity,
                username: userUpn
                );

            string authorizationHeaderWithUserToken = await authorizationHeaderProvider.CreateAuthorizationHeaderForUserAsync(
                scopes: ["https://graph.microsoft.com/.default"],
                options);
            Assert.NotNull(authorizationHeaderWithUserToken);

            // Extract token from authorization header and validate claims using extension methods
            string token = authorizationHeaderWithUserToken.Substring("Bearer ".Length);
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);
            var claimsIdentity = new CaseSensitiveClaimsIdentity(jwtToken.Claims);
            
            // Verify the token represents an agent user identity using the extension method
            Assert.True(claimsIdentity.IsAgentUserIdentity());
            
            // Verify we can retrieve the parent agent blueprint if present
            string? parentBlueprint = claimsIdentity.GetParentAgentBlueprint();
            Assert.Equal(agentApplication, parentBlueprint);

            // If you want to call Microsoft Graph, just inject and use the Microsoft Graph SDK with the agent identity.
            GraphServiceClient graphServiceClient = serviceProvider.GetRequiredService<GraphServiceClient>();
            var me = await graphServiceClient.Me.GetAsync(r => r.Options.WithAuthenticationOptions(options => options.WithAgentUserIdentity(agentIdentity, userUpn)));
            Assert.NotNull(me);

#if DOWNSTREAM
            // If you want to call downstream APIs letting IdWeb handle authentication.
            IDownstreamApi downstream = serviceProvider.GetService<IDownstreamApi>()!;
            string? response = await downstream.GetForAppAsync<string>("api", options => options.WithAgentIdentity("your-agent-identity-here"));
            response = await downstream.GetForUserAsync<string>("api", options => options.WithAgentIdentity("your-agent-identity-here"));
#endif
        }

        [Fact]
        public async Task AgentUserIdentityGetsTokenOtherAuthenticationSchemeAsync()
        {
            string alternateAuthenticationScheme = "AlternateScheme";
            IServiceCollection services = new ServiceCollection();

            // Configure the information about the agent application
            services.Configure<MicrosoftIdentityApplicationOptions>(alternateAuthenticationScheme,
                options =>
                {
                    options.Instance = instance;
                    options.TenantId = tenantId; // Replace with your tenant ID
                    options.ClientId = agentApplication; // Agent application.
                    options.ClientCredentials = [
                        CertificateDescription.FromStoreWithDistinguishedName(
                            "CN=LabAuth.MSIDLab.com", StoreLocation.LocalMachine, StoreName.My)
                    ];
                });
            IServiceProvider serviceProvider = services.ConfigureServicesForAgentIdentitiesTests();

            // Get an authorization header and handle the call to the downstream API yourself
            IAuthorizationHeaderProvider authorizationHeaderProvider = serviceProvider.GetService<IAuthorizationHeaderProvider>()!;
            AuthorizationHeaderProviderOptions options = new AuthorizationHeaderProviderOptions().WithAgentUserIdentity(
                agentApplicationId: agentIdentity,
                username: userUpn
                );
            options.AcquireTokenOptions.AuthenticationOptionsName = alternateAuthenticationScheme;

            string authorizationHeaderWithUserToken = await authorizationHeaderProvider.CreateAuthorizationHeaderForUserAsync(
                scopes: ["https://graph.microsoft.com/.default"],
                options);
            Assert.NotNull(authorizationHeaderWithUserToken);

            // Extract token from authorization header and validate claims using extension methods
            string token = authorizationHeaderWithUserToken.Substring("Bearer ".Length);
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);
            var claimsIdentity = new CaseSensitiveClaimsIdentity(jwtToken.Claims);
            
            // Verify the token represents an agent user identity using the extension method
            Assert.True(claimsIdentity.IsAgentUserIdentity());  
        }

        [Fact]
        public async Task AgentUserIdentityGetsTokenForGraphWithTenantOverrideAsync()
        {
            IServiceCollection services = new ServiceCollection();

            // Configure the information about the agent application
            services.Configure<MicrosoftIdentityApplicationOptions>(
                options =>
                {
                    options.Instance = instance;
                    options.TenantId = "common"; // Replace with your tenant ID
                    options.ClientId = agentApplication; // Agent application.
                    options.ClientCredentials = [
                        CertificateDescription.FromStoreWithDistinguishedName(
                            "CN=LabAuth.MSIDLab.com", StoreLocation.LocalMachine, StoreName.My)
                    ];
                });
            IServiceProvider serviceProvider = services.ConfigureServicesForAgentIdentitiesTests();

            // Get an authorization header and handle the call to the downstream API yourself
            IAuthorizationHeaderProvider authorizationHeaderProvider = serviceProvider.GetService<IAuthorizationHeaderProvider>()!;
            AuthorizationHeaderProviderOptions options = new AuthorizationHeaderProviderOptions().WithAgentUserIdentity(
                agentApplicationId: agentIdentity,
                username: userUpn
                );
            options.AcquireTokenOptions.Tenant = tenantId;

            string authorizationHeaderWithUserToken = await authorizationHeaderProvider.CreateAuthorizationHeaderForUserAsync(
                scopes: ["https://graph.microsoft.com/.default"],
                options);
            Assert.NotNull(authorizationHeaderWithUserToken);

            // Extract token from authorization header and validate claims using extension methods
            string token = authorizationHeaderWithUserToken.Substring("Bearer ".Length);
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);
            var claimsIdentity = new CaseSensitiveClaimsIdentity(jwtToken.Claims);
            
            // Verify the token represents an agent user identity using the extension method
            Assert.True(claimsIdentity.IsAgentUserIdentity());
            
            // Verify we can retrieve the parent agent blueprint if present
            string? parentBlueprint = claimsIdentity.GetParentAgentBlueprint();
            Assert.Equal(agentApplication, parentBlueprint);

            // If you want to call Microsoft Graph, just inject and use the Microsoft Graph SDK with the agent identity.
            GraphServiceClient graphServiceClient = serviceProvider.GetRequiredService<GraphServiceClient>();
            var me = await graphServiceClient.Me.GetAsync(r => r.Options.WithAuthenticationOptions(options =>
            {
                options.WithAgentUserIdentity(agentIdentity, userUpn);
                options.AcquireTokenOptions.Tenant = tenantId;
            }));
            Assert.NotNull(me);

#if DOWNSTREAM
            // If you want to call downstream APIs letting IdWeb handle authentication.
            IDownstreamApi downstream = serviceProvider.GetService<IDownstreamApi>()!;
            string? response = await downstream.GetForAppAsync<string>("api", options => options.WithAgentIdentity("your-agent-identity-here"));
            response = await downstream.GetForUserAsync<string>("api", options => options.WithAgentIdentity("your-agent-identity-here"));
#endif
        }


        [Fact]
        public async Task AgentUserIdentityGetsTokenForGraphWithCacheAsync()
        {
            IServiceCollection services = new ServiceCollection();

            // Configure the information about the agent application
            services.Configure<MicrosoftIdentityApplicationOptions>(
                options =>
                {
                    options.Instance = instance;
                    options.TenantId = tenantId; // Replace with your tenant ID
                    options.ClientId = agentApplication; // Agent application.
                    options.ClientCredentials = [
                        CertificateDescription.FromStoreWithDistinguishedName(
                            "CN=LabAuth.MSIDLab.com", StoreLocation.LocalMachine, StoreName.My)
                    ];
                });
            IServiceProvider serviceProvider = services.ConfigureServicesForAgentIdentitiesTests();

            // Get an authorization header and handle the call to the downstream API yourself
            IAuthorizationHeaderProvider authorizationHeaderProvider = serviceProvider.GetService<IAuthorizationHeaderProvider>()!;
            AuthorizationHeaderProviderOptions options = new AuthorizationHeaderProviderOptions().WithAgentUserIdentity(
                agentApplicationId: agentIdentity,
                username: userUpn
                );

            ClaimsPrincipal user = new ClaimsPrincipal();
            string authorizationHeaderWithUserToken = await authorizationHeaderProvider.CreateAuthorizationHeaderForUserAsync(
                scopes: ["https://graph.microsoft.com/.default"],
                options,
                user);
            Assert.NotNull(authorizationHeaderWithUserToken);
            Assert.True(user.HasClaim(c => c.Type == "uid"));
            Assert.True(user.HasClaim(c => c.Type == "utid"));

            // Extract token from authorization header and validate claims using extension methods
            string token = authorizationHeaderWithUserToken.Substring("Bearer ".Length);
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);
            var claimsIdentity = new CaseSensitiveClaimsIdentity(jwtToken.Claims);
            
            // Verify the token represents an agent user identity using the extension method
            Assert.True(claimsIdentity.IsAgentUserIdentity());
            
            // Verify we can retrieve the parent agent blueprint if present
            string? parentBlueprint = claimsIdentity.GetParentAgentBlueprint();
            Assert.Equal(agentApplication, parentBlueprint);

            // Use the cached user
            authorizationHeaderWithUserToken = await authorizationHeaderProvider.CreateAuthorizationHeaderForUserAsync(
                scopes: ["https://graph.microsoft.com/.default"],
                options,
                user);
            Assert.NotNull(authorizationHeaderWithUserToken);

#if DOWNSTREAM
            // If you want to call downstream APIs letting IdWeb handle authentication.
            IDownstreamApi downstream = serviceProvider.GetService<IDownstreamApi>()!;
            string? response = await downstream.GetForAppAsync<string>("api", options => options.WithAgentIdentity("your-agent-identity-here"));
            response = await downstream.GetForUserAsync<string>("api", options => options.WithAgentIdentity("your-agent-identity-here"));
#endif
        }

        [Fact]
        public async Task AgentUserIdentityGetsTokenForGraphByUserIdAsync()
        {
            string userOid = "a02b9a5b-ea57-40c9-bf00-8aa631b549a";           // Replace with the actual user OID.

            IServiceCollection services = new ServiceCollection();

            // Configure the information about the agent application
            services.Configure<MicrosoftIdentityApplicationOptions>(
                options =>
                {
                    options.Instance = instance;
                    options.TenantId = tenantId;
                    options.ClientId = agentApplication; // Agent application.
                    options.ClientCredentials = [
                        CertificateDescription.FromStoreWithDistinguishedName(
                            "CN=LabAuth.MSIDLab.com", StoreLocation.LocalMachine, StoreName.My)
                    ];
                });
            IServiceProvider serviceProvider = services.ConfigureServicesForAgentIdentitiesTests();

            // Get an authorization header using user OID instead of UPN
            IAuthorizationHeaderProvider authorizationHeaderProvider = serviceProvider.GetService<IAuthorizationHeaderProvider>()!;
            AuthorizationHeaderProviderOptions options = new AuthorizationHeaderProviderOptions().WithAgentUserIdentity(
                agentApplicationId: agentIdentity,
                userId: Guid.Parse(userOid)
                );

            string authorizationHeaderWithUserToken = await authorizationHeaderProvider.CreateAuthorizationHeaderForUserAsync(
                scopes: ["https://graph.microsoft.com/.default"],
                options);
            Assert.NotNull(authorizationHeaderWithUserToken);

            // Extract token from authorization header and validate claims using extension methods
            string token = authorizationHeaderWithUserToken.Substring("Bearer ".Length);
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);
            var claimsIdentity = new CaseSensitiveClaimsIdentity(jwtToken.Claims);
            
            // Verify the token represents an agent user identity using the extension method
            Assert.True(claimsIdentity.IsAgentUserIdentity());
            
            // Verify we can retrieve the parent agent blueprint if present
            string? parentBlueprint = claimsIdentity.GetParentAgentBlueprint();
            Assert.Equal(agentApplication, parentBlueprint);

            // If you want to call Microsoft Graph, just inject and use the Microsoft Graph SDK with the agent identity.
            GraphServiceClient graphServiceClient = serviceProvider.GetRequiredService<GraphServiceClient>();
            var me = await graphServiceClient.Me.GetAsync(r => r.Options.WithAuthenticationOptions(options => options.WithAgentUserIdentity(agentIdentity, Guid.Parse(userOid))));
            Assert.NotNull(me);

#if DOWNSTREAM
            // If you want to call downstream APIs letting IdWeb handle authentication.
            IDownstreamApi downstream = serviceProvider.GetService<IDownstreamApi>()!;
            string? response = await downstream.GetForAppAsync<string>("api", options => options.WithAgentIdentity("your-agent-identity-here"));
            response = await downstream.GetForUserAsync<string>("api", options => options.WithAgentIdentity("your-agent-identity-here"));
#endif
        }
    }
}
#endif // !FROM_GITHUB_ACTION

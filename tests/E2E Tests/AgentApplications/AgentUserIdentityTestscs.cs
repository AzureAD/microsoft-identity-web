// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#if !FROM_GITHUB_ACTION

using System;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Graph;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Web;

namespace AgentApplicationsTests
{
    public class AgentUserIdentityTests
    {
        [Fact]
        public async Task AgentUserIdentityGetsTokenForGraphAsync()
        {
            string instance = "https://login.microsoftonline.com/";
            string tenantId = "31a58c3b-ae9c-4448-9e8f-e9e143e800df";         // Replace with your tenant ID
            string agentApplication = "d15884b6-a447-4dd5-a5a5-a668c49f6300"; // Replace with the actual agent application client ID
            string agentIdentity = "d84da24a-2ea2-42b8-b5ab-8637ec208024";    // Replace with the actual agent identity
            string userUpn = "aui1@msidlabtoint.onmicrosoft.com";             // Replace with the actual user upn.

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
        public async Task AgentUserIdentityGetsTokenForGraphWithTenantOverrideAsync()
        {
            string instance = "https://login.microsoftonline.com/";
            string tenantId = "31a58c3b-ae9c-4448-9e8f-e9e143e800df";         // Replace with your tenant ID
            string agentApplication = "d15884b6-a447-4dd5-a5a5-a668c49f6300"; // Replace with the actual agent application client ID
            string agentIdentity = "d84da24a-2ea2-42b8-b5ab-8637ec208024";    // Replace with the actual agent identity
            string userUpn = "aui1@msidlabtoint.onmicrosoft.com";             // Replace with the actual user upn.

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
            string instance = "https://login.microsoftonline.com/";
            string tenantId = "31a58c3b-ae9c-4448-9e8f-e9e143e800df";         // Replace with your tenant ID
            string agentApplication = "d15884b6-a447-4dd5-a5a5-a668c49f6300"; // Replace with the actual agent application client ID
            string agentIdentity = "d84da24a-2ea2-42b8-b5ab-8637ec208024";    // Replace with the actual agent identity
            string userUpn = "aui1@msidlabtoint.onmicrosoft.com";             // Replace with the actual user upn.

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
            string instance = "https://login.microsoftonline.com/";
            string tenantId = "31a58c3b-ae9c-4448-9e8f-e9e143e800df";         // Replace with your tenant ID
            string agentApplication = "d15884b6-a447-4dd5-a5a5-a668c49f6300"; // Replace with the actual agent application client ID
            string agentIdentity = "d84da24a-2ea2-42b8-b5ab-8637ec208024";    // Replace with the actual agent identity
            string userOid = "e1f76997-1b35-4aa8-8a58-a5d8f1ac4636";           // Replace with the actual user OID.

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

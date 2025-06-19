// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Graph;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Web;

namespace AgentApplications
{
    public class AutonomousAgentTests
    {
        [Fact]
        public async Task AutonmousAgentGetsAppTokenForAgentIdentityToCallGraphAsync()
        {
            // Usual configuration for a web app or web API
            TokenAcquirerFactory tokenAcquirerFactory = TokenAcquirerFactory.GetDefaultInstance();
            IServiceCollection services = tokenAcquirerFactory.Services;
            IConfiguration configuration = tokenAcquirerFactory.Configuration;
            /*
                        services.Configure<MicrosoftIdentityApplicationOptions>(options =>
                        {
                            options.Instance = "https://login.microsoftonline.com/";
                            options.TenantId = "31a58c3b-ae9c-4448-9e8f-e9e143e800df";
                            options.ClientId = "5dcf7676-5a20-4078-9f88-369f5a591f6d"; // Agent application.
                            options.ClientCredentials = [
                                new CredentialDescription
                                {
                                    SourceType = CredentialSource.SignedAssertionFromManagedIdentity
                                }
                                ];
                        });
            */
            configuration["AzureAd:Instance"] = "https://login.microsoftonline.com/";
            configuration["AzureAd:TenantId"] = "31a58c3b-ae9c-4448-9e8f-e9e143e800df";
            configuration["AzureAd:ClientId"] = "5dcf7676-5a20-4078-9f88-369f5a591f6d"; // Agent application.
            configuration["AzureAd:ClientCredentials:0:SourceType"] = "SignedAssertionFromManagedIdentity";

            services.AddAgentIdentities();
            services.AddMicrosoftGraph(); // If you want to call Microsoft Graph
            var serviceProvider = tokenAcquirerFactory.Build();


            //// If you want to call Microsoft Graph, just inject and use the Microsoft Graph SDK with the agent identity.
            //GraphServiceClient graphServiceClient = serviceProvider.GetRequiredService<GraphServiceClient>();
            //var me = await graphServiceClient.Me.GetAsync(r => r.Options.WithAuthenticationOptions(options => options.WithAgentIdentity("your-agent-identity-here")));

            //// If you want to call downstream APIs letting IdWeb handle authentication.
            //IDownstreamApi downstream = serviceProvider.GetService<IDownstreamApi>()!;
            //string? response = await downstream.GetForAppAsync<string>("api", options => options.WithAgentIdentity("your-agent-identity-here"));
            //response = await downstream.GetForUserAsync<string>("api", options => options.WithAgentIdentity("your-agent-identity-here"));

            //// Get an authorization header and handle the call to the downstream API yoursel
            IAuthorizationHeaderProvider authorizationHeaderProvider = serviceProvider.GetService<IAuthorizationHeaderProvider>()!;
            AuthorizationHeaderProviderOptions options = new AuthorizationHeaderProviderOptions().WithAgentIdentity("9410a296-e85f-4d3c-966a-657da213694d");

            //// Request user tokens in interactive agents.
            //string authorizationHeaderWithUserToken = await authorizationHeaderProvider.CreateAuthorizationHeaderForUserAsync(["https://graph.microsoft.com/.default"], options);

            // Request agent tokens
            string authorizationHeaderWithAppTokens = await authorizationHeaderProvider.CreateAuthorizationHeaderForAppAsync("https://graph.microsoft.com/.default", options);
        }
    }

}

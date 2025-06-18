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

            configuration["AzureAd:Instance"] = "https://login.microsoftonline.com/";
            configuration["AzureAd:TenantId"] = "31a58c3b-ae9c-4448-9e8f-e9e143e800df";
            configuration["AzureAd:ClientId"] = "5dcf7676-5a20-4078-9f88-369f5a591f6d"; // Agent application.
            configuration["AzureAd:ClientCredentials:0:SourceType"] = "SignedAssertionFromManagedIdentity" 

            services.AddAgentIdentities();
            services.AddMicrosoftGraph(); // If you want to call Microsoft Graph
            var serviceProvider = tokenAcquirerFactory.Build();


            // If you want to call Microsoft Graph, just inject and use the Microsoft Graph SDK with the agent identity.
            GraphServiceClient graphServiceClient = serviceProvider.GetRequiredService<GraphServiceClient>();
            var me = await graphServiceClient.Me.GetAsync(r => r.Options.WithAuthenticationOptions(options => options.WithAgentIdentity("your-agent-identity-here")));

            // If you want to call downstream APIs letting IdWeb handle authentication.
            IDownstreamApi downstream = serviceProvider.GetService<IDownstreamApi>()!;
            string? response = await downstream.GetForAppAsync<string>("api", options => options.WithAgentIdentity("your-agent-identity-here"));
            response = await downstream.GetForUserAsync<string>("api", options => options.WithAgentIdentity("your-agent-identity-here"));

            // Get an authorization header and handle the call to the downstream API yoursel
            IAuthorizationHeaderProvider authorizationHeaderProvider = serviceProvider.GetService<IAuthorizationHeaderProvider>()!;
            AuthorizationHeaderProviderOptions options = new AuthorizationHeaderProviderOptions().WithAgentIdentity("your-agent-identity-here");

            // Request user tokens in interactive agents.
            string authorizationHeaderWithUserToken = await authorizationHeaderProvider.CreateAuthorizationHeaderForUserAsync(["https://graph.microsoft.com/.default"], options);

            // Request agent tokens
            string authorizationHeaderWithAppTokens = await authorizationHeaderProvider.CreateAuthorizationHeaderForAppAsync("https://graph.microsoft.com/.default", options);
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public static class AgentIdentityExtension
    {
        /// <summary>
        /// Enable Microsoft.Identity.Web to enable agent identities.
        /// </summary>
        /// <param name="services">Service collection</param>
        /// <returns></returns>
        public static IServiceCollection AddAgentIdentities(this IServiceCollection services)
        {
            // Register the OidcFic services for agent applications to work.
            services.AddOidcFic();

            return services;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="options"></param>
        /// <param name="agentApplicationId"></param>
        /// <returns></returns>
        public static AuthorizationHeaderProviderOptions WithAgentIdentity(this AuthorizationHeaderProviderOptions options, string agentApplicationId)
        {
            // It's possible to start with no options, so we initialize it if it's null.
            if (options == null)
                options = new AuthorizationHeaderProviderOptions();

            // AcquireTokenOptions holds the information needed to acquire a token for the Agent Identity
            if (options.AcquireTokenOptions == null)
                options.AcquireTokenOptions = new AcquireTokenOptions();

            options.AcquireTokenOptions.ForAgentIdentity(agentApplicationId);

            return options;
        }

        private static AcquireTokenOptions ForAgentIdentity(this AcquireTokenOptions options, string agentApplicationId)
        {
            if (options.ExtraParameters == null)
                options.ExtraParameters = new Dictionary<string, object>();

            // Until it makes it way through Abstractions
            options.ExtraParameters["fmiPathForClientAssertion"] = agentApplicationId;
            options.ExtraParameters["MicrosoftIdentityOptions"] = new MicrosoftEntraApplicationOptions
            {
                ClientId = agentApplicationId, // Agent identity Client ID.
                ClientCredentials = [ new CredentialDescription() {
                    SourceType = CredentialSource.CustomSignedAssertion,
                    CustomSignedAssertionProviderName = "OidcIdpSignedAssertion",
                    CustomSignedAssertionProviderData = new Dictionary<string, object> {
                        { "ConfigurationSection", "" }, // Default configuration section name
                    }
                }]
            };
            return options;
        }

        /*
         * 

            // Configuration for the Agent application (usual IdWeb/MISE configuration. Can be done rogrammatically or in appsettings.json).
            services.Configure<MicrosoftIdentityApplicationOptions>(
                options =>
                {
                    options.Instance = "https://login.microsoftonline.com/";
                    options.TenantId = "31a58c3b-ae9c-4448-9e8f-e9e143e800df"; // Replace with your tenant ID
                    options.ClientId = "5dcf7676-5a20-4078-9f88-369f5a591f6d"; // Agent application.
                    options.ClientCredentials = [ new CredentialDescription() {
                        SourceType = CredentialSource.SignedAssertionFromManagedIdentity,
                    }];
                });


         */


        /*
         *          // Autonomous agent identity accessing Microsoft Graph API using the AuthorizationHeaderProvider
                    authorizationHeaderProvider.CreateAuthorizationHeaderForAgentIdentity("https://graph.microsoft.com/.default",
                        "your-agent-application-id-here");
        */

        /*
             ITokenAcquirer tokenAcquirer = tokenAcquirerFactory.GetTokenAcquirer("");
             var token = await tokenAcquirer.GetTokenForAppAsync("https://graph.microsoft.com/.default",
                 new AcquireTokenOptions().WithAgentIdentity("5dcf7676-5a20-4078-9f88-369f5a591f6d"));
        */
    }
}

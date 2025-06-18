// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Web;

namespace AgentApplications
{
    public class AutonomousAgentTests
    {
        [Fact]
        public async Task AutonmousAgentGetsAppTokenForAgentIdentityToCallGraphAsync()
        {

            TokenAcquirerFactory tokenAcquirerFactory = TokenAcquirerFactory.GetDefaultInstance();


            tokenAcquirerFactory.Services.Configure<MicrosoftIdentityApplicationOptions>(options =>
            {
                options.Instance = "https://login.microsoftonline.com/";
                options.TenantId = "31a58c3b-ae9c-4448-9e8f-e9e143e800df"; // Replace with your tenant ID
                options.ClientId = "<agent-identity>"; // Agent identity Client ID.
                options.ClientCredentials = [ new CredentialDescription() {
                    SourceType = CredentialSource.CustomSignedAssertion,
                    CustomSignedAssertionProviderName = "OidcIdpSignedAssertion",
                    CustomSignedAssertionProviderData = new Dictionary<string, object> {
                        { "ConfigurationSection", "5dcf7676-5a20-4078-9f88-369f5a591f6d-creds" }, // Replace with your configuration section name
                    }
                }];
            });

            tokenAcquirerFactory.Services.Configure<MicrosoftIdentityApplicationOptions>("5dcf7676-5a20-4078-9f88-369f5a591f6d-creds",
                options =>
                {
                    options.Instance = "https://login.microsoftonline.com/";
                    options.TenantId = "31a58c3b-ae9c-4448-9e8f-e9e143e800df"; // Replace with your tenant ID
                    options.ClientId = "5dcf7676-5a20-4078-9f88-369f5a591f6d"; // Agent application.
                    options.ClientCredentials = [ new CredentialDescription() {
                        SourceType = CredentialSource.SignedAssertionFromManagedIdentity,
                    }];
                });

            tokenAcquirerFactory.Services.AddOidcFic();
            var sp = tokenAcquirerFactory.Build();


            ITokenAcquirer tokenAcquirer = tokenAcquirerFactory.GetTokenAcquirer("");
            var token = await tokenAcquirer.GetTokenForAppAsync("https://graph.microsoft.com/.default",
                new AcquireTokenOptions().WithAgentApplicationIdentity("5dcf7676-5a20-4078-9f88-369f5a591f6d"));

/*
            IAuthorizationHeaderProvider authorizationHeaderProvider = sp.GetService<IAuthorizationHeaderProvider>()!;
            authorizationHeaderProvider.CreateAuthorizationHeaderForAgentIdentity("https://graph.microsoft.com/.default",
                "your-agent-application-id-here");


            IDownstreamApi downstream = sp.GetService<IDownstreamApi>()!;

            // Autonomous agent identity accessing Microsoft Graph API
            string authorizationHeader = await authorizationHeaderProvider.CreateAuthorizationHeaderForAppAsync(
                "https://graph.microsoft.com/.default",
                new AuthorizationHeaderProviderOptions().WithAgentApplicationIdentity("your-agent-application-identity-here"));
*/


        }

    }

    /// <summary>
    /// 
    /// </summary>
    public static class Extension
    {
        public static AuthorizationHeaderProviderOptions WithAgentApplicationIdentity(this AuthorizationHeaderProviderOptions options, string agentApplicationId)
        {
            if (options.AcquireTokenOptions == null)
                options.AcquireTokenOptions = new AcquireTokenOptions();

            if (options.AcquireTokenOptions.ExtraParameters == null)
                options.AcquireTokenOptions.ExtraParameters = new Dictionary<string, object>();

            // Until it makes it way through Abstractions
            options.AcquireTokenOptions.ExtraParameters["fmiPathForClientAssertion"] = agentApplicationId;
            return options;
        }

        public static AcquireTokenOptions WithAgentApplicationIdentity(this AcquireTokenOptions options, string agentApplicationId)
        {
            if (options.ExtraParameters == null)
                options.ExtraParameters = new Dictionary<string, object>();

            // Until it makes it way through Abstractions
            options.ExtraParameters["fmiPathForClientAssertion"] = agentApplicationId;
            return options;
        }

    }
}

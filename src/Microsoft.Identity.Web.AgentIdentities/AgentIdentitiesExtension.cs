// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Abstractions;

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// Extensions methods to enable agent identities in Microsoft.Identity.Web.
    /// </summary>
    public static class AgentIdentityExtension
    {
        /// <summary>
        /// Enable support for agent identities.
        /// </summary>
        /// <param name="services">Service collection</param>
        /// <returns>the service collection for chaining</returns>
        public static IServiceCollection AddAgentIdentities(this IServiceCollection services)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services), "Service collection cannot be null.");
            }

            // Register the OidcFic services for agent applications to work.
            services.AddOidcFic();

            return services;
        }


        /// <summary>
        /// Updates the options to acquire a token for the agent identity.
        /// </summary>
        /// <param name="options">Authorization header provider options.</param>
        /// <param name="agentApplicationId">The Agent Identity GUID.</param>
        /// <returns>The updated authorization header provider options.</returns>
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

        // TODO:make public?
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
                        { "ConfigurationSection", "AzureAd" }, // Use the default configuration section name
                        { "RequiresSignedAssertionFmiPath", "true" }, // Use the default configuration section name
                    }
                }]
            };
            options.ExtraQueryParameters = new Dictionary<string, string>
            {
                { "dc", "ESTS-PUB-SCUS-FD000-TEST1-100"} // For the moment
            };
            return options;
        }

        /*
         * 

            // Configuration for the Agent application (usual IdWeb/MISE configuration. Can be done programmatically or in appsettings.json).
            services.Configure<MicrosoftIdentityApplicationOptions>(
                options =>
                {
                    options.Name = "AzureAd"; // Name of the configuration section in appsettings.json
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

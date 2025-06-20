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
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddAgentIdentities(this IServiceCollection services)
        {
            Throws.IfNull(services);

            // Register the OidcFic services for agent applications to work.
            services.AddOidcFic();

            return services;
        }


        /// <summary>
        /// Updates the options to acquire a token for the agent identity.
        /// </summary>
        /// <param name="options">Authorization header provider options.</param>
        /// <param name="agentApplicationId">The agent identity GUID.</param>
        /// <returns>The updated authorization header provider options.</returns>
        public static AuthorizationHeaderProviderOptions WithAgentIdentity(this AuthorizationHeaderProviderOptions options, string agentApplicationId)
        {
            // It's possible to start with no options, so we initialize it if it's null.
            if (options == null)
                options = new AuthorizationHeaderProviderOptions();

            // AcquireTokenOptions holds the information needed to acquire a token for the Agent Identity
            options.AcquireTokenOptions ??= new AcquireTokenOptions();
            options.AcquireTokenOptions.ForAgentIdentity(agentApplicationId);

            return options;
        }

        // TODO:make public?
        private static AcquireTokenOptions ForAgentIdentity(this AcquireTokenOptions options, string agentApplicationId)
        {
            options.ExtraParameters ??= new Dictionary<string, object>();

            // Until it makes it way through Abstractions
            options.ExtraParameters["fmiPathForClientAssertion"] = agentApplicationId;

            // TODO: do we want to expose a mechanism to override the MicrosoftIdentityOptions instead of leveraging
            // the default configuration section / named options?.
            options.ExtraParameters["MicrosoftIdentityOptions"] = new MicrosoftEntraApplicationOptions
            {
                ClientId = agentApplicationId, // Agent identity Client ID.
                ClientCredentials = [ new CredentialDescription() {
                    SourceType = CredentialSource.CustomSignedAssertion,
                    CustomSignedAssertionProviderName = "OidcIdpSignedAssertion",
                    CustomSignedAssertionProviderData = new Dictionary<string, object> {
                        { "ConfigurationSection", "AzureAd" },        // Use the default configuration section name
                        { "RequiresSignedAssertionFmiPath", true }, // The OidcIdpSignedAssertionProvider will require the fmiPath to be provided in the assertionRequestOptions.
                    }
                }]
            };
            return options;
        }
    }
}

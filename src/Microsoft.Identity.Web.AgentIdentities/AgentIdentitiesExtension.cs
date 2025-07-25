// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Web.AgentIdentities;

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

            // Register a callback to process the agent user identity before acquiring a token.
            services.Configure<TokenAcquisitionExtensionOptions>(options =>
            {
                options.OnBeforeTokenAcquisitionForTestUserAsync += AgentUserIdentityMsalAddIn.OnBeforeUserFicForAgentUserIdentityAsync;
            });

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

        /// <summary>
        /// Updates the options to acquire a token for the agent user identity.
        /// </summary>
        /// <param name="options">Authorization header provider options.</param>
        /// <param name="agentApplicationId">The agent identity GUID.</param>
        /// <param name="username">upn of the user.</param>
        /// <returns>The updated authorization header provider options.</returns>
        public static AuthorizationHeaderProviderOptions WithAgentUserIdentity(this AuthorizationHeaderProviderOptions options, string agentApplicationId, string username)
        {
            // It's possible to start with no options, so we initialize it if it's null.
            if (options == null)
                options = new AuthorizationHeaderProviderOptions();

            options.AcquireTokenOptions ??= new AcquireTokenOptions();
            options.AcquireTokenOptions.ExtraParameters ??= new Dictionary<string, object>();
            options.WithAgentIdentity(agentApplicationId);

            options.AcquireTokenOptions.ExtraParameters[Constants.UsernameKey] = username;
            options.AcquireTokenOptions.ExtraParameters![Constants.AgentIdentityKey] = agentApplicationId;
            return options;
        }

        // TODO:would it make sense to have it public?
        internal static AcquireTokenOptions ForAgentIdentity(this AcquireTokenOptions options, string agentApplicationId)
        {
            options.ExtraParameters ??= new Dictionary<string, object>();

            // Until it makes it way through Abstractions
            options.ExtraParameters[Constants.FmiPathForClientAssertion] = agentApplicationId;

            // TODO: do we want to expose a mechanism to override the MicrosoftIdentityOptions instead of leveraging
            // the default configuration section / named options?.
            options.ExtraParameters[Constants.MicrosoftIdentityOptionsParameter] = new MicrosoftEntraApplicationOptions
            {
                ClientId = agentApplicationId, // Agent identity Client ID.
                ClientCredentials = [ new CredentialDescription() {
                    SourceType = CredentialSource.CustomSignedAssertion,
                    CustomSignedAssertionProviderName = "OidcIdpSignedAssertion",
                    CustomSignedAssertionProviderData = new Dictionary<string, object> {
                        { "ConfigurationSection", "AzureAd" },      // Use the default configuration section name
                        { "RequiresSignedAssertionFmiPath", true }, // The OidcIdpSignedAssertionProvider will require the fmiPath to be provided in the assertionRequestOptions.
                    }
                }]
            };
            return options;
        }
    }
}

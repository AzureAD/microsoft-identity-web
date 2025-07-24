// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Extensibility;

namespace Microsoft.Identity.Web.AgentIdentities
{
    internal static class AgentUserIdentityMsalAddIn
    {
        internal static async Task OnBeforeUserFicForAgentUserIdentityAsync(
            AcquireTokenByUsernameAndPasswordConfidentialParameterBuilder builder,
            AcquireTokenOptions? options,
            ClaimsPrincipal user)
        {
            if (options == null || options.ExtraParameters == null)
            {
                return;
            }
            IServiceProvider serviceProvider = (IServiceProvider)options.ExtraParameters![Constants.ExtensionOptionsServiceProviderKey];
            options.ExtraParameters.TryGetValue(Constants.AgentIdentityKey, out object? agentIdentityObject);
            options.ExtraParameters.TryGetValue(Constants.UsernameKey, out object? usernameObject);
            if (agentIdentityObject is string agentIdentity && usernameObject is string username)
            {
                ITokenAcquirerFactory tokenAcquirerFactory = serviceProvider.GetRequiredService<ITokenAcquirerFactory>();
                IAuthenticationSchemeInformationProvider authenticationSchemeInformationProvider =
                    serviceProvider.GetRequiredService<IAuthenticationSchemeInformationProvider>();
                ITokenAcquirer tokenAcquirer = tokenAcquirerFactory.GetTokenAcquirer(authenticationSchemeInformationProvider.GetEffectiveAuthenticationScheme(options.AuthenticationOptionsName));

                // Get the signed assertion for the Agent identity (to be used as a user creds in the user FIC)
                var resultUserFicAssertion = await tokenAcquirer.GetTokenForAppAsync(
                    "api://AzureAdTokenExchange/.default",
                    options.ForAgentIdentity(agentIdentity));
                string? userFicAssertion = resultUserFicAssertion?.AccessToken;

                // Get the client assertion for the agent identity.
                // We built this parameter when the developper called WithAgentUserIdentity, so we know its structure.
                MicrosoftEntraApplicationOptions? o = options.ExtraParameters[Constants.MicrosoftIdentityOptionsParameter] as MicrosoftEntraApplicationOptions;
                ClientAssertionProviderBase? clientAssertionProvider = o!.ClientCredentials!.First().CachedValue as ClientAssertionProviderBase;
                string clientAssertion = await clientAssertionProvider!.GetSignedAssertionAsync(null)!; // Its' coming from the cache, as computed when getting the user assertion.

                // Register the MSAL extension that will modify the token request just in time.
                MsalAuthenticationExtension extension = new()
                {
                    OnBeforeTokenRequestHandler = (request) =>
                    {
                        // Important: this is on behalf of the agent identity, not agent application.
                        request.BodyParameters["client_id"] = agentIdentity;

                        // User FIC parameters
                        request.BodyParameters["username"] = username;
                        request.BodyParameters["user_federated_identity_credential"] = userFicAssertion;
                        request.BodyParameters["grant_type"] = "user_fic";
                        request.BodyParameters["client_assertion"] = clientAssertion;
                        request.BodyParameters.Remove("password");

                        if (request.BodyParameters.TryGetValue("client_secret", out var secret)
                                && secret.Equals("default", StringComparison.OrdinalIgnoreCase))
                        {
                            request.BodyParameters.Remove("client_secret");
                        }

                        // For the moment
                        request.RequestUri = new Uri(request.RequestUri + "?slice=first");
                        return Task.CompletedTask;
                    }
                };
                builder.WithAuthenticationExtension(extension);
            }
        }
    }
}

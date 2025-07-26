// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
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
        private const string azureAdTokenExchangeScope = "api://AzureADTokenExchange/.default";
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

                AcquireTokenOptions optionsForAgentIdFic = options.Clone();
                optionsForAgentIdFic.ExtraParameters = new Dictionary<string, object>();
                optionsForAgentIdFic.FmiPath = agentIdentity;
                var agentIdentityFic = await tokenAcquirer.GetTokenForAppAsync(azureAdTokenExchangeScope, options);
                if (agentIdentityFic is null)
                {
                    throw new InvalidOperationException("Failed to acquire the signed assertions.");
                }

                string userFicAssertion = agentIdentityFic.AccessToken!;

                // Register the MSAL extension that will modify the token request just in time.
                MsalAuthenticationExtension extension = new()
                {
                    OnBeforeTokenRequestHandler = (request) =>
                    {
                        // Already in the request:
                        // - client_id = agentIdentity;
                        // - client_assertion is the AA FIC

                        // User FIC parameters
                        request.BodyParameters["username"] = username;
                        request.BodyParameters["user_federated_identity_credential"] = userFicAssertion;
                        request.BodyParameters["grant_type"] = "user_fic";
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

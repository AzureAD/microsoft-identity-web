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
        internal static Task OnBeforeUserFicForAgentUserIdentityAsync(
            AcquireTokenByUsernameAndPasswordConfidentialParameterBuilder builder,
            AcquireTokenOptions? options,
            ClaimsPrincipal user)
        {
            if (options == null || options.ExtraParameters == null)
            {
                return Task.CompletedTask;
            }
            IServiceProvider serviceProvider = (IServiceProvider)options.ExtraParameters![Constants.ExtensionOptionsServiceProviderKey];
            options.ExtraParameters.TryGetValue(Constants.AgentIdentityKey, out object? agentIdentityObject);
            options.ExtraParameters.TryGetValue(Constants.UsernameKey, out object? usernameObject);
            if (agentIdentityObject is string agentIdentity && usernameObject is string username)
            {
                // Register the MSAL extension that will modify the token request just in time.
                MsalAuthenticationExtension extension = new()
                {
                    // This will be called AFTER the client assertion callback !
                    OnBeforeTokenRequestHandler = async (request) =>
                    {
                        // Already in the request:
                        // - client_id = agentIdentity;
                        // - client_assertion is the AA FIC
                        ITokenAcquirerFactory tokenAcquirerFactory = serviceProvider.GetRequiredService<ITokenAcquirerFactory>();
                        IAuthenticationSchemeInformationProvider authenticationSchemeInformationProvider =
                            serviceProvider.GetRequiredService<IAuthenticationSchemeInformationProvider>();

                        ITokenAcquirer tokenAcquirer = tokenAcquirerFactory.GetTokenAcquirer(authenticationSchemeInformationProvider.GetEffectiveAuthenticationScheme(options.AuthenticationOptionsName));

                        var agentIdentityFic = await tokenAcquirer.GetTokenForAppAsync(azureAdTokenExchangeScope, options).ConfigureAwait(false);

                        // User FIC parameters
                        request.BodyParameters["username"] = username;
                        request.BodyParameters["user_federated_identity_credential"] = agentIdentityFic.AccessToken;
                        request.BodyParameters["grant_type"] = "user_fic";
                        request.BodyParameters.Remove("password");

                        if (request.BodyParameters.TryGetValue("client_secret", out var secret)
                                && secret.Equals("default", StringComparison.OrdinalIgnoreCase))
                        {
                            request.BodyParameters.Remove("client_secret");
                        }

                        // For the moment
                        request.RequestUri = new Uri(request.RequestUri + "?slice=first");
                       // return Task.CompletedTask;
                    }
                };
                builder.WithAuthenticationExtension(extension);
            }
            return Task.CompletedTask;

        }
    }
}

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Extensibility;

namespace Microsoft.Identity.Web.AgentIdentities
{
    internal static class MsalAgentUserIdentityAddIn
    {
        internal static void OnBeforeUserFicForAgentUserIdentity(
            AcquireTokenByUsernameAndPasswordConfidentialParameterBuilder builder,
            AcquireTokenOptions? options,
            ClaimsPrincipal user)
        {
            if (options == null)
            {
                return;
            }
            IServiceProvider serviceProvider = (IServiceProvider)options.ExtraParameters![Constants.ExtensionOptionsServiceProviderKey];
            options.ExtraParameters.TryGetValue(Constants.AgentIdentityKey, out object? agentIdentityObject);
            options.ExtraParameters.TryGetValue(Constants.UsernameKey, out object? usernameObject);
            if (agentIdentityObject is string agentIdentity && usernameObject is string username)
            {
                // Get a FIC token for the AA. This will be the client credentials.
                // This could be set in the WithAgentUserIdentity method as a custom FIC assertion
                // this could also be done here.
                ITokenAcquirerFactory tokenAcquirerFactory = serviceProvider.GetRequiredService<ITokenAcquirerFactory>();
                IAuthenticationSchemeInformationProvider authenticationSchemeInformationProvider =
                    serviceProvider.GetRequiredService<IAuthenticationSchemeInformationProvider>();
                ITokenAcquirer tokenAcquirer = tokenAcquirerFactory.GetTokenAcquirer(authenticationSchemeInformationProvider.GetEffectiveAuthenticationScheme(options.AuthenticationOptionsName));

                // Get the signed a assertion for the agent application (to be used as a client creds in the user FIC).
#pragma warning disable VSTHRD002 // Avoid problematic synchronous waits
                var options2 = options.Clone();
                options2.FmiPath = agentIdentity;
                var resultSignedAssertion = tokenAcquirer.GetTokenForAppAsync(
                    "api://AzureAdTokenExchange/.default",
                    options2).GetAwaiter().GetResult();
#pragma warning restore VSTHRD002 // Avoid problematic synchronous waits
                string? clientAssertion = resultSignedAssertion?.AccessToken;


                // Get the signed assertion for the Agent identity (to be used as a user creds in the user FIC)
#pragma warning disable VSTHRD002 // Avoid problematic synchronous waits
                var resultUserFicAssertion = tokenAcquirer.GetTokenForAppAsync(
                    "api://AzureAdTokenExchange/.default",
                    options.ForAgentIdentity(agentIdentity, true)).GetAwaiter().GetResult();
#pragma warning restore VSTHRD002 // Avoid problematic synchronous waits
                string? userFicAssertion = resultUserFicAssertion?.AccessToken;

                if (clientAssertion == null || userFicAssertion == null)
                {
                    throw new ArgumentException("Failed to acquire the signed assertion for the agent identity or user FIC assertion.");
                }

                builder.WithUserFederatedIdentityCredential(username, clientAssertion, userFicAssertion, agentIdentity);
            }
        }

        internal static AcquireTokenByUsernameAndPasswordConfidentialParameterBuilder WithUserFederatedIdentityCredential(
           this AcquireTokenByUsernameAndPasswordConfidentialParameterBuilder builder,
           string username,
           string clientAssertion,
           string userAssertion,
           string agentIdentity)
        {
            if (string.IsNullOrEmpty(username))
            {
                throw new ArgumentNullException(nameof(username));
            }

            if (string.IsNullOrEmpty(userAssertion))
            {
                throw new ArgumentNullException(nameof(userAssertion));
            }

            if (string.IsNullOrEmpty(clientAssertion))
            {
                throw new ArgumentNullException(nameof(clientAssertion));
            }

            AssertionRequestOptions assertionOptions = new();

            MsalAuthenticationExtension extension = new()
            {
                OnBeforeTokenRequestHandler = (request) =>
                {
                    request.BodyParameters["username"] = username;
                    request.BodyParameters["user_federated_identity_credential"] = userAssertion;
                    request.BodyParameters["grant_type"] = "user_fic";
                    request.BodyParameters.Remove("password");
                    request.BodyParameters["client_assertion"] = clientAssertion;

                    if (request.BodyParameters.TryGetValue("client_secret", out var secret)
                        && secret.Equals("default", StringComparison.OrdinalIgnoreCase))
                    {
                        request.BodyParameters.Remove("client_secret");
                    }

                    request.RequestUri = new Uri(request.RequestUri + "?slice=first");
                    return Task.CompletedTask;
                }
            };

            return (AcquireTokenByUsernameAndPasswordConfidentialParameterBuilder)builder.WithAuthenticationExtension(extension);
        }
    }
}

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
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
                    OnBeforeTokenRequestHandler = async (request) =>
                    {
                        // Get the services from the service provider.
                        ITokenAcquirerFactory tokenAcquirerFactory = serviceProvider.GetRequiredService<ITokenAcquirerFactory>();
                        IAuthenticationSchemeInformationProvider authenticationSchemeInformationProvider =
                            serviceProvider.GetRequiredService<IAuthenticationSchemeInformationProvider>();
                        IOptionsMonitor<MicrosoftIdentityApplicationOptions> optionsMonitor =
                            serviceProvider.GetRequiredService<IOptionsMonitor<MicrosoftIdentityApplicationOptions>>();

                        // Get the FIC token for the agent application.
                        string authenticationScheme = authenticationSchemeInformationProvider.GetEffectiveAuthenticationScheme(options.AuthenticationOptionsName);
                        ITokenAcquirer tokenAcquirer = tokenAcquirerFactory.GetTokenAcquirer(authenticationScheme);
                        ITokenAcquirer agentApplicationTokenAcquirer = tokenAcquirerFactory.GetTokenAcquirer();
                        AcquireTokenResult aaFic = await agentApplicationTokenAcquirer.GetFicAsync(new() { FmiPath = agentIdentity }); // Uses the regular client credentials
                        string? clientAssertion = aaFic.AccessToken;

                        // Get the FIC token for the agent identity.
                        MicrosoftIdentityApplicationOptions microsoftIdentityApplicationOptions = optionsMonitor.Get(authenticationScheme);
                        ITokenAcquirer agentIdentityTokenAcquirer = tokenAcquirerFactory.GetTokenAcquirer(new MicrosoftIdentityApplicationOptions
                        {
                            ClientId = agentIdentity,
                            Instance = microsoftIdentityApplicationOptions.Instance,
                            Authority = microsoftIdentityApplicationOptions.Authority,
                            TenantId = microsoftIdentityApplicationOptions.TenantId
                        });
                        AcquireTokenResult aidFic = await agentIdentityTokenAcquirer.GetFicAsync(clientAssertion: clientAssertion); // Uses the agent identity
                        string? userFicAssertion = aidFic.AccessToken;

                        // Already in the request:
                        // - client_id = agentIdentity;

                        // User FIC parameters
                        request.BodyParameters["client_assertion_type"] = "urn:ietf:params:oauth:client-assertion-type:jwt-bearer";
                        request.BodyParameters["client_assertion"] = clientAssertion;
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
                    }
                };
                builder.WithAuthenticationExtension(extension);
            }
            return Task.CompletedTask;
        }
    }
}

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
            options.ExtraParameters.TryGetValue(Constants.UserIdKey, out object? userIdObject);
            if (agentIdentityObject is string agentIdentity && (usernameObject is string || userIdObject is string))
            {
                // Register the MSAL extension that will modify the token request just in time.
                MsalAuthenticationExtension extension = new()
                {
                    OnBeforeTokenRequestHandler = async (request) =>
                    {
                        // Get the services from the service provider.
                        ITokenAcquirerFactory tokenAcquirerFactory = serviceProvider.GetRequiredService<ITokenAcquirerFactory>();
                        Abstractions.IAuthenticationSchemeInformationProvider authenticationSchemeInformationProvider =
                            serviceProvider.GetRequiredService<Abstractions.IAuthenticationSchemeInformationProvider>();
                        IOptionsMonitor<MicrosoftIdentityApplicationOptions> optionsMonitor =
                            serviceProvider.GetRequiredService<IOptionsMonitor<MicrosoftIdentityApplicationOptions>>();

                        // Get the FIC token for the agent application.
                        string authenticationScheme = authenticationSchemeInformationProvider.GetEffectiveAuthenticationScheme(options.AuthenticationOptionsName);
                        ITokenAcquirer tokenAcquirer = tokenAcquirerFactory.GetTokenAcquirer(authenticationScheme);
                        ITokenAcquirer agentApplicationTokenAcquirer = tokenAcquirerFactory.GetTokenAcquirer();
                        AcquireTokenResult aaFic = await agentApplicationTokenAcquirer.GetFicTokenAsync(new() { Tenant = options.Tenant, FmiPath = agentIdentity }); // Uses the regular client credentials
                        string? clientAssertion = aaFic.AccessToken;

                        // Get the FIC token for the agent identity.
                        MicrosoftIdentityApplicationOptions microsoftIdentityApplicationOptions = optionsMonitor.Get(authenticationScheme);
                        ITokenAcquirer agentIdentityTokenAcquirer = tokenAcquirerFactory.GetTokenAcquirer(new MicrosoftIdentityApplicationOptions
                        {
                            ClientId = agentIdentity,
                            Instance = microsoftIdentityApplicationOptions.Instance,
                            Authority = microsoftIdentityApplicationOptions.Authority,
                            TenantId = options.Tenant ?? microsoftIdentityApplicationOptions.TenantId
                        });
                        AcquireTokenResult aidFic = await agentIdentityTokenAcquirer.GetFicTokenAsync(options: new() { Tenant = options.Tenant }, clientAssertion: clientAssertion); // Uses the agent identity
                        string? userFicAssertion = aidFic.AccessToken;

                        // Already in the request:
                        // - client_id = agentIdentity;

                        // User FIC parameters
                        request.BodyParameters["client_assertion_type"] = "urn:ietf:params:oauth:client-assertion-type:jwt-bearer";
                        request.BodyParameters["client_assertion"] = clientAssertion;
                        
                        // Handle UPN vs OID: UPN takes precedence if both are present
                        if (usernameObject is string username && !string.IsNullOrEmpty(username))
                        {
                            request.BodyParameters["username"] = username;
                            if (request.BodyParameters.ContainsKey("user_id"))
                            {
                                request.BodyParameters.Remove("user_id");
                            }
                        }
                        else if (userIdObject is string userId && !string.IsNullOrEmpty(userId))
                        {
                            request.BodyParameters["user_id"] = userId;
                            if (request.BodyParameters.ContainsKey("username"))
                            {
                                request.BodyParameters.Remove("username");
                            }
                        }
                        
                        request.BodyParameters["user_federated_identity_credential"] = userFicAssertion;
                        request.BodyParameters["grant_type"] = "user_fic";
                        request.BodyParameters.Remove("password");

                        if (request.BodyParameters.TryGetValue("client_secret", out var secret))
                        {
                            request.BodyParameters.Remove("client_secret");
                        }
                    }
                };
                builder.WithAuthenticationExtension(extension);
            }
            return Task.CompletedTask;
        }
    }
}

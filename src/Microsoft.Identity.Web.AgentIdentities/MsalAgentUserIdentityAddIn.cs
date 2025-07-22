// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Extensibility;

namespace Microsoft.Identity.Web.AgentIdentities
{
    internal static class MsalAgentUserIdentityAddIn
    {
        public static AcquireTokenByUsernameAndPasswordConfidentialParameterBuilder WithUserFederatedIdentityCredential(
           this AcquireTokenByUsernameAndPasswordConfidentialParameterBuilder builder,
           string username,
           string userAssertion)
        {
            if (string.IsNullOrEmpty(username))
            {
                throw new ArgumentNullException(nameof(username));
            }

            if (string.IsNullOrEmpty(userAssertion))
            {
                throw new ArgumentNullException(nameof(userAssertion));
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

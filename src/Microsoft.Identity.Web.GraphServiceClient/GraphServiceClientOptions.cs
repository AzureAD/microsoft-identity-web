// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Abstractions;
using System.Collections.Generic;
using System.Security.Claims;

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// Options passed-in to call Microsoft Graph.
    /// </summary>
    public class GraphServiceClientOptions : AuthorizationHeaderProviderOptions
    {
        /// <summary>
        /// Options used to configure the authentication provider for Microsoft Graph.
        /// </summary>
        public GraphServiceClientOptions()
        {
            BaseUrl = Constants.GraphBaseUrlV1;
            Scopes = new[] { Constants.UserReadScope };
        }

        /// <summary>
        /// Scopes required to call the downstream web API.
        /// For instance "user.read mail.read".
        /// For Microsoft identity, in the case of application tokens (token 
        /// requested by the app on behalf of itself), there should be only one scope, and it
        /// should end in "./default")
        /// </summary>
        public IEnumerable<string> Scopes { get; set; }

        /// <summary>
        /// When calling Microsoft graph with delegated permissions offers a way to override the
        /// user on whose behalf the call is made.
        /// </summary>
        public ClaimsPrincipal? User { get; set; }
    }
}

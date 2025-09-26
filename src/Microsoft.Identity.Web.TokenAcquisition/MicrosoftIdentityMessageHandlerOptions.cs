// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Identity.Abstractions;

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// Configuration options for MicrosoftIdentityMessageHandler authentication.
    /// </summary>
    public class MicrosoftIdentityMessageHandlerOptions
    {
        /// <summary>
        /// The scopes to request for the token.
        /// </summary>
        public IList<string> Scopes { get; set; } = new List<string>();

        /// <summary>
        /// Additional authorization header provider options.
        /// </summary>
        public AuthorizationHeaderProviderOptions? AuthorizationHeaderProviderOptions { get; set; }

        /// <summary>
        /// Creates an AuthorizationHeaderProviderOptions instance with scopes configured.
        /// </summary>
        /// <returns>An AuthorizationHeaderProviderOptions instance.</returns>
        internal AuthorizationHeaderProviderOptions ToAuthorizationHeaderProviderOptions()
        {
            var options = AuthorizationHeaderProviderOptions ?? new AuthorizationHeaderProviderOptions();
            
            // Copy over any additional properties as needed
            return options;
        }

        /// <summary>
        /// Gets the scopes as an enumerable of strings.
        /// </summary>
        /// <returns>The scopes.</returns>
        internal IEnumerable<string> GetScopes()
        {
            return Scopes ?? Enumerable.Empty<string>();
        }
    }
}
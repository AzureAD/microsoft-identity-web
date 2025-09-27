// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using Microsoft.Identity.Abstractions;

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// Configuration options for MicrosoftIdentityMessageHandler authentication.
    /// </summary>
    public class MicrosoftIdentityMessageHandlerOptions : AuthorizationHeaderProviderOptions
    {
        /// <summary>
        /// The scopes to request for the token.
        /// For instance "user.read mail.read".
        /// For Microsoft identity, in the case of application tokens (token 
        /// requested by the app on behalf of itself), there should be only one scope, and it
        /// should end in ".default")
        /// </summary>
        public IList<string> Scopes { get; set; } = new List<string>();
    }
}
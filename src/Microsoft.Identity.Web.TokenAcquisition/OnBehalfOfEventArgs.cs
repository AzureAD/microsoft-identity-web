// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Security.Claims;

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// Event arguments for on-behalf-of token acquisition operations.
    /// Contains user assertion token information about the user and additional data for the token request.
    /// </summary>
    public class OnBehalfOfEventArgs
    {
        /// <summary>
        /// Gets or sets the claims principal representing the user for whom the token is being acquired.
        /// This is the claims principal into the the api.
        /// This value can be null.
        /// </summary>
        public ClaimsPrincipal? User { get; set; }

        /// <summary>
        /// Gets or sets additional context information for the token acquisition operation.
        /// If the incoming token is encrypted, it will be decrypted and made available in this property.
        /// </summary>
        public string? UserAssertionToken { get; set; }
    }
}

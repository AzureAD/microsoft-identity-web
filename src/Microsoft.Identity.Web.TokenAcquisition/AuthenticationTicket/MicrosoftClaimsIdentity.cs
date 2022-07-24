// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;

// TODO
// This class should really be in Microsoft.IdentityModel.Tokens

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// Extension of ClaimsIdentity to represent the data like the Token, the raw token
    /// </summary>
    class MicrosoftClaimsIdentity : ClaimsIdentity
    {
        // TODO: decide the contructors to have.


        /// <summary>
        /// Token associated with this ClaimsIdentity (if any)
        /// </summary>
        public SecurityToken? Token { get; }

        /// <summary>
        /// Token raw data associated with this claims identity (if any)
        /// Could also be the BootstrapContext?
        /// </summary>
        public string? TokenRawData { get; }
    }
}

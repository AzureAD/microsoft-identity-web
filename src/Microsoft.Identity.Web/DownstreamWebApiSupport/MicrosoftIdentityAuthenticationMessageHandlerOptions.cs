// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// Options passed-in to Microsoft Identity message handlers.
    /// </summary>
    public class MicrosoftIdentityAuthenticationMessageHandlerOptions : MicrosoftIdentityAuthenticationBaseOptions, ICloneable
    {
        /// <summary>
        /// Clone the options (to be able to override them).
        /// </summary>
        /// <returns>A clone of the options.</returns>
        public MicrosoftIdentityAuthenticationMessageHandlerOptions Clone()
        {
            return new MicrosoftIdentityAuthenticationMessageHandlerOptions
            {
                Scopes = Scopes,
                Tenant = Tenant,
                UserFlow = UserFlow,
                IsProofOfPossessionRequest = IsProofOfPossessionRequest,
                TokenAcquisitionOptions = TokenAcquisitionOptions.Clone(),
                AuthenticationScheme = AuthenticationScheme,
            };
        }

        /// <summary>
        /// Clone the options (to be able to override them).
        /// </summary>
        /// <returns>A clone of the options.</returns>
        object ICloneable.Clone()
        {
            return Clone();
        }
    }
}

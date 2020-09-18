// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// Options passed-in to create the token acquisition object which calls into MSAL .NET.
    /// </summary>
    public class TokenAcquisitionOptions
    {
        /// <summary>
        /// Sets the correlation id to be used in the authentication request
        /// to the /token endpoint.
        /// </summary>
        public Guid CorrelationId { get; set; }

        /// <summary>
        /// Sets Extra Query Parameters for the query string in the HTTP authentication request.
        /// </summary>
        public Dictionary<string, string>? ExtraQueryParameters { get; set; } = null;

        /// <summary>
        /// Clone the options (to be able to override them).
        /// </summary>
        /// <returns>A clone of the options.</returns>
        public TokenAcquisitionOptions Clone()
        {
            return new TokenAcquisitionOptions
            {
                CorrelationId = CorrelationId,
                ExtraQueryParameters = ExtraQueryParameters,
            };
        }
    }
}

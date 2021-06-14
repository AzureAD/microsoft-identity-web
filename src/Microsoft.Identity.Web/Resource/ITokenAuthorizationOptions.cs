// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// Properties related to authorization of access tokens in web APIs.
    /// </summary>
    public interface ITokenAuthorizationOptions
    {
        /// <summary>
        /// Applies to a web API: does the web API allow app-only tokens?.
        /// </summary>
        /// <remarks>
        /// In the app registration you'll need to opt-in for the idtyp claim.
        /// </remarks>
        public bool ApiAllowsAppOnlyTokens { get; set; }

        /// <summary>
        /// Applies to a web API: does the web API allow guest accounts?.
        /// </summary>
        /// <remarks>
        /// In the app registration for your web API, you'll need to opt-in for the acct claim.
        /// </remarks>
        public bool ApiAllowsGuestAccounts { get; set; }

        /// <summary>
        /// Applies to web apps and web APIs. Enables to provide the list
        /// of tenant Ids the user of whom are allowed to call the web API
        /// or log-in to the web app.
        /// </summary>
        public IEnumerable<string>? AllowedTenantIds { get; set; }

        /// <summary>
        /// Applies to web APIs. Enables to provide the list
        /// of client applications allowed to call this API.
        /// </summary>
        public IEnumerable<string>? AllowedClientApplications { get; set; }
    }
}

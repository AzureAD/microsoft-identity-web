// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading.Tasks;
using Microsoft.Identity.Client;

namespace Microsoft.Identity.Web.Extensibility
{
    /// <summary>
    /// Provides access to the <see cref="IConfidentialClientApplication"/> managed by
    /// Microsoft.Identity.Web for a given authentication scheme. Use this when you need
    /// to call MSAL directly (e.g., for custom token acquisition flows) while reusing
    /// the same application instance, credentials, and token cache that IdWeb manages.
    /// </summary>
    public interface IConfidentialClientApplicationProvider
    {
        /// <summary>
        /// Gets the <see cref="IConfidentialClientApplication"/> for the specified authentication scheme.
        /// </summary>
        /// <param name="authenticationScheme">
        /// The authentication scheme name. If null, the effective default scheme is used.
        /// </param>
        /// <returns>The confidential client application instance.</returns>
        Task<IConfidentialClientApplication> GetConfidentialClientApplicationAsync(
            string? authenticationScheme = null);
    }
}

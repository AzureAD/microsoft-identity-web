// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// Options passed-in to call Microsoft Graph.
    /// </summary>
    public class MicrosoftGraphOptions
    {
        /// <summary>
        /// Base URL for the Microsoft Graph API. By default: <c>"https://graph.microsoft.com/v1.0/"</c>
        /// but it can be changed to use the Microsoft Graph Beta endpoint or national cloud versions
        /// of MicrosoftGraph.
        /// </summary>
        public string BaseUrl { get; set; } = Constants.GraphBaseUrlV1;

        /// <summary>
        /// Space separated scopes used to call Microsoft Graph,
        /// for instance <c>user.read mail.read</c>.
        /// </summary>
        public string? Scopes { get; set; } = Constants.UserReadScope;
    }
}

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Kiota.Abstractions;

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// Authentication options controlling how the authentication request to the Microsoft Graph service.
    /// </summary>
    public class GraphAuthenticationOptions : GraphServiceClientOptions, IRequestOption
    {
        /// <summary>
        /// Base URL for the Microsoft Graph API. By default: <c>"https://graph.microsoft.com/v1.0/"</c>
        /// </summary>
        public new string BaseUrl { get { return base.BaseUrl!; } }
    }
}

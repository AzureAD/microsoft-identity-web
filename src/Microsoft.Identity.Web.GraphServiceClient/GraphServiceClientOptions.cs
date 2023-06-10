// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Net.Http;
using System;
using Microsoft.Identity.Abstractions;
using System.ComponentModel;

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// Options passed-in to call Microsoft Graph.
    /// </summary>
    public class GraphServiceClientOptions : DownstreamApiOptions
    {
        /// <summary>
        /// Options used to configure the authentication provider for Microsoft Graph.
        /// </summary>
        public GraphServiceClientOptions()
        {
            BaseUrl = Constants.GraphBaseUrlV1;
            Scopes = new[] { Constants.UserReadScope };
        }

        // Hiding members that should not be configured in case of Microsoft Graph.
        [EditorBrowsable(EditorBrowsableState.Never)]
        private new Func<object?, HttpContent?>? Serializer { get { return base.Serializer; } set { base.Serializer = value; } }

        [EditorBrowsable(EditorBrowsableState.Never)]
        private new Func<HttpContent?, object?>? Deserializer { get { return base.Deserializer; } set { base.Deserializer = value; } }

        [EditorBrowsable(EditorBrowsableState.Never)]
        private new string RelativePath {get { return base.RelativePath; } set { base.RelativePath = value; } }

        [EditorBrowsable(EditorBrowsableState.Never)]
        private new HttpMethod HttpMethod { get { return base.HttpMethod; } set { base.HttpMethod = value; } }

        [EditorBrowsable(EditorBrowsableState.Never)]
        private new Action<HttpRequestMessage>? CustomizeHttpRequestMessage { get { return base.CustomizeHttpRequestMessage; } set { base.CustomizeHttpRequestMessage = value; } }
    }
}

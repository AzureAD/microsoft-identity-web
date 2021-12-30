// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Net.Http;

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// Options passed-in to call downstream web APIs. To call Microsoft Graph, see rather
    /// <c>MicrosoftGraphOptions</c> in the <c>Microsoft.Identity.Web.MicrosoftGraph</c> assembly.
    /// </summary>
    public class DownstreamWebApiOptions : MicrosoftIdentityAuthenticationBaseOptions, ICloneable
    {
        /// <summary>
        /// Base URL for the called downstream web API. For instance <c>"https://graph.microsoft.com/beta/".</c>.
        /// </summary>
        public string BaseUrl { get; set; } = Constants.GraphBaseUrlV1;

        /// <summary>
        /// Path relative to the <see cref="BaseUrl"/> (for instance "me").
        /// </summary>
        public string RelativePath { get; set; } = string.Empty;

        /// <summary>
        /// HTTP method used to call this downstream web API (by default Get).
        /// </summary>
        public HttpMethod HttpMethod { get; set; } = HttpMethod.Get;

        /// <summary>
        /// Provides an opportunity to customize the HttpRequestMessage. For example,
        /// to customize the headers. This is called after the message was formed, including
        /// the AuthorizationHeader, and just before the message is sent.
        /// </summary>
        public Action<HttpRequestMessage>? CustomizeHttpRequestMessage { get; set; }

        /// <summary>
        /// Clone the options (to be able to override them).
        /// </summary>
        /// <returns>A clone of the options.</returns>
        public DownstreamWebApiOptions Clone()
        {
            return new DownstreamWebApiOptions
            {
                BaseUrl = BaseUrl,
                RelativePath = RelativePath,
                Scopes = Scopes,
                Tenant = Tenant,
                UserFlow = UserFlow,
                HttpMethod = HttpMethod,
                AuthenticationScheme = AuthenticationScheme,
                TokenAcquisitionOptions = TokenAcquisitionOptions.Clone(),
            };
        }

        /// <summary>
        /// Return the downstream web API URL.
        /// </summary>
        /// <returns>URL of the downstream web API.</returns>
#pragma warning disable CA1055 // Uri return values should not be strings
        public string GetApiUrl()
#pragma warning restore CA1055 // Uri return values should not be strings
        {
            return BaseUrl?.TrimEnd('/') + $"/{RelativePath}";
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

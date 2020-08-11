// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Net.Http;

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// Options passed-in to call web APIs. To call Microsoft Graph, see rather
    /// <see cref="MicrosoftGraphOptions"/>.
    /// </summary>
    public class CalledApiOptions
    {
        /// <summary>
        /// Base URL for the called Web API. For instance <c>"https://graph.microsoft.com/beta/".</c>.
        /// </summary>
        public string BaseUrl { get; set; } = "https://graph.microsoft.com/v1.0";

        /// <summary>
        /// Path relative to the <see cref="BaseUrl"/> (for instance "me").
        /// </summary>
        public string RelativePath { get; set; } = string.Empty;

        /// <summary>
        /// Space separated scopes required to call the Web API.
        /// For instance "user.read mail.read".
        /// </summary>
        public string? Scopes { get; set; } = null;

        /// <summary>
        /// [Optional] tenant ID. This is used for specific scenarios where
        /// the application needs to call a Web API on behalf of a user in several tenants.
        /// It would mostly be used from code, not from the configuration.
        /// </summary>
        public string? Tenant { get; set; } = null;

        /// <summary>
        /// [Optional]. User flow (in the case of a B2C Web API). If not
        /// specified, the B2C web API will be called with the default user flow from
        /// <see cref="MicrosoftIdentityOptions.DefaultUserFlow"/>.
        /// </summary>
        public string? UserFlow { get; set; } = null;

        /// <summary>
        /// Http method used to call this API (by default Get).
        /// </summary>
        public HttpMethod HttpMethod { get; set; } = HttpMethod.Get;

        /// <summary>
        /// Clone the options (to be able to override them).
        /// </summary>
        /// <returns>A clone of the options.</returns>
        public CalledApiOptions Clone()
        {
            return new CalledApiOptions
            {
                BaseUrl = BaseUrl,
                RelativePath = RelativePath,
                Scopes = Scopes,
                Tenant = Tenant,
                UserFlow = UserFlow,
                HttpMethod = HttpMethod,
            };
        }

        /// <summary>
        /// Return the Api URL.
        /// </summary>
        /// <returns>URL of the API</returns>
        public string GetApiUrl()
        {
            return BaseUrl?.TrimEnd('/') + $"/{RelativePath}";
        }

        /// <summary>
        /// Returns the scopes.
        /// </summary>
        /// <returns>Scopes.</returns>
        public string[] GetScopes()
        {
            return string.IsNullOrWhiteSpace(Scopes) ? new string[0] : Scopes.Split(' ');
        }

    }
}

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
    public class DownstreamWebApiOptions : ICloneable
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
        /// Space separated scopes required to call the downstream web API.
        /// For instance "user.read mail.read".
        /// </summary>
        public string? Scopes { get; set; }

        /// <summary>
        /// [Optional] tenant ID. This is used for specific scenarios where
        /// the application needs to call a downstream web API on  behalf of a user in several tenants.
        /// It would mostly be used from code, not from the configuration.
        /// </summary>
        public string? Tenant { get; set; }

        /// <summary>
        /// [Optional]. User flow (in the case of a B2C downstream web API). If not
        /// specified, the B2C downstream web API will be called with the default user flow from
        /// <see cref="MicrosoftIdentityOptions.DefaultUserFlow"/>.
        /// </summary>
        public string? UserFlow { get; set; }

        /// <summary>
        /// HTTP method used to call this downstream web API (by default Get).
        /// </summary>
        public HttpMethod HttpMethod { get; set; } = HttpMethod.Get;

        /// <summary>
        /// Modifies the token acquisition request so that the acquired token is a Proof of Possession token (PoP),
        /// rather than a Bearer token.
        /// PoP tokens are similar to Bearer tokens, but are bound to the HTTP request and to a cryptographic key,
        /// which MSAL can manage. See https://aka.ms/msal-net-pop.
        /// Set to true to enable PoP tokens automatically.
        /// </summary>
        public bool IsProofOfPossessionRequest { get; set; }

        /// <summary>
        ///  Options passed-in to create the token acquisition object which calls into MSAL .NET.
        /// </summary>
        public TokenAcquisitionOptions TokenAcquisitionOptions { get; set; } = new TokenAcquisitionOptions();

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
        /// Returns the scopes.
        /// </summary>
        /// <returns>Scopes.</returns>
        public string[] GetScopes()
        {
            return string.IsNullOrWhiteSpace(Scopes) ? new string[0] : Scopes.Split(' ');
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

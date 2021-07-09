// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// Options passed-in to Microsoft Identity message handlers.
    /// </summary>
    public class MicrosoftIdentityAuthenticationMessageHandlerOptions : ICloneable
    {
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
        /// Authentication scheme. If null, will use OpenIdConnectDefault.AuthenticationScheme
        /// if called from a web app, and JwtBearerDefault.AuthenticationScheme if called from a web API.
        /// </summary>
        public string? AuthenticationScheme { get; set; }

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

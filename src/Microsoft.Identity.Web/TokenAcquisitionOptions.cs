// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.Identity.Client.AppConfig;

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
        public Dictionary<string, string>? ExtraQueryParameters { get; set; }

        /// <summary>
        /// A string with one or multiple claims to request.
        /// Normally used with Conditional Access.
        /// </summary>
        public string? Claims { get; set; }

        /// <summary>
        /// Specifies if the token request will ignore the access token in the token cache
        /// and will attempt to acquire a new access token.
        /// If <c>true</c>, the request will ignore the token cache. The default is <c>false</c>.
        /// Use this option with care and only when needed, for instance, if you know that conditional access policies have changed,
        /// for it induces performance degradation, as the token cache is not utilized.
        /// </summary>
        public bool ForceRefresh { get; set; }

        /// <summary>
        /// Modifies the token acquisition request so that the acquired token is a Proof of Possession token (PoP),
        /// rather than a Bearer token.
        /// PoP tokens are similar to Bearer tokens, but are bound to the HTTP request and to a cryptographic key,
        /// which MSAL can manage. See https://aka.ms/msal-net-pop.
        /// </summary>
        public PoPAuthenticationConfiguration? PoPConfiguration { get; set; }

        /// <summary>
        /// todo.
        /// </summary>
        public CancellationToken CancellationToken { get; set; } = CancellationToken.None;

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
                ForceRefresh = ForceRefresh,
                Claims = Claims,
                PoPConfiguration = PoPConfiguration,
                CancellationToken = CancellationToken,
            };
        }
    }
}

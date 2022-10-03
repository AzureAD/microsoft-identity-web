// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;

namespace Microsoft.Identity.Abstractions
{
    /// <summary>
    /// Options directing the token acquisition.
    /// </summary>
    public class AcquireTokenOptions
    {
        /// <summary>
        /// Enables to override the tenant/account for which to get a token. 
        /// This is useful in multi-tenant apps in the cases where a given user account is a guest 
        /// in other tenants, and you want to acquire tokens for a specific tenant.
        /// </summary>
        /// <remarks>Can be the tenant ID or domain name.</remarks>
        public string? Tenant { get; set; }

        /// <summary>
        /// In the case of AzureAD B2C, uses a particular user flow.
        /// </summary>
        public string? UserFlow { get; set; }

        /// <summary>
        /// Gets the parameters describing the confidential client application (ClientId,
        /// Region, Authority, client credentials) from a particular 
        /// (ASP.NET Core) authentication scheme / settings.
        /// </summary>
        public string? AuthenticationOptionsName { get; set; }

        /// <summary>
        /// Sets the correlation id to be used in the request to the STS "/token" endpoint.
        /// </summary>
        public Guid CorrelationId { get; set; }

        /// <summary>
        /// Sets query parameters for the query string in the HTTP request to the 
        /// "/token" endpoint.
        /// </summary>
        public IDictionary<string, string>? ExtraQueryParameters { get; set; }

        /// Sets extra headers in the HTTP request to the STS "/token" endpoint.
        public IDictionary<string, string>? ExtraHeadersParameters { get; set; }

        /// <summary>
        /// A string with one or multiple claims to request. It's a json blob (encoded or not)
        /// Normally used with Conditional Access. It receives the Claims member of the UiRequiredException.
        /// </summary>
        public string? Claims { get; set; }

        /// <summary>
        /// Specifies if the token request will ignore the access token in the token cache
        /// and will attempt to acquire a new access token.
        /// If <c>true</c>, the request will ignore the token cache. The default is <c>false</c>.
        /// Use this option with care and only when needed, for instance, if you know that conditional access policies have changed,
        /// for it induces performance degradation, as the token cache is not utilized, and the STS might throttle the app.
        /// </summary>
        public bool ForceRefresh { get; set; }

        /// <summary>
        /// Modifies the token acquisition request so that the acquired token is a Proof of Possession token (PoP),
        /// rather than a Bearer token.
        /// PoP tokens are similar to Bearer tokens, but are bound to the HTTP request and to a cryptographic key,
        /// which MSAL can manage. See https://aka.ms/msal-net-pop.
        /// </summary>
        public string? PopPublicKey { get; set; }

        /// <summary>
        /// Key used for long running web APIs that need to call downstream web
        /// APIs on behalf of the user. Can be null, if you are not developing a long
        /// running web API, <see cref="LongRunningWebApiSessionKeyAuto"/> if you want
        /// Microsoft.Identity.Web to allocate a session key for you, or your own string
        /// if you want to associate the session with some information you have externally
        /// (for instance a Microsoft Graph hook identifier).
        /// </summary>
        public string? LongRunningWebApiSessionKey { get; set; }

        /// <summary>
        /// Value that can be used for <see cref="LongRunningWebApiSessionKey"/> so that
        /// MSAL.NET allocates the long running web api session key for the developer.
        /// </summary>
        public static string LongRunningWebApiSessionKeyAuto { get; set; } = "AllocateForMe";

        /// <summary>
        /// Clone the options (to be able to override them).
        /// </summary>
        /// <returns>A clone of the options.</returns>
        public AcquireTokenOptions Clone()
        {
            return new AcquireTokenOptions
            {
                Tenant = Tenant,
                UserFlow = UserFlow,
                AuthenticationOptionsName = AuthenticationOptionsName,
                CorrelationId = CorrelationId,
                ExtraQueryParameters = ExtraQueryParameters,
                ExtraHeadersParameters = ExtraHeadersParameters,
                ForceRefresh = ForceRefresh,
                Claims = Claims,
                PopPublicKey = PopPublicKey,
                LongRunningWebApiSessionKey = LongRunningWebApiSessionKey,
            };
        }
    }
}

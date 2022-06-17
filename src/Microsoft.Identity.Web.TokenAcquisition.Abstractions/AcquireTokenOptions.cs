// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;

namespace Microsoft.Identity.Web
{
    // TODO: split between TokenAcquirerOptions and AadTokenAcquirerOptions?

    /// <summary>
    /// Options passed-in to create the token acquisition object which calls into MSAL .NET.
    /// </summary>
    public class AcquireTokenOptions
    {
        /// <summary>
        /// Enables to override the tenant/account for the same identity. This is useful in multi-tenant apps 
        /// in the cases where a given account is a guest in other tenants, and you want to acquire tokens 
        /// for a specific tenant.
        /// </summary>
        /// <remarks>Can be the tenant ID or domain name.</remarks>
        public string? Tenant { get; set; }

        /// <summary>
        /// Uses a particular user flow (in the case of AzureAD B2C)
        /// </summary>
        public string? UserFlow { get; set; }

        /// <summary>
        /// Requires a particular (ASP.NET Core) authentication scheme / settings
        /// </summary>
        public string? AuthenticationScheme { get; set; }

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
        /// Sets Extra Header Parameters for the query string in the HTTP authentication request.
        /// </summary>
        public Dictionary<string, string>? ExtraHeadersParameters { get; set; }

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
                AuthenticationScheme = AuthenticationScheme,
                CorrelationId = CorrelationId,
                ExtraQueryParameters = ExtraQueryParameters,
                ForceRefresh = ForceRefresh,
                Claims = Claims,
                PopPublicKey = PopPublicKey,
                LongRunningWebApiSessionKey = LongRunningWebApiSessionKey,
            };
        }
    }
}

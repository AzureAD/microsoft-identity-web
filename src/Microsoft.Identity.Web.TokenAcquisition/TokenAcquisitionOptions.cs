// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Client.AppConfig;

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// Options passed-in to create the token acquisition object which calls into MSAL .NET.
    /// </summary>
    public class TokenAcquisitionOptions : AcquireTokenOptions
    {
        /// <summary>
        /// Modifies the token acquisition request so that the acquired token is a Proof of Possession token (PoP),
        /// rather than a Bearer token.
        /// PoP tokens are similar to Bearer tokens, but are bound to the HTTP request and to a cryptographic key,
        /// which MSAL can manage. See https://aka.ms/msal-net-pop.
        /// </summary>
        public PoPAuthenticationConfiguration? PoPConfiguration { get; set; }

        /// <summary>
        /// Cancellation token to be used when calling the token acquisition methods.
        /// </summary>
        public CancellationToken CancellationToken { get; set; } = CancellationToken.None;

        /// <summary>
        /// Clone the options (to be able to override them).
        /// </summary>
        /// <returns>A clone of the options.</returns>
        public new TokenAcquisitionOptions Clone()
        {
            return new TokenAcquisitionOptions
            {
                Tenant = Tenant,
                UserFlow = UserFlow,
                AuthenticationOptionsName = AuthenticationOptionsName,
                CorrelationId = CorrelationId,
                ExtraQueryParameters = ExtraQueryParameters,
                ForceRefresh = ForceRefresh,
                Claims = Claims,
                PoPConfiguration = PoPConfiguration,
                PopPublicKey = PopPublicKey,
                PopClaim = PopClaim,
                CancellationToken = CancellationToken,
                LongRunningWebApiSessionKey = LongRunningWebApiSessionKey,
                ManagedIdentity = ManagedIdentity,
                FmiPath = FmiPath
            };
        }
    }
}

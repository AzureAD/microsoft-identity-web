// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
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
        /// Creates a new instance of TokenAcquisitionOptions from an instance of the base class AcquireTokenOptions.
        /// </summary>
        /// <param name="acquireTokenOptions">The instance of the base class to clone</param>
        /// <returns>A new instance of TokenAquisitionOptions</returns>
        public static TokenAcquisitionOptions CloneFromBaseClass(AcquireTokenOptions? acquireTokenOptions)
        {
            if (acquireTokenOptions == null)
            {
                return new TokenAcquisitionOptions();
            }

            // alternate option 1: serialize the base class then deserialize it back to the derived class, this is probably not
            // ideal since it likely has small performance repercussions but it doesn't have to be done with json.

            // alternate option 2: add the two extra fields of PoPConfiguration and CancellationToken to the base class in
            // Abstractions and leave this child as a 1:1 wrapper with the base class to avoid breaking anyone.
            // This is my personal preference as it is most performant since no conversion would be needed. The drawback is I'd
            // need to replace all instances of TokenAcquisitionOptions with AcquireTokenOptions in the codebase.
            //      2.1 - I spoke with JM and he said this would be a longer conversation as he wants to deprecate
            //      TokenAcquisitionOptions entirely and drop the CancellationToken and PoPConfiguration fields as they
            //      apparently don't belong in AcquireTokenOptions.

            // This is how we have been doing it so far (in the DefaultAuthorizationHeaderProvider class) I put it here to make
            // it easier to find if we ever add more fields to the base class, but it will still require manual intervention to
            // add the new fields here.
            return new TokenAcquisitionOptions
            {
                AuthenticationOptionsName = acquireTokenOptions.AuthenticationOptionsName,
                Claims = acquireTokenOptions.Claims,
                CorrelationId = acquireTokenOptions.CorrelationId ?? Guid.Empty,
                ExtraQueryParameters = acquireTokenOptions.ExtraQueryParameters,
                ForceRefresh = acquireTokenOptions.ForceRefresh,
                LongRunningWebApiSessionKey = acquireTokenOptions.LongRunningWebApiSessionKey,
                ManagedIdentity = acquireTokenOptions.ManagedIdentity,
                Tenant = acquireTokenOptions.Tenant,
                UserFlow = acquireTokenOptions.UserFlow,
                PopPublicKey = acquireTokenOptions.PopPublicKey,
            };
        }

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
            };
        }
    }
}

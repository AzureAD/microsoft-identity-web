// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Client;

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// Enables optional key attestation on a managed identity mTLS proof-of-possession request.
    /// </summary>
    public interface IKeyAttestationProvider
    {
        /// <summary>
        /// Configures key attestation on the managed identity request.
        /// </summary>
        /// <param name="builder">The managed identity request builder.</param>
        /// <returns>The configured request builder.</returns>
        AcquireTokenForManagedIdentityParameterBuilder EnableAttestation(
            AcquireTokenForManagedIdentityParameterBuilder builder);
    }
}

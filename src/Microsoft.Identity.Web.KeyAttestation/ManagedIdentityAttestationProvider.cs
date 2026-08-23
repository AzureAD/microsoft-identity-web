// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.KeyAttestation;

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// Enables Credential Guard key attestation for managed identity mTLS proof-of-possession requests.
    /// </summary>
    internal sealed class ManagedIdentityAttestationProvider : IManagedIdentityAttestationProvider
    {
        /// <inheritdoc/>
        public AcquireTokenForManagedIdentityParameterBuilder EnableAttestation(
            AcquireTokenForManagedIdentityParameterBuilder builder)
        {
            ArgumentNullException.ThrowIfNull(builder);

            return builder.WithAttestationSupport();
        }
    }
}

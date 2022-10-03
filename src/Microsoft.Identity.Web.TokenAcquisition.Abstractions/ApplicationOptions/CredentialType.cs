// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Identity.Abstractions
{
    /// <summary>
    /// Crendential type
    /// </summary>
    public enum CredentialType
    {   
        /// <summary>
        /// Certificate.
        /// </summary>
        Certificate,

        /// <summary>
        /// (Client) secret.
        /// </summary>
        Secret, 
        
        /// <summary>
        /// Signed assertion.
        /// </summary>
        SignedAssertion
    }
}

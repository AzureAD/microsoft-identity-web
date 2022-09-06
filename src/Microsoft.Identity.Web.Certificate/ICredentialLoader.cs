// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// Interface to implement loading of credentials.
    /// </summary>
    public interface ICredentialLoader
    {
        /// <summary>
        /// Load the credential from the description, if needed.
        /// </summary>
        /// <param name="credentialDescription">Description of the credential.</param>
        void LoadIfNeeded(CredentialDescription credentialDescription);

        /// <summary>
        /// Loadable CredentialSource.
        /// </summary>
        CredentialSource CredentialSource { get; }
    }
}

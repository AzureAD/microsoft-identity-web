// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;

namespace Microsoft.Identity.Abstractions
{
    /// <summary>
    /// Loader of credentials.
    /// </summary>
    public interface ICredentialsLoader
    {
        /// <summary>
        /// Dictionary of credential loaders per credential source. The application can add more to 
        /// process additional credential sources (like dMSI).
        /// </summary>
        IDictionary<CredentialSource, ICredentialSourceLoader> CredentialSourceLoaders { get; }

        /// <summary>
        /// Load a given credential description, if needed.
        /// </summary>
        /// <param name="credentialDescription">Description of the credentials to load.</param>
        void LoadCredentialsIfNeeded(CredentialDescription credentialDescription);

        /// <summary>
        /// Load the first valid credential from the credentials description list.
        /// </summary>
        /// <param name="credentialDescriptions">Description of the credentials.</param>
        /// <returns>First valid credential description that could be loaded from the credential description list.
        /// <c>null</c> if none is valid.</returns>
        CredentialDescription? LoadFirstValidCredentials(IEnumerable<CredentialDescription> credentialDescriptions);

        /// <summary>
        /// Resets resettable credentials in the credential description list (for instance reset the 
        /// certificates so that they can be re-loaded again)
        /// Use, for example, before a retry.
        /// </summary>
        /// <param name="credentialDescriptions">Description of the credentials.</param>
        void ResetCredentials(IEnumerable<CredentialDescription> credentialDescriptions);
    }
}

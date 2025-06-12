// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading.Tasks;
using Microsoft.Identity.Abstractions;
using Microsoft.IdentityModel.Tokens;

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// Interface for loading signing credentials.
    /// </summary>
    public interface ISigningCredentialsLoader
    {
        /// <summary>
        /// Loads SigningCredentials from the credential description.
        /// </summary>
        /// <param name="credentialDescription">Credential description.</param>
        /// <param name="parameters">Optional parameters for loading credentials.</param>
        /// <returns>SigningCredentials if successful, null otherwise.</returns>
        Task<SigningCredentials?> LoadSigningCredentialsAsync(
            CredentialDescription credentialDescription,
            CredentialSourceLoaderParameters? parameters = null);
    }
}

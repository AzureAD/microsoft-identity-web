// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Abstractions;

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// Credential provider class. Provides access to configured credentials and observers.
    /// </summary>
    public interface ICredentialsProvider
    {
        /// <summary>
        /// Gets a credential to be used for authentication, based on the provided credential descriptions.
        /// The provider may choose to return a credential based on any of the provided descriptions, and is not required to return a credential for each description.
        /// The provider may also choose to return null, in which case the system will attempt to authenticate without client credentials, if applicable.
        /// </summary>
        /// <param name="credentialSourceLoaderParameters">Parameters to use for credential selection.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A matching and loaded credential, if any are applicable.</returns>
        public Task<CredentialDescription?> GetCredentialAsync(
            CredentialSourceLoaderParameters? credentialSourceLoaderParameters,
            CancellationToken cancellationToken);

        /// <summary>
        /// Gets a credential to be used for authentication, based on the provided credential descriptions.
        /// The provider may choose to return a credential based on any of the provided descriptions, and is not required to return a credential for each description.
        /// The provider may also choose to return null, in which case the system will attempt to authenticate without client credentials, if applicable.
        /// </summary>
        /// <param name="mergedOptions">The merged options to use to select a certificate from.</param>
        /// <param name="credentialSourceLoaderParameters">Parameters to use for credential selection.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A matching and loaded credential, if any are applicable.</returns>
        internal Task<CredentialDescription?> GetCredentialAsync(
            MergedOptions mergedOptions,
            CredentialSourceLoaderParameters? credentialSourceLoaderParameters,
            CancellationToken cancellationToken);

        /// <summary>
        /// Notifies that a certificate was used.
        /// </summary>
        /// <param name="authenticationScheme">The authentication scheme.</param>
        /// <param name="certificateDescription">The description of the certificate.</param>
        /// <param name="certificate">The certificate, distinct from the description in case the certificate value has changed.</param>
        /// <param name="successful">Whether he usage was successful or a failure.</param>
        /// <param name="exception">The exception, if applicable.</param>
        public void NotifyCertificateUsed(
            string authenticationScheme,
            CredentialDescription certificateDescription,
            X509Certificate2 certificate,
            bool successful,
            Exception? exception);
    }
}

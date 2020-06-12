// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// Interface to implement loading of a certificate.
    /// </summary>
    internal interface ICertificateLoader
    {
        /// <summary>
        /// Load the certificate from the description if needed.
        /// </summary>
        /// <param name="certificateDescription">Description of the certificate.</param>
        void LoadIfNeeded(CertificateDescription certificateDescription);
    }
}

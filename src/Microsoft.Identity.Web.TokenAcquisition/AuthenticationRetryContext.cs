// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Identity.Abstractions;

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// Describes a failed token acquisition or mTLS request for an authentication retry policy.
    /// </summary>
    public class AuthenticationRetryContext
    {
        /// <summary>
        /// Gets or sets the certificate used for authentication, or null for non-certificate credentials.
        /// </summary>
        public X509Certificate2? Certificate { get; set; }

        /// <summary>
        /// Gets or sets the credential description for the selected certificate.
        /// This is null when no certificate was selected or no matching description was found.
        /// </summary>
        public CredentialDescription? CredentialDescription { get; set; }

        /// <summary>
        /// Gets or sets the credential loading parameters, including the authentication protocol.
        /// </summary>
        public CredentialSourceLoaderParameters? CredentialSourceLoaderParameters { get; set; }

        /// <summary>
        /// Gets or sets the exception from token acquisition. This is null for a failed mTLS HTTP response.
        /// </summary>
        public Exception? ThrownException { get; set; }

        /// <summary>
        /// Gets or sets the failed mTLS HTTP response. This is null for token acquisition failures.
        /// </summary>
        /// <remarks>
        /// The caller owns the response. The policy must not dispose it.
        /// </remarks>
        public HttpResponseMessage? Response { get; set; }
    }
}

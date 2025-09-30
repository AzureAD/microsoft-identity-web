// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Identity.Abstractions;

// Types in the Microsoft.Identity.Web.Experimental namespace
// are meant to get feedback from the community on proposed features, and
// may be modified or removed in future releases without obeying to the
// semantic versionning.
namespace Microsoft.Identity.Web.Experimental
{
    /// <summary>
    /// Action of the token acquirer on the certificate.
    /// </summary>
    public enum CerticateObserverAction
    {
        /// <summary>
        /// The certificate was selected as a client certificate.
        /// </summary>
        Selected,

        /// <summary>
        /// The certificate was deselected as a client certificate. This
        /// happens when the STS does not accept the certificate any longer.
        /// </summary>
        Deselected,

        /// <summary>
        /// The certificate was successfully used.
        /// </summary>
        SuccessfullyUsed,
    }

    /// <summary>
    /// Event argument about the certificate consumption by the app
    /// </summary>
    public class CertificateChangeEventArg
    {
        /// <summary>
        /// Action on the certificate
        /// </summary>
        public CerticateObserverAction Action { get; set; }

        /// <summary>
        /// Certificate
        /// </summary>
        public X509Certificate2? Certificate { get; set; }

        /// <summary>
        /// Credential description
        /// </summary>
        public CredentialDescription? CredentialDescription { get; set; }

        /// <summary>
        /// Gets the exception thrown during the certificate selection or deselection.
        /// </summary>
        public Exception? ThrownException { get; set; } 
    }

    /// <summary>
    /// Interface that apps can implement to be notified when a certificate is selected or removed.
    /// </summary>
    public interface ICertificatesObserver
    {
        /// <summary>
        /// Called when a certificate is selected or removed.
        /// </summary>
        /// <param name="e"></param>
        public void OnClientCertificateChanged(CertificateChangeEventArg e);
    }
}

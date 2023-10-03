// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Security.Cryptography.X509Certificates;
using Microsoft.Identity.Abstractions;

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// Action of the app on the certificate.
    /// </summary>
    public enum CerticateObserverAction
    {
        /// <summary>
        /// The certificate was selected.
        /// </summary>
        Selected,

        /// <summary>
        /// The certificate was deselected.
        /// </summary>
        Deselected,
    }

    /// <summary>
    /// Event argument about the certificate consumption by the app
    /// </summary>
    public class CertificateChangeEventArg
    {
        /// <summary>
        /// Action of the certifcate
        /// </summary>
        public CerticateObserverAction Action { get; set; }

        /// <summary>
        /// Certificate
        /// </summary>
        public X509Certificate2 Certificate { get; set; }

        /// <summary>
        /// Credential description
        /// </summary>
        public CredentialDescription? CredentialDescription { get; set; }
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

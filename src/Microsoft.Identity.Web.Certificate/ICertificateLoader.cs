// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// Interface to implement loading of a certificate.
    /// Only use when loading a certificate from a daemon application, or an ASP NET app, using MSAL .NET directly.
    /// For an ASP NET Core app, <b>Microsoft Identity Web will handle the certificate loading</b> for you.
    /// <example><code>
    /// IConfidentialClientApplication app;
    /// ICertificateLoader certificateLoader = new DefaultCertificateLoader();
    ///     certificateLoader.LoadIfNeeded(config.CertificateDescription);
    ///
    ///    app = ConfidentialClientApplicationBuilder.Create(config.ClientId)
    ///           .WithCertificate(config.CertificateDescription.Certificate)
    ///           .WithAuthority(new Uri(config.Authority))
    ///           .Build();
    /// </code></example>
    /// </summary>
    public interface ICertificateLoader
    {
        /// <summary>
        /// Load the certificate from the description, if needed.
        /// </summary>
        /// <param name="certificateDescription">Description of the certificate.</param>
        void LoadIfNeeded(CertificateDescription certificateDescription);
    }
}

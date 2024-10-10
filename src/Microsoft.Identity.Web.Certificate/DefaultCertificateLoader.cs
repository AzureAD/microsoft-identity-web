// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Abstractions;

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// Certificate Loader.
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
    public class DefaultCertificateLoader : DefaultCredentialsLoader, ICertificateLoader
    {
        /// <summary>
        /// Constructor with a logger.
        /// </summary>
        /// <param name="logger"></param>
        public DefaultCertificateLoader(ILogger<DefaultCertificateLoader>? logger) : base(logger)
        {
        }

        /// <summary>
        /// Default constuctor.
        /// </summary>
        public DefaultCertificateLoader() : this(null)
        {
        }

        /// <summary>
        ///  This default is overridable at the level of the credential description (for the certificate from KeyVault).
        /// </summary>
        public static string? UserAssignedManagedIdentityClientId
        {
            get
            {
                return KeyVaultCertificateLoader.UserAssignedManagedIdentityClientId;
            }
            set
            {
                KeyVaultCertificateLoader.UserAssignedManagedIdentityClientId = value;
            }
        }


        /// <summary>
        /// Load the first certificate from the certificate description list.
        /// </summary>
        /// <param name="certificateDescriptions">Description of the certificates.</param>
        /// <returns>First certificate in the certificate description list.</returns>
        public static X509Certificate2? LoadFirstCertificate(IEnumerable<CertificateDescription> certificateDescriptions)
        {
            DefaultCertificateLoader defaultCertificateLoader = new(null);
            CertificateDescription? certDescription = certificateDescriptions.FirstOrDefault(c =>
            {
                defaultCertificateLoader.LoadCredentialsIfNeededAsync(c).GetAwaiter().GetResult();
                return c.Certificate != null;
            });

            return certDescription?.Certificate;
        }
        
        /// <summary>
        /// Load the first certificate from the certificate description list.
        /// </summary>
        /// <param name="certificateDescriptions">Description of the certificates.</param>
        /// <returns>First certificate in the certificate description list.</returns>
        public static async Task<X509Certificate2?> LoadFirstCertificateAsync(IEnumerable<CertificateDescription> certificateDescriptions)
        {
            DefaultCertificateLoader defaultCertificateLoader = new(null);
            CertificateDescription? certDescription = null;
            foreach (var c in certificateDescriptions)
            {
                await defaultCertificateLoader.LoadCredentialsIfNeededAsync(c).ConfigureAwait(false);
                if (c.Certificate != null)
                {
                    certDescription = c;
                    break;
                }
            };

            return certDescription?.Certificate;
        }


        /// <summary>
        /// Load all the certificates from the certificate description list.
        /// </summary>
        /// <param name="certificateDescriptions">Description of the certificates.</param>
        /// <returns>All the certificates in the certificate description list.</returns>
        public static IEnumerable<X509Certificate2?> LoadAllCertificates(IEnumerable<CertificateDescription> certificateDescriptions)
        {
            DefaultCertificateLoader defaultCertificateLoader = new(null);
            return defaultCertificateLoader.LoadCertificates(certificateDescriptions);
        }

        /// <summary>
        /// Load the certificates from the certificate description list.
        /// </summary>
        /// <param name="certificateDescriptions"></param>
        /// <returns>a collection of certificates</returns>
        private IEnumerable<X509Certificate2?> LoadCertificates(IEnumerable<CertificateDescription> certificateDescriptions)
        {
            if (certificateDescriptions != null)
            {
                foreach (var certDescription in certificateDescriptions)
                {
                    _ = LoadCredentialsIfNeededAsync(certDescription);
                    if (certDescription.Certificate != null)
                    {
                        yield return certDescription.Certificate;
                    }
                }
            }
        }

        /// <summary>
        /// Resets all the certificates in the certificate description list.
        /// Use, for example, before a retry.
        /// </summary>
        /// <param name="certificateDescriptions">Description of the certificates.</param>
        public static void ResetCertificates(IEnumerable<CertificateDescription>? certificateDescriptions)
        {
            if (certificateDescriptions != null)
            {
                foreach (var cert in certificateDescriptions)
                {
                    cert.Certificate = null;
                    cert.CachedValue = null;
                }
            }
        }

        /// <summary>
        /// Resets all the certificates in the certificate description list.
        /// Use, for example, before a retry.
        /// </summary>
        /// <param name="credentialDescription">Description of the certificates.</param>
        public static void ResetCertificates(IEnumerable<CredentialDescription>? credentialDescription)
        {
            if (credentialDescription != null)
            {
                foreach (var cert in credentialDescription.Where(c => c.Certificate != null))
                {
                    cert.Certificate = null;
                    cert.CachedValue = null;
                }
            }
        }

        /// <summary>
        /// Load the certificate from the description, if needed.
        /// </summary>
        /// <param name="certificateDescription">Description of the certificate.</param>
        public void LoadIfNeeded(CertificateDescription certificateDescription)
        {
            LoadCredentialsIfNeededAsync(certificateDescription).GetAwaiter().GetResult();
        }
        
        /// <summary>
        /// Load the certificate from the description, if needed.
        /// </summary>
        /// <param name="certificateDescription">Description of the certificate.</param>
        public async Task LoadIfNeededAsync(CertificateDescription certificateDescription)
        {
            await LoadCredentialsIfNeededAsync(certificateDescription);
        }
    }
}

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// Class used to handle gracefully the obsolete token decyrption certificate parameter in
    /// deprecated AddProtectedWebApi methods.
    /// </summary>
    internal static class ObsoleteLegacyTokenDecryptCertificateParameter
    {
        internal static void HandleLegacyTokenDecryptionCertificateParameter(MicrosoftIdentityOptions options, Action<MicrosoftIdentityOptions> configureMicrosoftIdentityOptions, X509Certificate2 tokenDecryptionCertificate)
        {
            // Case where a legacy tokenDecryptionCertificate was passed. We then replace
            // the delegate called by the developer by a delegate which calls the delegate
            // of the developer and insert the certificate in the TokenDecryptionCertificates
            if (tokenDecryptionCertificate != null)
            {
                // Call the method that the developer provided to setup the options
                configureMicrosoftIdentityOptions(options);

                // Prepare a list and add the tokenDecryptionCertificate
                List<CertificateDescription> newCertificateDescriptions = new List<CertificateDescription>
                    {
                        CertificateDescription.FromCertificate(tokenDecryptionCertificate),
                    };

                // Add as well the token validation certificate descriptions in the options if there are any
                if (options.TokenDecryptionCertificates != null)
                {
                    newCertificateDescriptions.AddRange(options.TokenDecryptionCertificates);
                }
            }
            else
            {
                // just call the method that the developer provided to setup the options
                configureMicrosoftIdentityOptions(options);
            }
        }
    }
}

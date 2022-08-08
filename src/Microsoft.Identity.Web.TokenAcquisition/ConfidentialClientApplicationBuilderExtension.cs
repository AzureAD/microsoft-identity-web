// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Identity.Client;

namespace Microsoft.Identity.Web
{
    internal static class ConfidentialClientApplicationBuilderExtension
    {
        public static ConfidentialClientApplicationBuilder WithClientCredentials(this ConfidentialClientApplicationBuilder builder, IEnumerable<CredentialDescription> clientCredentials)
        {
            foreach (var credential in clientCredentials)
            {
                // TODO, use the credential loader
                if (credential.SourceType == CredentialSource.SignedAssertionFromManagedIdentity)
                {
                    if (credential.CachedValue == null)
                    {
                        credential.CachedValue = new ManagedIdentityClientAssertion(credential.ManagedIdentityClientId);
                    }
                    return builder.WithClientAssertion((credential.CachedValue as ManagedIdentityClientAssertion)!.GetSignedAssertion);
                }

                if (credential.CredentialType == CredentialType.Certificate)
                {
                    var certs = clientCredentials.Where(c => c.CredentialType == CredentialType.Certificate);
                    if (certs != null && certs.Any())
                    {
                        var clientCertificates = certs.Select(c => new CertificateDescription(c));
                        credential.CachedValue = DefaultCertificateLoader.LoadFirstCertificate(clientCertificates);
                        X509Certificate2? certificate = DefaultCertificateLoader.LoadFirstCertificate(clientCertificates);
                        if (certificate == null)
                        {
                            throw new ArgumentException(
                                IDWebErrorMessage.ClientCertificatesHaveExpiredOrCannotBeLoaded,
                                nameof(clientCredentials));
                        }
                        return builder.WithCertificate(certificate);
                    }
                }

                if (credential.CredentialType == CredentialType.Secret)
                {
                    return builder.WithClientSecret(credential.ClientSecret);
                }
            }

            return builder;
        }
    }
}

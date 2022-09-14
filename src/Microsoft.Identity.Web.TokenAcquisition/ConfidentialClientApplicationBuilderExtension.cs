// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using Azure.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Client;

namespace Microsoft.Identity.Web
{
    internal static class ConfidentialClientApplicationBuilderExtension
    {
        public static ConfidentialClientApplicationBuilder WithClientCredentials(this ConfidentialClientApplicationBuilder builder, IEnumerable<CredentialDescription> clientCredentials, ILogger logger)
        {
            foreach (var credential in clientCredentials)
            {
                if (!credential.Skip)
                {
                    // TODO, use the credential loader
                    if (credential.SourceType == CredentialSource.SignedAssertionFromManagedIdentity)
                    {
                        ManagedIdentityClientAssertion? managedIdentityClientAssertion = credential.CachedValue as ManagedIdentityClientAssertion;
                        if (credential.CachedValue == null)
                        {
                            managedIdentityClientAssertion = new ManagedIdentityClientAssertion(credential.ManagedIdentityClientId);
                            credential.CachedValue = managedIdentityClientAssertion;
                        }
                        try
                        {
                            // Given that managed identity can be not available locally, we need to try to get a
                            // signed assertion, and if it fails, move to the next credentials
                            managedIdentityClientAssertion!.GetSignedAssertion(CancellationToken.None).GetAwaiter().GetResult();
                        }
                        catch (AuthenticationFailedException ex)
                        {
                            credential.Skip = true;
                            logger.LogInformation($"Not using Managed identity for client credentials + {ex.Message}. ");
                            continue;
                        }
                        logger.LogInformation("Using Managed identity as client credentials. ");
                        return builder.WithClientAssertion((credential.CachedValue as ManagedIdentityClientAssertion)!.GetSignedAssertion);
                    }
                    if (credential.SourceType == CredentialSource.SignedAssertionFilePath)
                    {
                        if (credential.CachedValue == null)
                        {
                            credential.CachedValue = new PodIdentityClientAssertion(credential.SignedAssertionFileDiskPath);
                        }
                        logger.LogInformation($"Using Pod identity file {credential.SignedAssertionFileDiskPath} as client credentials. ");
                        return builder.WithClientAssertion((credential.CachedValue as PodIdentityClientAssertion)!.GetSignedAssertion);
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
                            logger.LogInformation($"Using certificate Thumbprint={certificate.Thumbprint} as client credentials. ");
                            return builder.WithCertificate(certificate);
                        }
                    }

                    if (credential.CredentialType == CredentialType.Secret)
                    {
                        return builder.WithClientSecret(credential.ClientSecret);
                    }
                }
            }

            return builder;
        }
    }
}

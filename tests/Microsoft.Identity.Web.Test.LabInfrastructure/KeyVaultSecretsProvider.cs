// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Security.Cryptography.X509Certificates;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Web.Test.Common;

namespace Microsoft.Identity.Web.Test.LabInfrastructure
{
    public class KeyVaultSecretsProvider
    {
        public KeyVaultSecretsProvider()
        {
            DefaultCertificateLoader defaultcertloader = new();
            CredentialDescription credentialDescription= new()
            {
                SourceType = CredentialSource.StoreWithThumbprint,
                CertificateStorePath = "LocalMachine/My",
                CertificateThumbprint = "444B697D869032F29F9A162D711AF3E2791AD748"

            };
            defaultcertloader.LoadCredentialsIfNeededAsync(credentialDescription).GetAwaiter().GetResult();
            Certificate = credentialDescription.Certificate;
        }

        public X509Certificate2 Certificate  { get; set; }

        public KeyVaultSecret GetKeyVaultSecret()
        {
            Uri keyVaultUri = new Uri(TestConstants.BuildAutomationKeyVaultName);
            ClientCertificateCredential clientCertificateCredential = new(TestConstants.ConfidentialClientLabTenant, TestConstants.LabClientId, Certificate);
            SecretClient secretClient = new(keyVaultUri, clientCertificateCredential);

            return secretClient.GetSecret(TestConstants.AzureADIdentityDivisionTestAgentSecret);
        }

        public KeyVaultSecret GetMsidLabSecret(string secretName)
        { 
            ClientCertificateCredential clientCertificateCredential = new(TestConstants.ConfidentialClientLabTenant, TestConstants.LabClientId, Certificate);
            SecretClient secretClient = new(new Uri(TestConstants.MSIDLabLabKeyVaultName), clientCertificateCredential);
            return secretClient.GetSecret(secretName);
        }
    }
}

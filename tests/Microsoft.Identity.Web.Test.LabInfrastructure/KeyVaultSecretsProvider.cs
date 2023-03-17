// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Identity.Web.Test.Common;

namespace Microsoft.Identity.Web.Test.LabInfrastructure
{
    public class KeyVaultSecretsProvider
    {
        public KeyVaultSecret GetKeyVaultSecret()
        {
            Uri keyVaultUri = new Uri(TestConstants.BuildAutomationKeyVaultName);
            DefaultAzureCredentialOptions options = new()
            {
                ManagedIdentityClientId = TestConstants.LabClientId,
            };
            DefaultAzureCredential credential = new(options);
            SecretClient secretClient = new(keyVaultUri, credential);

            return secretClient.GetSecret(TestConstants.AzureADIdentityDivisionTestAgentSecret);
        }

        public KeyVaultSecret GetMsidLabSecret(string secretName)
        {
            DefaultAzureCredentialOptions options = new()
            {
                ManagedIdentityClientId = TestConstants.LabClientId,
            };
            DefaultAzureCredential credential = new(options);
            SecretClient secretClient = new(new Uri(TestConstants.MSIDLabLabKeyVaultName), credential);
            return secretClient.GetSecret(secretName);
        }
    }
}

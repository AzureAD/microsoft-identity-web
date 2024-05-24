// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading;
using Microsoft.Identity.Abstractions;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Microsoft.Identity.Web
{
    internal class SignedAssertionFilePathCredentialsLoader : ICredentialSourceLoader
    {
        ILogger? _logger;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="logger">Optional logger.</param>
        public SignedAssertionFilePathCredentialsLoader(ILogger? logger)
        {
            _logger = logger;
        }
        public CredentialSource CredentialSource => CredentialSource.SignedAssertionFilePath;

        public async Task LoadIfNeededAsync(CredentialDescription credentialDescription, CredentialSourceLoaderParameters? credentialSourceLoaderParameters)
        {
            if (credentialDescription.SourceType == CredentialSource.SignedAssertionFilePath)
            {
                AzureIdentityForKubernetesClientAssertion? signedAssertion = credentialDescription.CachedValue as AzureIdentityForKubernetesClientAssertion;
                if (credentialDescription.CachedValue == null)
                {
                    signedAssertion = new AzureIdentityForKubernetesClientAssertion(credentialDescription.SignedAssertionFileDiskPath, _logger);
                }
                try
                {
                    // Given that managed identity can be not available locally, we need to try to get a
                    // signed assertion, and if it fails, move to the next credentials
                    _= await signedAssertion!.GetSignedAssertionAsync(null);
                    credentialDescription.CachedValue = signedAssertion;
                }
                catch (Exception)
                {
                    credentialDescription.Skip = true;
                }
            }
        }
    }
}

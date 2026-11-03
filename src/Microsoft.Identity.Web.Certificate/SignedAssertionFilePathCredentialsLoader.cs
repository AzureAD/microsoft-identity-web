// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

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
                // Cache only after the assertion file can be read and parsed.
                _ = await signedAssertion!.GetSignedAssertionAsync(null).ConfigureAwait(false);
                credentialDescription.CachedValue = signedAssertion;
            }
        }
    }
}

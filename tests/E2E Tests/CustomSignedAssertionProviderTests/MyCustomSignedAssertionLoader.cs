// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;
using Microsoft.Identity.Abstractions;

namespace CustomSignedAssertionProviderTests
{
    internal class MyCustomSignedAssertionLoader : ICustomSignedAssertionProvider
    {
        private readonly ILogger<MyCustomSignedAssertionLoader> _logger;

        public MyCustomSignedAssertionLoader(ILogger<MyCustomSignedAssertionLoader> logger)
        {
            _logger = logger;
        }

        public CredentialSource CredentialSource => CredentialSource.CustomSignedAssertion;

        public string Name => "MyCustomExtension";

        public async Task LoadIfNeededAsync(CredentialDescription credentialDescription, CredentialSourceLoaderParameters? parameters = null)
        {
            MyCustomSignedAssertionProvider? signedAssertion = credentialDescription.CachedValue as MyCustomSignedAssertionProvider;
            if (credentialDescription.CachedValue == null)
            {
                signedAssertion = new MyCustomSignedAssertionProvider(credentialDescription.CustomSignedAssertionProviderData);
            }

            try
            {
                // Try to get a signed assertion, and if it fails, move to the next credentials
                _ = await signedAssertion!.GetSignedAssertionAsync(null);
                credentialDescription.CachedValue = signedAssertion;
            }
            catch (Exception ex)
            {
                _logger.LogError(22, ex.Message);
                _logger.LogError(42, "Failed to get signed assertion.");
                credentialDescription.Skip = true;
                throw;
            }
        }
    }
}

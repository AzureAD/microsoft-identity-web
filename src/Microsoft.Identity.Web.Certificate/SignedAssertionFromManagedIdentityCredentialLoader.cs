// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Client;

namespace Microsoft.Identity.Web
{
    internal class SignedAssertionFromManagedIdentityCredentialLoader : ICredentialSourceLoader
    {
        private readonly ILogger<DefaultCredentialsLoader> _logger;

        public SignedAssertionFromManagedIdentityCredentialLoader(ILogger<DefaultCredentialsLoader> logger)
        {
            _logger = logger;
        }

        public CredentialSource CredentialSource => CredentialSource.SignedAssertionFromManagedIdentity;

        public async Task LoadIfNeededAsync(CredentialDescription credentialDescription, CredentialSourceLoaderParameters? credentialSourceLoaderParameters)
        {
            if (credentialDescription.SourceType == CredentialSource.SignedAssertionFromManagedIdentity)
            {
                ManagedIdentityClientAssertion? managedIdentityClientAssertion = credentialDescription.CachedValue as ManagedIdentityClientAssertion;
                if (credentialDescription.CachedValue == null)
                {
                    managedIdentityClientAssertion = new ManagedIdentityClientAssertion(
                        credentialDescription.ManagedIdentityClientId, 
                        credentialDescription.TokenExchangeUrl,
                        _logger);
                }
                // Probe availability for local fallback. Cache only success so a failed load can be retried.
                AssertionRequestOptions? assertionOptions =
                    credentialSourceLoaderParameters is IClientAssertionEnrichmentOptions { OtelTagsEnricher: { } enricher }
                        ? new AssertionRequestOptions { OtelTagsEnricher = enricher }
                        : null;
                _ = await managedIdentityClientAssertion!.GetSignedAssertionAsync(assertionOptions).ConfigureAwait(false);
                credentialDescription.CachedValue = managedIdentityClientAssertion;
            }
        }
    }
}

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Client;

namespace Microsoft.Identity.Web
{
    internal sealed class KeyAttestedManagedIdentityCredentialLoader : ICredentialSourceLoader
    {
        private readonly IManagedIdentityAttestationProvider _attestationProvider;
        private readonly ILogger<KeyAttestedManagedIdentityCredentialLoader> _logger;

        public KeyAttestedManagedIdentityCredentialLoader(
            IManagedIdentityAttestationProvider attestationProvider,
            ILogger<KeyAttestedManagedIdentityCredentialLoader> logger)
        {
            _attestationProvider = attestationProvider;
            _logger = logger;
        }

        public CredentialSource CredentialSource => CredentialSource.SignedAssertionFromManagedIdentity;

        public async Task LoadIfNeededAsync(
            CredentialDescription credentialDescription,
            CredentialSourceLoaderParameters? credentialSourceLoaderParameters)
        {
            if (credentialDescription.SourceType != CredentialSource.SignedAssertionFromManagedIdentity)
            {
                return;
            }

            ManagedIdentityClientAssertion? managedIdentityClientAssertion =
                credentialDescription.CachedValue as ManagedIdentityClientAssertion;

            if (credentialDescription.CachedValue is null)
            {
                managedIdentityClientAssertion = new ManagedIdentityClientAssertion(
                    credentialDescription.ManagedIdentityClientId,
                    credentialDescription.TokenExchangeUrl,
                    _logger,
                    _attestationProvider);
            }

            try
            {
                _ = await managedIdentityClientAssertion!
                    .GetSignedAssertionAsync(null)
                    .ConfigureAwait(false);
                credentialDescription.CachedValue = managedIdentityClientAssertion;
            }
            catch (MsalServiceException)
            {
                credentialDescription.Skip = true;
                throw;
            }
        }
    }
}

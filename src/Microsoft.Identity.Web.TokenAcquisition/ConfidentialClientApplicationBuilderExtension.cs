// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.AppConfig;
using static Microsoft.Identity.Web.TokenAcquisition;

namespace Microsoft.Identity.Web
{
    internal static partial class ConfidentialClientApplicationBuilderExtension
    {
        public static async Task<ConfidentialClientApplicationBuilder> WithClientCredentialsAsync(
            this ConfidentialClientApplicationBuilder builder,
            MergedOptions mergedOptions,
            ICredentialsProvider credentialsProvider,
            CredentialSourceLoaderParameters? credentialSourceLoaderParameters,
            bool isTokenBinding,
            CancellationToken cancellationToken = default)
        {
            var credential = await credentialsProvider.GetCredentialAsync(
                    mergedOptions,
                    credentialSourceLoaderParameters,
                    cancellationToken)
                .ConfigureAwait(false);

            if (isTokenBinding)
            {
                if (credential?.Certificate != null)
                {
                    return builder.WithCertificate(credential.Certificate);
                }

                // CachedValue holds the concrete provider instance that the credential loader
                // created and cached on the CredentialDescription. Providers opt into mTLS PoP
                // by overriding ClientAssertionProviderBase.SupportsTokenBinding and returning
                // a ClientSignedAssertion (assertion + binding certificate) from
                // GetSignedAssertionWithBindingAsync. Today only ManagedIdentityClientAssertion
                // ships with that capability; OIDC IdP / Kubernetes federation providers do not.
                if (credential?.CredentialType == CredentialType.SignedAssertion
                    && credential.CachedValue is ClientAssertionProviderBase bindingProvider
                    && bindingProvider.SupportsTokenBinding)
                {
                    return builder.WithClientAssertion(
                        async (options, ct) =>
                            (await bindingProvider
                                .GetSignedAssertionWithBindingAsync(options, ct)
                                .ConfigureAwait(false))!);
                }

                throw new InvalidOperationException(IDWebErrorMessage.MissingTokenBindingCertificate);
            }

            if (credential == null)
            {
                return builder;
            }

            switch (credential.CredentialType)
            {
                case CredentialType.SignedAssertion:
                    return builder.WithClientAssertion((credential.CachedValue as ClientAssertionProviderBase)!.GetSignedAssertionAsync);
                case CredentialType.Certificate:
                    if (credential.UseBoundCredential && credential.Certificate is not null)
                    {
                        return builder.WithCertificate(
                            credential.Certificate,
                            new CertificateOptions { SendCertificateOverMtls = true });
                    }

                    return builder.WithCertificate(credential.Certificate);
                case CredentialType.Secret:
                    return builder.WithClientSecret(credential.ClientSecret);
                default:
                    throw new NotImplementedException();

            }
        }
    }
}

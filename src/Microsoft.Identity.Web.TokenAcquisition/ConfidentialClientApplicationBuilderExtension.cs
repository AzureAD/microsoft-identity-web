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
                // GetSignedAssertionWithBindingAsync. ManagedIdentityClientAssertion and the OIDC
                // IdP federation provider ship with that capability; Kubernetes/file federation
                // providers do not.
                if (credential?.CredentialType == CredentialType.SignedAssertion
                    && credential.CachedValue is ClientAssertionProviderBase bindingProvider
                    && bindingProvider.SupportsTokenBinding)
                {
                    return WithBoundClientAssertion(builder, bindingProvider);
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
                    var signedAssertionProvider = credential.CachedValue as ClientAssertionProviderBase;

                    // UseBoundCredential on a signed-assertion credential selects the bound
                    // callback (assertion + binding certificate) while the final token remains
                    // Bearer (authentication uses jwt-pop over mTLS). This is generic: it works
                    // for any binding-capable ClientAssertionProviderBase (OIDC IdP, managed
                    // identity, future providers) without special-casing a concrete type.
                    if (credential.UseBoundCredential)
                    {
                        if (signedAssertionProvider is null || !signedAssertionProvider.SupportsTokenBinding)
                        {
                            throw new InvalidOperationException(IDWebErrorMessage.MissingTokenBindingCertificate);
                        }

                        return WithBoundClientAssertion(builder, signedAssertionProvider);
                    }

                    return builder.WithClientAssertion(signedAssertionProvider!.GetSignedAssertionAsync);
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

        /// <summary>
        /// Wires an MSAL <see cref="ClientSignedAssertion"/> callback (assertion + binding
        /// certificate) from a binding-capable <see cref="ClientAssertionProviderBase"/>. Shared
        /// by the <c>isTokenBinding</c> (final mtls_pop) and <c>UseBoundCredential</c>
        /// (final Bearer, jwt-pop over mTLS) paths. A null result from the provider surfaces the
        /// clear IDW10115 error instead of a NullReferenceException.
        /// </summary>
        private static ConfidentialClientApplicationBuilder WithBoundClientAssertion(
            ConfidentialClientApplicationBuilder builder,
            ClientAssertionProviderBase bindingProvider)
        {
            return builder.WithClientAssertion(
                async (options, cancellationToken) =>
                {
                    ClientSignedAssertion? result = await bindingProvider
                        .GetSignedAssertionWithBindingAsync(options, cancellationToken)
                        .ConfigureAwait(false);

                    // Fail fast with the clear IDW10115 error when a binding-capable provider returns a
                    // null result, or one missing its assertion or binding certificate, so malformed
                    // (e.g. third-party) providers surface an actionable error instead of a later, more
                    // opaque MSAL failure.
                    if (result is null
                        || string.IsNullOrEmpty(result.Assertion)
                        || result.TokenBindingCertificate is null)
                    {
                        throw new InvalidOperationException(IDWebErrorMessage.MissingTokenBindingCertificate);
                    }

                    return result;
                });
        }
    }
}

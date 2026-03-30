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
                    return builder.WithCertificate(credential.Certificate);
                case CredentialType.Secret:
                    return builder.WithClientSecret(credential.ClientSecret);
                default:
                    throw new NotImplementedException();

            }
        }
    }
}

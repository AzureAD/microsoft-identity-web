// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Client;

namespace Microsoft.Identity.Web
{
    internal static partial class ConfidentialClientApplicationBuilderExtension
    {
        [Obsolete(IDWebErrorMessage.WithClientCredentialsIsObsolete, false)]
        public static ConfidentialClientApplicationBuilder WithClientCredentials(
            this ConfidentialClientApplicationBuilder builder,
            IEnumerable<CredentialDescription> clientCredentials,
            ILogger logger,
            ICredentialsLoader credentialsLoader,
            CredentialSourceLoaderParameters credentialSourceLoaderParameters)
        {
            return WithClientCredentialsAsync(builder, clientCredentials, logger, credentialsLoader,
                credentialSourceLoaderParameters).GetAwaiter().GetResult();
        }

        /// <summary>
        /// This tries to resolve ID.Web credentials to MSAL credentials. It needs to resolve it at the point of building the ConfidentialClientApplicationBuilder.
        /// This means that it will **eagerly** try to load a credential, and if it fails, each credential is responsible for setting the Skip flag.
        /// Eagerly means that for SignedAssertions, the options are not set, because it's not MSAL that fires the callback.
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="clientCredentials"></param>
        /// <param name="logger"></param>
        /// <param name="credentialsLoader"></param>
        /// <param name="credentialSourceLoaderParameters"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public static async Task<ConfidentialClientApplicationBuilder> WithClientCredentialsAsync(
            this ConfidentialClientApplicationBuilder builder,
            IEnumerable<CredentialDescription> clientCredentials,
            ILogger logger,
            ICredentialsLoader credentialsLoader,
            CredentialSourceLoaderParameters? credentialSourceLoaderParameters)
        {
            var credential = await LoadCredentialForMsalOrFailAsync(
                    clientCredentials,
                    logger,
                    credentialsLoader,
                    credentialSourceLoaderParameters)
                .ConfigureAwait(false);

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

        internal /* for test */ async static Task<CredentialDescription?> LoadCredentialForMsalOrFailAsync(
            IEnumerable<CredentialDescription> clientCredentials,
            ILogger logger,
            ICredentialsLoader credentialsLoader,
            CredentialSourceLoaderParameters? credentialSourceLoaderParameters)
        {
            string errorMessage = "\n";

            foreach (CredentialDescription credential in clientCredentials)
            {
                Logger.AttemptToLoadCredentials(logger, credential);

                if (!credential.Skip)
                {
                    // Load the credentials and record error messages in case we need to fail at the end
                    try
                    {
                        
                        await credentialsLoader.LoadCredentialsIfNeededAsync(credential, credentialSourceLoaderParameters);
                    }
                    catch (Exception ex)
                    {
                        Logger.AttemptToLoadCredentialsFailed(logger, credential, ex);
                        errorMessage += $"Credential {credential.Id} failed because: {ex} \n";
                    }


                    if (credential.CredentialType == CredentialType.SignedAssertion)
                    {
                        if (credential.SourceType == CredentialSource.SignedAssertionFromManagedIdentity)
                        {
                            if (credential.Skip)
                            {
                                Logger.NotUsingManagedIdentity(logger, errorMessage);
                            }
                            else
                            {
                                Logger.UsingManagedIdentity(logger);
                                return credential;
                            }
                        }
                        if (credential.SourceType == CredentialSource.SignedAssertionFilePath)
                        {
                            if (!credential.Skip)
                            {
                                Logger.UsingPodIdentityFile(logger, credential.SignedAssertionFileDiskPath ?? "not found");
                                return credential;
                            }
                        }
                        if (credential.SourceType == CredentialSource.SignedAssertionFromVault)
                        {
                            if (!credential.Skip)
                            {
                                Logger.UsingSignedAssertionFromVault(logger, credential.KeyVaultUrl ?? "undefined");
                                return credential;
                            }
                        }
                        if (credential.SourceType == CredentialSource.CustomSignedAssertion)
                        {
                            if (!credential.Skip)
                            {
                                Logger.UsingSignedAssertionFromCustomProvider(logger, credential.CustomSignedAssertionProviderName ?? "undefined");
                                return credential;
                            }
                        }
                    }

                    if (credential.CredentialType == CredentialType.Certificate)
                    {
                        if (credential.Certificate != null)
                        {
                            Logger.UsingCertThumbprint(logger, credential.Certificate.Thumbprint);
                            return credential;
                        }
                    }

                    if (credential.CredentialType == CredentialType.Secret)
                    {
                        return credential;
                    }
                }
            }

            if (clientCredentials.Any(c => c.CredentialType == CredentialType.Certificate || c.CredentialType == CredentialType.SignedAssertion))
            {
                throw new ArgumentException(
                   IDWebErrorMessage.ClientCertificatesHaveExpiredOrCannotBeLoaded + errorMessage,
                   nameof(clientCredentials));
            }

            logger.LogInformation($"No client credential could be used. Secret may have been defined elsewhere. " +
                $"Count {(clientCredentials != null ? clientCredentials.Count() : 0)} ");

            return null;
        }
    }
}

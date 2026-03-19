// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Web.Experimental;

namespace Microsoft.Identity.Web
{
    internal partial class CredentialsProvider : ICredentialsProvider
    {
        private readonly ILogger _logger;
        private readonly ITokenAcquisitionHost? _tokenHost;
        private readonly ICredentialsLoader _credentialsLoader;
        private readonly IReadOnlyList<ICertificatesObserver> _certificatesObservers;

        public CredentialsProvider(
            ILogger<CredentialsProvider> logger,
            ICredentialsLoader credentialsLoader,
            IEnumerable<ICertificatesObserver> certificatesObservers,
            ITokenAcquisitionHost? tokenHost = null)
        {
            _logger = logger;
            _tokenHost = tokenHost;
            _credentialsLoader = credentialsLoader;
            _certificatesObservers = [.. certificatesObservers];
        }

        public Task<CredentialDescription?> GetCredentialAsync(
            CredentialSourceLoaderParameters? credentialSourceLoaderParameters,
            CancellationToken cancellationToken = default)
        {
            if (_tokenHost == null)
            {
                throw new InvalidOperationException("Token acquisition host is not available.");
            }

            return GetCredentialAsync(
                _tokenHost.GetOptions(null, out string effectiveScheme) ?? throw new InvalidOperationException($"Unable to load client credentials for scheme '{effectiveScheme}'."),
                credentialSourceLoaderParameters,
                cancellationToken);
        }

        public async Task<CredentialDescription?> GetCredentialAsync(
            MergedOptions options,
            CredentialSourceLoaderParameters? credentialSourceLoaderParameters,
            CancellationToken cancellationToken = default)
        {
            IEnumerable<CredentialDescription> clientCredentials = options.ClientCredentials ?? [];

            string errorMessage = "\n";

            foreach (CredentialDescription credential in clientCredentials)
            {
                LogMessages.AttemptToLoadCredentials(_logger, credential);

                if (!credential.Skip)
                {
                    // Load the credentials and record error messages in case we need to fail at the end
                    try
                    {

                        await _credentialsLoader.LoadCredentialsIfNeededAsync(credential, credentialSourceLoaderParameters);
                    }
                    catch (Exception ex)
                    {
                        LogMessages.AttemptToLoadCredentialsFailed(_logger, credential, ex);
                        errorMessage += $"Credential {credential.Id} failed because: {ex} \n";
                    }


                    if (credential.CredentialType == CredentialType.SignedAssertion)
                    {
                        if (credential.SourceType == CredentialSource.SignedAssertionFromManagedIdentity)
                        {
                            if (credential.Skip)
                            {
                                LogMessages.NotUsingManagedIdentity(_logger, errorMessage);
                            }
                            else
                            {
                                LogMessages.UsingManagedIdentity(_logger);
                                return credential;
                            }
                        }
                        if (credential.SourceType == CredentialSource.SignedAssertionFilePath)
                        {
                            if (!credential.Skip)
                            {
                                LogMessages.UsingPodIdentityFile(_logger, credential.SignedAssertionFileDiskPath ?? "not found");
                                return credential;
                            }
                        }
                        if (credential.SourceType == CredentialSource.SignedAssertionFromVault)
                        {
                            if (!credential.Skip)
                            {
                                LogMessages.UsingSignedAssertionFromVault(_logger, credential.KeyVaultUrl ?? "undefined");
                                return credential;
                            }
                        }
                        if (credential.SourceType == CredentialSource.CustomSignedAssertion)
                        {
                            if (!credential.Skip)
                            {
                                LogMessages.UsingSignedAssertionFromCustomProvider(_logger, credential.CustomSignedAssertionProviderName ?? "undefined");
                                return credential;
                            }
                        }
                    }

                    if (credential.CredentialType == CredentialType.Certificate)
                    {
                        var certificate = credential.Certificate;
                        if (certificate != null)
                        {
                            LogMessages.UsingCertThumbprint(_logger, certificate.Thumbprint);
                            NotifyCertificateAction(
                                credentialSourceLoaderParameters,
                                credential,
                                certificate,
                                CerticateObserverAction.Selected,
                                null);
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

            _logger.LogInformation($"No client credential could be used. Secret may have been defined elsewhere. " +
                $"Count {(clientCredentials != null ? clientCredentials.Count() : 0)} ");

            return null;
        }

        public void NotifyCertificateUsed(
            CredentialSourceLoaderParameters? credentialSourceLoaderParameters,
            CredentialDescription certificateDescription,
            X509Certificate2 certificate,
            bool successful,
            Exception? exception)
        {
            CerticateObserverAction action = successful ? CerticateObserverAction.SuccessfullyUsed : CerticateObserverAction.Deselected;
            NotifyCertificateAction(credentialSourceLoaderParameters, certificateDescription, certificate, action, exception);
        }

        private void NotifyCertificateAction(
            CredentialSourceLoaderParameters? sourceLoaderParameters,
            CredentialDescription certificateDescription,
            X509Certificate2 certificate,
            CerticateObserverAction action,
            Exception? exception)
        {
            for (int i = 0; i < _certificatesObservers.Count; i++)
            {
                _certificatesObservers[i].OnClientCertificateChanged(
                    new CertificateChangeEventArg()
                    {
                        Action = action,
                        Certificate = certificate,
                        CredentialDescription = certificateDescription,
                        CredentialSourceLoaderParameters = sourceLoaderParameters,
                        ThrownException = exception,
                    });
            }

            // If deselected, clear the values so they can be reloaded.
            if (action == CerticateObserverAction.Deselected)
            {
                certificateDescription.Certificate = null;
                certificateDescription.CachedValue = null;
            }
        }
    }
}

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
    internal class CredentialsProvider : ICredentialsProvider
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
                            NotifyCertificateAction(string.Empty, credential, certificate, CerticateObserverAction.Selected, null);
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
            string authenticationScheme,
            CredentialDescription certificateDescription,
            X509Certificate2 certificate,
            bool successful,
            Exception? exception)
        {
            CerticateObserverAction action = successful ? CerticateObserverAction.SuccessfullyUsed : CerticateObserverAction.Deselected;
            NotifyCertificateAction(authenticationScheme, certificateDescription, certificate, action, exception);
        }

        private void NotifyCertificateAction(
            string authenticationScheme,
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

        private class LogMessages
        {
            private static readonly Action<ILogger, string, Exception?> s_notManagedIdentity =
                LoggerMessage.Define<string>(
                    LogLevel.Information,
                    LoggingEventId.NotUsingManagedIdentity,
                    "[MsIdWeb] Not using Managed identity for client credentials: {ErrorMessage}. ");

            private static readonly Action<ILogger, Exception?> s_usingManagedIdentity =
                LoggerMessage.Define(
                    LogLevel.Information,
                    LoggingEventId.UsingManagedIdentity,
                    "[MsIdWeb] Using Managed identity for client credentials. ");

            private static readonly Action<ILogger, string, Exception?> s_usingPodIdentityFile =
                LoggerMessage.Define<string>(
                    LogLevel.Information,
                    LoggingEventId.UsingPodIdentityFile,
                    "[MsIdWeb] Using Pod identity file {signedAssertionFileDiskPath} as client credentials. ");

            private static readonly Action<ILogger, string, Exception?> s_usingSignedAssertionFromVault =
                LoggerMessage.Define<string>(
                    LogLevel.Information,
                    LoggingEventId.UsingSignedAssertionFromVault,
                    "[MsIdWeb] Using signed assertion from {signedAssertionUri} as client credentials. ");

            private static readonly Action<ILogger, string, Exception?> s_usingSignedAssertionFromCustomProvider =
                LoggerMessage.Define<string>(
                    LogLevel.Information,
                    LoggingEventId.UsingSignedAssertionFromCustomProvider,
                    "[MsIdWeb] Using signed assertion from {signedAssertionUri} as client credentials. ");

            private static readonly Action<ILogger, string, Exception?> s_usingCertThumbprint =
                LoggerMessage.Define<string>(
                    LogLevel.Information,
                    LoggingEventId.UsingCertThumbprint,
                    "[MsIdWeb] Using certificate Thumbprint={certThumbprint} as client credentials. ");

            private static readonly Action<ILogger, string, string, Exception?> s_credentialAttempt =
                LoggerMessage.Define<string, string>(
                    LogLevel.Information,
                    LoggingEventId.CredentialLoadAttempt,
                    "[MsIdWeb] Attempting to load the credential from the CredentialDescription with Id={Id} and Skip={Skip} . ");

            private static readonly Action<ILogger, string, string, Exception?> s_credentialAttemptFailed =
                LoggerMessage.Define<string, string>(
                    LogLevel.Information,
                    LoggingEventId.CredentialLoadAttemptFailed,
                    "[MsIdWeb] Loading the credential from CredentialDescription Id={Id} failed. Will the credential be re-attempted? - {Skip}.");

            /// <summary>
            /// Logger for attempting to use a CredentialDescription with MSAL
            /// </summary>
            /// <param name="logger"></param>
            /// <param name="certificateDescription"></param>
            /// <param name="ex"></param>
            public static void AttemptToLoadCredentialsFailed(
                ILogger logger,
                CredentialDescription certificateDescription,
                Exception ex)
            {
                s_credentialAttemptFailed(
                        logger,
                        certificateDescription.Id,
                        certificateDescription.Skip.ToString(),
                        ex);
            }

            /// <summary>
            /// Logger for attempting to use a CredentialDescription with MSAL
            /// </summary>
            /// <param name="logger"></param>
            /// <param name="certificateDescription"></param>
            public static void AttemptToLoadCredentials(
                ILogger logger,
                CredentialDescription certificateDescription)
            {
                s_credentialAttempt(
                        logger,
                        certificateDescription.Id,
                        certificateDescription.Skip.ToString(),
                        default!);
            }

            /// <summary>
            /// Logger for attempting to use a CredentialDescription with MSAL
            /// </summary>
            /// <param name="logger"></param>
            /// <param name="certificateDescription"></param>
            public static void FailedToLoadCredentials(
                ILogger logger,
                CredentialDescription certificateDescription)
            {
                s_credentialAttemptFailed(
                        logger,
                        certificateDescription.Id,
                        certificateDescription.Skip.ToString(),
                        default!);
            }

            /// <summary>
            /// Logger for handling information specific to ConfidentialClientApplicationBuilderExtension.
            /// </summary>
            /// <param name="logger">ILogger.</param>
            /// <param name="message">Exception message.</param>
            public static void NotUsingManagedIdentity(
                ILogger logger,
                string message)
            {
                s_notManagedIdentity(logger, message, default!);
            }

            /// <summary>
            /// Logger for handling information specific to ConfidentialClientApplicationBuilderExtension.
            /// </summary>
            /// <param name="logger">ILogger.</param>
            public static void UsingManagedIdentity(
                ILogger logger)
            {
                s_usingManagedIdentity(logger, default!);
            }

            /// <summary>
            /// Logger for handling information specific to ConfidentialClientApplicationBuilderExtension.
            /// </summary>
            /// <param name="logger">ILogger.</param>
            /// <param name="signedAssertionFileDiskPath"></param>
            public static void UsingPodIdentityFile(
                ILogger logger,
                string signedAssertionFileDiskPath)
            {
                s_usingPodIdentityFile(logger, signedAssertionFileDiskPath, default!);
            }

            /// <summary>
            /// Logger for handling information specific to ConfidentialClientApplicationBuilderExtension.
            /// </summary>
            /// <param name="logger">ILogger.</param>
            /// <param name="signedAssertionUri"></param>
            public static void UsingSignedAssertionFromVault(
                ILogger logger,
                string signedAssertionUri)
            {
                s_usingSignedAssertionFromVault(logger, signedAssertionUri, default!);
            }

            /// <summary>
            /// Logger for handling information specific to ConfidentialClientApplicationBuilderExtension.
            /// </summary>
            /// <param name="logger">ILogger.</param>
            /// <param name="signedAssertionUri"></param>
            public static void UsingSignedAssertionFromCustomProvider(
                ILogger logger,
                string signedAssertionUri)
            {
                s_usingSignedAssertionFromCustomProvider(logger, signedAssertionUri, default!);
            }

            /// <summary>
            /// Logger for handling information specific to ConfidentialClientApplicationBuilderExtension.
            /// </summary>
            /// <param name="logger">ILogger.</param>
            /// <param name="certThumbprint"></param>
            public static void UsingCertThumbprint(
                ILogger logger,
                string? certThumbprint)
            {
                s_usingCertThumbprint(logger, certThumbprint ?? "null", default!);
            }
        }
    }
}

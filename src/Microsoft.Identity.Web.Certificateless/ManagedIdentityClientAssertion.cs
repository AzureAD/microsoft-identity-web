// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.AppConfig;
using Microsoft.Identity.Client.Extensibility;
using Microsoft.Identity.Web.Certificateless;

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// See https://aka.ms/ms-id-web/certificateless.
    /// </summary>
    public partial class ManagedIdentityClientAssertion : ClientAssertionProviderBase
    {
        IManagedIdentityApplication _managedIdentityApplication;
        private readonly string _tokenExchangeUrl;
        private readonly ILogger? _logger;

        /// <summary>
        /// See https://aka.ms/ms-id-web/certificateless.
        /// </summary>
        /// <param name="managedIdentityClientId">Optional ClientId of the Managed Identity</param>
        public ManagedIdentityClientAssertion(string? managedIdentityClientId) :
            this(managedIdentityClientId, tokenExchangeUrl: null, logger: null)
        {

        }

        /// <summary>
        /// See https://aka.ms/ms-id-web/certificateless.
        /// </summary>
        /// <param name="managedIdentityClientId">Optional ClientId of the Managed Identity</param>
        /// <param name="tokenExchangeUrl">Optional audience of the token to be requested from Managed Identity. Default value is "api://AzureADTokenExchange". 
        /// This value is different on clouds other than Azure Public</param>
        public ManagedIdentityClientAssertion(string? managedIdentityClientId, string? tokenExchangeUrl) :
            this(managedIdentityClientId, tokenExchangeUrl, null)
        {
        }

        /// <summary>
        /// See https://aka.ms/ms-id-web/certificateless.
        /// </summary>
        /// <param name="managedIdentityClientId">Optional ClientId of the Managed Identity</param>
        /// <param name="tokenExchangeUrl">Optional audience of the token to be requested from Managed Identity. Default value is "api://AzureADTokenExchange". 
        /// This value is different on clouds other than Azure Public</param>
        /// <param name="logger">A logger</param>
        public ManagedIdentityClientAssertion(string? managedIdentityClientId, string? tokenExchangeUrl, ILogger? logger)
        {
            _tokenExchangeUrl = tokenExchangeUrl ?? CertificatelessConstants.DefaultTokenExchangeUrl;
            _logger = logger;

            var id = ManagedIdentityId.SystemAssigned;
            if (!string.IsNullOrEmpty(managedIdentityClientId))
            {
                id = ManagedIdentityId.WithUserAssignedClientId(managedIdentityClientId);
            }

            var builder = ManagedIdentityApplicationBuilder.Create(id);
            if (_logger != null)
            {
                builder = builder.WithLogging(Log, ConvertMicrosoftExtensionsLogLevelToMsal(_logger), enablePiiLogging: false);
                Logger.ManagedIdentityClientAssertionInitialized(_logger, _tokenExchangeUrl);
            }

            _managedIdentityApplication = builder
                .Build();
        }

        /// <summary>
        /// Prototype of certificate-less authentication using a signed assertion
        /// acquired with managed identity (certificateless).
        /// </summary>
        /// <returns>The signed assertion.</returns>
        protected override async Task<ClientAssertion> GetClientAssertionAsync(AssertionRequestOptions? assertionRequestOptions)
        {
            var result = await _managedIdentityApplication
                .AcquireTokenForManagedIdentity(_tokenExchangeUrl)
                .ExecuteAsync(assertionRequestOptions?.CancellationToken ?? CancellationToken.None)
                .ConfigureAwait(false);

            return new ClientAssertion(result.AccessToken, result.ExpiresOn);
        }

        private void Log(
          Client.LogLevel level,
          string message,
          bool containsPii)
        {
            switch (level)
            {
                case Client.LogLevel.Always:
                    _logger.LogInformation(message);
                    break;
                case Client.LogLevel.Error:
                    _logger.LogError(message);
                    break;
                case Client.LogLevel.Warning:
                    _logger.LogWarning(message);
                    break;
                case Client.LogLevel.Info:
                    _logger.LogInformation(message);
                    break;
                case Client.LogLevel.Verbose:
                    _logger.LogDebug(message);
                    break;
            }
        }

        private Client.LogLevel? ConvertMicrosoftExtensionsLogLevelToMsal(ILogger logger)
        {
            if (logger.IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug)
                || logger.IsEnabled(Microsoft.Extensions.Logging.LogLevel.Trace))
            {
                return Client.LogLevel.Verbose;
            }
            else if (logger.IsEnabled(Microsoft.Extensions.Logging.LogLevel.Information))
            {
                return Client.LogLevel.Info;
            }
            else if (logger.IsEnabled(Microsoft.Extensions.Logging.LogLevel.Warning))
            {
                return Client.LogLevel.Warning;
            }
            else if (logger.IsEnabled(Microsoft.Extensions.Logging.LogLevel.Error)
                || logger.IsEnabled(Microsoft.Extensions.Logging.LogLevel.Critical))
            {
                return Client.LogLevel.Error;
            }
            else
            {
                return null;
            }
        }

    }
}

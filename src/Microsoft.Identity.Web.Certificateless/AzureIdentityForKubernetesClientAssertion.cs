// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Client;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// Gets a signed assertion from Azure workload identity for kubernetes when an app is running in a container
    /// in Azure Kubernetes Services. See https://aka.ms/ms-id-web/certificateless and
    /// https://learn.microsoft.com/azure/aks/workload-identity-overview
    /// </summary>
    public partial class AzureIdentityForKubernetesClientAssertion : ClientAssertionProviderBase
    {
        const string azureAccessTokenFileEnvironmentVariable = "AZURE_ACCESS_TOKEN_FILE";
        const string azureFederatedTokenFileEnvironmentVariable =   "AZURE_FEDERATED_TOKEN_FILE";
        private readonly string? _filePath;
        private readonly ILogger? _logger;

        /// <summary>
        /// Gets a signed assertion from Azure workload identity for kubernetes. The file path is provided
        /// by an environment variable ("AZURE_FEDERATED_TOKEN_FILE")
        /// See https://aka.ms/ms-id-web/certificateless.
        /// </summary>
        public AzureIdentityForKubernetesClientAssertion(ILogger? logger = null) : this(null, logger)
        {
        }

        /// <summary>
        /// Gets a signed assertion from a file.
        /// See https://aka.ms/ms-id-web/certificateless.
        /// </summary>
        /// <param name="filePath">Path to a file containing the signed assertion.</param>
        /// <param name="logger">Logger.</param>
        public AzureIdentityForKubernetesClientAssertion(string? filePath, ILogger? logger = null)
        {
            _logger = logger;

            if (filePath == null)
            {
                Logger.SignedAssertionFileDiskPathNotProvided(_logger);
            }

            _filePath = _filePath ?? Environment.GetEnvironmentVariable(azureAccessTokenFileEnvironmentVariable);
            if (filePath == null)
            {
                Logger.SignedAssertionEnvironmentVariableNotProvided(_logger, azureAccessTokenFileEnvironmentVariable);
            }

            // See https://blog.identitydigest.com/azuread-federate-k8s/
            _filePath = filePath ?? Environment.GetEnvironmentVariable(azureFederatedTokenFileEnvironmentVariable);
            if (_filePath == null)
            {
                Logger.SignedAssertionEnvironmentVariableNotProvided(_logger, azureFederatedTokenFileEnvironmentVariable);
                Logger.NoSignedAssertionParameterProvided(_logger);
            }
        }

        /// <summary>
        /// Get the signed assertion from a file.
        /// </summary>
        /// <returns>The signed assertion.</returns>
        protected override Task<ClientAssertion> GetClientAssertionAsync(AssertionRequestOptions? assertionRequestOptions)
        {
            if (_filePath != null && !File.Exists(_filePath))
            {
                Logger.FileAssertionPathNotFound(_logger, _filePath);
                throw new FileNotFoundException($"The file '{_filePath}' containing the signed assertion was not found.");

            }
            string signedAssertion = File.ReadAllText(_filePath);

            // Verify that the assertion is a JWS, JWE, and computes the expiry
            try
            {
                JsonWebToken jwt = new JsonWebToken(signedAssertion);

                Logger.SuccessFullyReadSignedAssertion(_logger, _filePath!, jwt.ValidTo);

                return Task.FromResult(new ClientAssertion(signedAssertion, jwt.ValidTo));
            }
            catch (ArgumentException ex)
            {
                Logger.FileDoesNotContainValidAssertion(_logger, _filePath!, ex.Message);
                throw;
            }
        }
    }
}

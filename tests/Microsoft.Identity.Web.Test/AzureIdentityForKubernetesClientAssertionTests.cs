// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Web.Test.Common;
using Microsoft.IdentityModel.JsonWebTokens;
using Xunit;

namespace Microsoft.Identity.Web.Tests.Certificateless
{
    public class AzureIdentityForKubernetesClientAssertionTests
    {
        string token;
        private const string FilePath = "signedAssertion.txt";
        private const string FilePath2 = "signedAssertion2.txt";

        public AzureIdentityForKubernetesClientAssertionTests()
        {
            JsonWebTokenHandler handler = new JsonWebTokenHandler();
            token = handler.CreateToken("{}");
        }

        [Fact]
        public async Task GetAksClientAssertion_WhenSpecifiedSignedAssertionFileExists_ReturnsClientAssertion()
        {
            // Arrange
            File.WriteAllText(FilePath, token.ToString());
            AzureIdentityForKubernetesClientAssertion azureIdentityForKubernetesClientAssertion = new AzureIdentityForKubernetesClientAssertion(FilePath);

            // Act
            string signedAssertion = await azureIdentityForKubernetesClientAssertion.GetSignedAssertion(CancellationToken.None);

            // Assert
            Assert.NotNull(signedAssertion);
        }

        [Fact]
        public async Task GetAksClientAssertion_WhenEnvironmentVariablePointsToSignedAssertionFileExists_ReturnsClientAssertion()
        {
            // Arrange
            File.WriteAllText(FilePath2, token.ToString());
            Environment.SetEnvironmentVariable("AZURE_FEDERATED_TOKEN_FILE", FilePath2);
            AzureIdentityForKubernetesClientAssertion azureIdentityForKubernetesClientAssertion = new AzureIdentityForKubernetesClientAssertion();

            // Act
            string signedAssertion = await azureIdentityForKubernetesClientAssertion.GetSignedAssertion(CancellationToken.None);

            // Assert
            Assert.NotNull(signedAssertion);

            // Delete the signed assertion file and remove the environment variable.
            Environment.SetEnvironmentVariable("AZURE_FEDERATED_TOKEN_FILE", null);
        }

        [Fact]
        public async Task GetAksClientAssertion_WhenSignedAssertionFileDoesNotExist_ThrowsFileNotFoundException()
        {
            // Arrange
            var filePath = "doesNotExist.txt";
            AzureIdentityForKubernetesClientAssertion azureIdentityForKubernetesClientAssertion = new AzureIdentityForKubernetesClientAssertion(filePath);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<FileNotFoundException>(() => azureIdentityForKubernetesClientAssertion.GetSignedAssertion(CancellationToken.None));
            Assert.Contains(filePath, ex.Message, System.StringComparison.OrdinalIgnoreCase);
        }
    }
}

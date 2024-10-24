// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.IdentityModel.JsonWebTokens;
using Xunit;

namespace Microsoft.Identity.Web.Tests.Certificateless
{
    public class AzureIdentityForKubernetesClientAssertionTests
    {
        readonly string _token;
        private readonly string _filePath = "signedAssertion" + Environment.CurrentManagedThreadId.ToString(CultureInfo.InvariantCulture) + ".txt";

        public AzureIdentityForKubernetesClientAssertionTests()
        {
            JsonWebTokenHandler handler = new JsonWebTokenHandler();
            _token = handler.CreateToken("{}");
        }

        [Fact]
        public async Task GetAksClientAssertion_WhenSpecifiedSignedAssertionFileExists_ReturnsClientAssertionAsync()
        {
            // Arrange
            File.WriteAllText(_filePath, _token.ToString());
            AzureIdentityForKubernetesClientAssertion azureIdentityForKubernetesClientAssertion = new AzureIdentityForKubernetesClientAssertion(_filePath);

            // Act
            string signedAssertion = await azureIdentityForKubernetesClientAssertion.GetSignedAssertionAsync(null);

            // Assert
            Assert.NotNull(signedAssertion);
        }

        [Fact]
        public async Task GetAksClientAssertion_WhenEnvironmentVariablePointsToSignedAssertionFileExists_ReturnsClientAssertionAsync()
        {
            // Arrange
            File.WriteAllText(_filePath, _token.ToString());
            Environment.SetEnvironmentVariable("AZURE_FEDERATED_TOKEN_FILE", _filePath);
            AzureIdentityForKubernetesClientAssertion azureIdentityForKubernetesClientAssertion = new AzureIdentityForKubernetesClientAssertion();

            // Act
            string signedAssertion = await azureIdentityForKubernetesClientAssertion.GetSignedAssertionAsync(null);

            // Assert
            Assert.NotNull(signedAssertion);

            // Delete the signed assertion file and remove the environment variable.
            Environment.SetEnvironmentVariable("AZURE_FEDERATED_TOKEN_FILE", null);
        }

        [Fact]
        public async Task GetAksClientAssertion_WhenSignedAssertionFileDoesNotExist_ThrowsFileNotFoundExceptionAsync()
        {
            // Arrange
            var filePath = "doesNotExist.txt";
            AzureIdentityForKubernetesClientAssertion azureIdentityForKubernetesClientAssertion = new AzureIdentityForKubernetesClientAssertion(filePath);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<FileNotFoundException>(() => azureIdentityForKubernetesClientAssertion.GetSignedAssertionAsync(null));
            Assert.Contains(filePath, ex.Message, System.StringComparison.OrdinalIgnoreCase);
        }
    }
}

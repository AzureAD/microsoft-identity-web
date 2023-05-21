using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.IdentityModel.JsonWebTokens;
using Xunit;

namespace Microsoft.Identity.Web.Tests.Certificateless
{
    public class AzureIdentityForKubernetesClientAssertionTests
    {
        string filePath = "signedAssertion.txt";
        string token;

        public AzureIdentityForKubernetesClientAssertionTests()
        {
            JsonWebTokenHandler handler = new JsonWebTokenHandler();
            token = handler.CreateToken("{}");
        }

        [Fact]
        public async Task GetAksClientAssertion_WhenSpecifiedSignedAssertionFileExists_ReturnsClientAssertion()
        {
            // Arrange
            File.WriteAllText(filePath, token.ToString());
            AzureIdentityForKubernetesClientAssertion azureIdentityForKubernetesClientAssertion = new AzureIdentityForKubernetesClientAssertion(filePath);

            // Act
            string signedAssertion = await azureIdentityForKubernetesClientAssertion.GetSignedAssertion(CancellationToken.None);

            // Assert
            Assert.NotNull(signedAssertion);

            // Delete the signed assertion file.
            File.Delete(filePath);
        }

        [Fact]
        public async Task GetAksClientAssertion_WhenEnvironmentVariablePointsToSignedAssertionFileExists_ReturnsClientAssertion()
        {
            // Arrange
            File.WriteAllText(filePath, token.ToString());
            Environment.SetEnvironmentVariable("AZURE_FEDERATED_TOKEN_FILE", filePath);
            AzureIdentityForKubernetesClientAssertion azureIdentityForKubernetesClientAssertion = new AzureIdentityForKubernetesClientAssertion();

            // Act
            string signedAssertion = await azureIdentityForKubernetesClientAssertion.GetSignedAssertion(CancellationToken.None);

            // Assert
            Assert.NotNull(signedAssertion);

            // Delete the signed assertion file and remove the environment variable.
            File.Delete(filePath);
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

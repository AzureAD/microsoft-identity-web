// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Identity.Abstractions;
using Microsoft.IdentityModel.JsonWebTokens;
using Xunit;

namespace Microsoft.Identity.Web.Tests.Certificateless
{
    public class SignedAssertionFilePathCredentialsLoaderTests
    {
        private const string FilePath = "signedAssertion.txt";
        private const string FilePath2 = "signedAssertion2.txt";
        private const string AksEnvironmentVariableName = "AZURE_FEDERATED_TOKEN_FILE";
        private readonly string _token;
        private readonly SignedAssertionFilePathCredentialsLoader _signedAssertionFilePathCredentialsLoader = new(null);

        public SignedAssertionFilePathCredentialsLoaderTests()
        {
            JsonWebTokenHandler handler = new();
            _token = handler.CreateToken("{}");
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task GetClientAssertion_WhenSpecifiedSignedAssertionFileExists_ReturnsClientAssertionAsync(bool withEnvVariable)
        {
            // Arrange
            var filePath = withEnvVariable ? FilePath : FilePath2;
            lock (_signedAssertionFilePathCredentialsLoader)
            {
                File.WriteAllText(filePath, _token.ToString());
            }
            CredentialDescription credentialDescription;
            if (withEnvVariable)
            {
                Environment.SetEnvironmentVariable(AksEnvironmentVariableName, filePath);
                credentialDescription = new()
                {
                    SourceType = CredentialSource.SignedAssertionFilePath,
                };
            }
            else
            {
                credentialDescription = new()
                {
                    SourceType = CredentialSource.SignedAssertionFilePath,
                    SignedAssertionFileDiskPath = filePath
                };
            }

            // Act
            await _signedAssertionFilePathCredentialsLoader.LoadIfNeededAsync(credentialDescription, null);

            // Assert
            Assert.NotNull(credentialDescription.CachedValue);

            // Delete the signed assertion file.
            if (withEnvVariable)
            {
                Environment.SetEnvironmentVariable(AksEnvironmentVariableName, null);
            }
        }

        [Fact]
        public async Task GetClientAssertion_WhenSignedAssertionFileDoesNotExist_ThrowsFileNotFoundExceptionAsync()
        {
            // Act
            CredentialDescription credentialDescription = new()
            {
                SourceType = CredentialSource.SignedAssertionFilePath,
            };

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => _signedAssertionFilePathCredentialsLoader.LoadIfNeededAsync(
                credentialDescription, null));
        }
        
    }
}

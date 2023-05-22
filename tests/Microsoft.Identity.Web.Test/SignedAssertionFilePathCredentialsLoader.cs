// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Web.Test.Common;
using Microsoft.IdentityModel.JsonWebTokens;
using Xunit;

namespace Microsoft.Identity.Web.Tests.Certificateless
{
    public class SignedAssertionFilePathCredentialsLoaderTests
    {
        const string filePath = "signedAssertion.txt";
        const string aksEnvironmentVariableName = "AZURE_FEDERATED_TOKEN_FILE";
        string token;
        SignedAssertionFilePathCredentialsLoader signedAssertionFilePathCredentialsLoader = new SignedAssertionFilePathCredentialsLoader(null);


        public SignedAssertionFilePathCredentialsLoaderTests()
        {
            JsonWebTokenHandler handler = new JsonWebTokenHandler();
            token = handler.CreateToken("{}");
        }

        [Fact]
        public async Task GetClientAssertion_WhenSpecifiedSignedAssertionFileExists_ReturnsClientAssertion()
        {
            // Arrange
            File.WriteAllText(filePath, token.ToString());
            CredentialDescription credentialDescription = new CredentialDescription
            {
                SourceType = CredentialSource.SignedAssertionFilePath,
                SignedAssertionFileDiskPath = filePath
            };

            // Act
            await signedAssertionFilePathCredentialsLoader.LoadIfNeededAsync(credentialDescription, null);

            // Assert
            Assert.NotNull(credentialDescription.CachedValue);

            // Delete the signed assertion file.
            File.Delete(filePath);
        }

        [Fact]
        public async Task GetClientAssertion_WhenEnvironmentVariablePointsToSignedAssertionFileExists_ReturnsClientAssertion()
        {
            // Arrange
            File.WriteAllText(filePath, token.ToString());
            Environment.SetEnvironmentVariable(aksEnvironmentVariableName, filePath);
            CredentialDescription credentialDescription = new CredentialDescription
            {
                SourceType = CredentialSource.SignedAssertionFilePath,
            };

            // Act
            await signedAssertionFilePathCredentialsLoader.LoadIfNeededAsync(credentialDescription, null);

            // Assert
            Assert.NotNull(credentialDescription.CachedValue);

            // Delete the signed assertion file and remove the environment variable.
            File.Delete(filePath);
            Environment.SetEnvironmentVariable(aksEnvironmentVariableName, null);
        }

        [Fact]
        public async Task GetClientAssertion_WhenSignedAssertionFileDoesNotExist_ThrowsFileNotFoundException()
        {
            // Act
            CredentialDescription credentialDescription = new CredentialDescription
            {
                SourceType = CredentialSource.SignedAssertionFilePath,
            };
            await signedAssertionFilePathCredentialsLoader.LoadIfNeededAsync(credentialDescription, null);

            // Act & Assert
            Assert.Null(credentialDescription.CachedValue);
        }
    }
}

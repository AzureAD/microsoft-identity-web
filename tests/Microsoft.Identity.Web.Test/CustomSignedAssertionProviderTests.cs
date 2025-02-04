// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Moq;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Abstractions;
using Xunit;

namespace Microsoft.Identity.Web.Test
{
    public class CustomSignedAssertionProviderTests
    {
        [Theory]
        [MemberData(nameof(CustomSignedAssertionTestData))]
        public async Task ProcessCustomSignedAssertionAsync_Tests(
            List<ICustomSignedAssertionProvider> providerList,
            CredentialDescription credentialDescription,
            string? expectedMessage = null)
        {
            // Arrange
            var loggedMessages = new List<string>();
            var loggerMock = new Mock<ILogger<DefaultCredentialsLoader>>();
            loggerMock.Setup(x => x.IsEnabled(It.IsAny<LogLevel>())).Returns(true);

            var loader = new DefaultCredentialsLoader(providerList, loggerMock.Object);

            // Act
            try
            {
                await loader.LoadCredentialsIfNeededAsync(credentialDescription, null);

            }
            catch (Exception ex)
            {
                Assert.Equal(expectedMessage, ex.Message);

                // Haven't figured out yet how to get the mock logger to see the log coming from DefaultCredentialsLoader.Logger where it is logged using LogMessage.Define()
                loggerMock.Verify(
                    x =>
                    x.Log(
                        It.IsAny<LogLevel>(),
                        It.IsAny<EventId>(),
                        It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(expectedMessage!)),
                        It.IsAny<Exception>(),
                        It.Is<Func<It.IsAnyType, Exception?, string>>((v,t) => true)));
                return;
            }

            // Assert
            if (expectedMessage != null)
            {
                loggerMock.Verify(
                    x => x.Log(
                        LogLevel.Error,
                        It.IsAny<EventId>(),
                        It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(expectedMessage)),
                        It.IsAny<Exception?>(),
                        It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                    Times.Once);
            }
            else
            {
                loggerMock.Verify(
                    x => x.Log(
                        It.IsAny<LogLevel>(),
                        It.IsAny<EventId>(),
                        It.IsAny<It.IsAnyType>(),
                        It.IsAny<Exception>(),
                        It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                    Times.Never);
            }
        }

        public static IEnumerable<object[]> CustomSignedAssertionTestData()
        {
            // No source loaders
            yield return new object[]
            {
                    new List<ICustomSignedAssertionProvider>(),
                    new CredentialDescription
                    {
                        CustomSignedAssertionProviderName = "Provider1",
                        SourceType = CredentialSource.CustomSignedAssertion,
                        Skip = false
                    },
                    CertificateErrorMessage.CustomProviderSourceLoaderNullOrEmpty
            };

            // No provider name given
            yield return new object[]
            {
                    new List<ICustomSignedAssertionProvider> { new SuccessfulCustomSignedAssertionProvider("Provider2") },
                    new CredentialDescription
                    {
                        CustomSignedAssertionProviderName = null,
                        SourceType = CredentialSource.CustomSignedAssertion
                    },
                    CertificateErrorMessage.CustomProviderNameNullOrEmpty
            };

            // Given provider name not found
            yield return new object[]
            {
                    new List<ICustomSignedAssertionProvider> { new SuccessfulCustomSignedAssertionProvider("NotProvider3") },
                    new CredentialDescription
                    {
                        CustomSignedAssertionProviderName = "Provider3",
                        SourceType = CredentialSource.CustomSignedAssertion
                    },
                    string.Format(CultureInfo.InvariantCulture, CertificateErrorMessage.CustomProviderNotFound, "Provider3")
            };

            // Happy path (no logging expected)
            yield return new object[]
            {
                    new List<ICustomSignedAssertionProvider> { new SuccessfulCustomSignedAssertionProvider("Provider4") },
                    new CredentialDescription
                    {
                        CustomSignedAssertionProviderName = "Provider4",
                        SourceType = CredentialSource.CustomSignedAssertion
                    }
            };

            // CustomSignedAssertionProvider (i.e. the user's extension) throws an exception
            yield return new object[]
            {
                new List<ICustomSignedAssertionProvider> { new FailingCustomSignedAssertionProvider("Provider5") },
                new CredentialDescription
                {
                    CustomSignedAssertionProviderName = "Provider5",
                    SourceType = CredentialSource.CustomSignedAssertion
                },
                FailingCustomSignedAssertionProvider.ExceptionMessage
            };
        }
    }

    // Helper class
    internal class SuccessfulCustomSignedAssertionProvider : ICustomSignedAssertionProvider
    {
        public string Name { get; }

        public CredentialSource CredentialSource => CredentialSource.CustomSignedAssertion;

        public SuccessfulCustomSignedAssertionProvider(string name)
        {
            Name = name;
        }

        public Task LoadIfNeededAsync(CredentialDescription credentialDescription, CredentialSourceLoaderParameters? parameters)
        {
            return Task.CompletedTask;
        }
    }

    internal class FailingCustomSignedAssertionProvider : ICustomSignedAssertionProvider
    {
        public string Name { get; }
        public const string ExceptionMessage = "This extension is broken :(";

        public CredentialSource CredentialSource => CredentialSource.CustomSignedAssertion;

        public FailingCustomSignedAssertionProvider(string name)
        {
            Name = name;
        }

        public Task LoadIfNeededAsync(CredentialDescription credentialDescription, CredentialSourceLoaderParameters? parameters)
        {
            throw new Exception("This extension is broken :(");
        }
    }
}

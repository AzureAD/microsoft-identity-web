// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Abstractions;
using Moq;
using Xunit;

namespace Microsoft.Identity.Web.Test
{
    public class CustomSignedAssertionProviderTests
    {
        [Theory]
        [MemberData(nameof(CustomSignedAssertionLoggingTestData))]
        public async Task ProcessCustomSignedAssertionAsync_Tests(
            List<ICustomSignedAssertionProvider> providerList,
            CredentialDescription credentialDescription,
            LogLevel expectedLogLevel = LogLevel.None,
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

                // This is validating the logging behavior defined by DefaultCredentialsLoader.Logger.CustomSignedAssertionProviderLoadingFailure
                loggerMock.Verify(
                    x => x.Log(
                        expectedLogLevel,
                        It.IsAny<EventId>(),
                        It.Is<It.IsAnyType>((v, t) => true), // In Microsoft.Logging.Abstractions this is a private struct which is why it is defined so loosely.
                        It.IsAny<Exception>(),
                        It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                    Times.Once);
                return;
            }

            // Assert
            if (expectedMessage != null)
            {
                loggerMock.Verify(
                x => x.Log(
                    expectedLogLevel,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(expectedMessage)),
                    It.IsAny<Exception?>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                    Times.Once
                );
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
                    Times.Never
                );
            }
        }

        public static IEnumerable<object[]> CustomSignedAssertionLoggingTestData()
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
                    LogLevel.Error,
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
                    LogLevel.Error,
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
                    LogLevel.Error,
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
                LogLevel.Information,
                FailingCustomSignedAssertionProvider.ExceptionMessage
            };

            // Multiple providers with the same name
            yield return new object[]
            {
                    new List<ICustomSignedAssertionProvider> { new SuccessfulCustomSignedAssertionProvider("Provider6"), new SuccessfulCustomSignedAssertionProvider("Provider6") },
                    new CredentialDescription
                    {
                        CustomSignedAssertionProviderName = "Provider6",
                        SourceType = CredentialSource.CustomSignedAssertion
                    },
                    LogLevel.Warning,
                    string.Format(CultureInfo.InvariantCulture, CertificateErrorMessage.CustomProviderNameAlreadyExists, "Provider6")
            };
        }
    }

    // Helper class mocking an implementation of ICustomSignedAssertionProvider normally provided by a user where the LoadIfNeededAsync method completes without error.
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

    // Helper class mocking an implementation of ICustomSignedAssertionProvider normally provided by a user where the LoadIfNeededAsync method throws error.
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

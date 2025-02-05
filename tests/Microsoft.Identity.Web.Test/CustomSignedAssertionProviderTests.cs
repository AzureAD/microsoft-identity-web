// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Abstractions;
using Xunit;

namespace Microsoft.Identity.Web.Test
{
    public class CustomSignedAssertionProviderTests
    {
        [Fact]
        public void Constructor_NullProviders_ThrowsArgumentNullException()
        {
            // Arrange
            var loggerMock = new CustomMockLogger<DefaultCredentialsLoader>();
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new DefaultCredentialsLoader(null!, loggerMock));
        }

        [Theory]
        [MemberData(nameof(CustomSignedAssertionLoggingTestData))]
        public async Task ProcessCustomSignedAssertionAsync_Tests(
            List<ICustomSignedAssertionProvider> providerList,
            CredentialDescription credentialDescription,
            LogLevel expectedLogLevel = LogLevel.None,
            string? expectedLogMessage = null,
            string? expectedExceptionMessage = null)
        {
            // Arrange
            var loggerMock = new CustomMockLogger<DefaultCredentialsLoader>();

            var loader = new DefaultCredentialsLoader(providerList, loggerMock);

            // Act
            try
            {
                await loader.LoadCredentialsIfNeededAsync(credentialDescription, null);
            }
            catch (Exception ex)
            {
                Assert.Equal(expectedExceptionMessage, ex.Message);

                // This is validating the logging behavior defined by DefaultCredentialsLoader.Logger.CustomSignedAssertionProviderLoadingFailure
                if (expectedLogMessage is not null)
                {
                    Assert.Contains(loggerMock.LoggedMessages, log => log.LogLevel == expectedLogLevel && log.Message.Contains(expectedLogMessage, StringComparison.InvariantCulture));
                }
                return;
            }

            // Assert
            if (expectedLogMessage != null)
            {
                Assert.Contains(loggerMock.LoggedMessages, log => log.LogLevel == expectedLogLevel && log.Message.Contains(expectedLogMessage, StringComparison.InvariantCulture));
            }
            else
            {
                Assert.DoesNotContain(loggerMock.LoggedMessages, log => log.LogLevel == expectedLogLevel);
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
            CredentialDescription providerFiveCredDesc = new()
                {
                    CustomSignedAssertionProviderName = "Provider5",
                    SourceType = CredentialSource.CustomSignedAssertion
                };

            yield return new object[]
            {
                new List<ICustomSignedAssertionProvider> { new FailingCustomSignedAssertionProvider("Provider5") },
                providerFiveCredDesc,
                LogLevel.Information,
                string.Format
                (
                    CultureInfo.InvariantCulture,
                    DefaultCredentialsLoader.CustomSignedAssertionProviderLoadingFailureMessage
                    (
                        providerFiveCredDesc.CustomSignedAssertionProviderName ?? DefaultCredentialsLoader.nameMissing,
                        providerFiveCredDesc.SourceType.ToString(),
                        providerFiveCredDesc.Skip.ToString()
                    )
                ),
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

    // Custom logger implementation
    sealed class CustomMockLogger<T> : ILogger<T>
    {
        public List<LogEntry> LoggedMessages { get; } = new List<LogEntry>();

        IDisposable ILogger.BeginScope<TState>(TState state) => null!;

        bool ILogger.IsEnabled(LogLevel logLevel) => true;

        void ILogger.Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            LoggedMessages.Add(new LogEntry
            {
                LogLevel = logLevel,
                Message = formatter(state, exception),
                Exception = exception
            });
        }
    }

    public class LogEntry
    {
        public LogLevel LogLevel { get; set; }
        public string Message { get; set; } = string.Empty;
        public Exception? Exception { get; set; }
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

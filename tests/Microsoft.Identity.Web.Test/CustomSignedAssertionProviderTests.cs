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
        [MemberData(nameof(CustomSignedAssertionProviderLoggingTestData), DisableDiscoveryEnumeration = true)]
        public async Task ProcessCustomSignedAssertionAsync_Tests(CustomSignedAssertionProviderTheoryData data)
        {
            // Arrange
            var loggerMock = new CustomMockLogger<DefaultCredentialsLoader>();

            var loader = new DefaultCredentialsLoader(data.AssertionProviderList, loggerMock);

            // Act
            try
            {
                await loader.LoadCredentialsIfNeededAsync(data.CredentialDescription, null);
            }
            catch (Exception ex)
            {
                Assert.Equal(data.ExpectedExceptionMessage, ex.Message);

                // This is validating the logging behavior defined by DefaultCredentialsLoader.Logger.CustomSignedAssertionProviderLoadingFailure
                if (data.ExpectedLogMessage is not null)
                {
                    Assert.Contains(loggerMock.LoggedMessages, log => log.LogLevel == data.ExpectedLogLevel && log.Message.Contains(data.ExpectedLogMessage, StringComparison.InvariantCulture));
                }
                return;
            }

            // Assert
            if (data.ExpectedLogMessage is not null)
            {
                Assert.Contains(loggerMock.LoggedMessages, log => log.LogLevel == data.ExpectedLogLevel && log.Message.Contains(data.ExpectedLogMessage, StringComparison.InvariantCulture));
            }
            else
            {
                Assert.DoesNotContain(loggerMock.LoggedMessages, log => log.LogLevel == data.ExpectedLogLevel);
            }
        }

        public static TheoryData<CustomSignedAssertionProviderTheoryData> CustomSignedAssertionProviderLoggingTestData()
        {
            return
            [
                // No source loaders
                new CustomSignedAssertionProviderTheoryData
                {
                    AssertionProviderList = [],
                    CredentialDescription = new CredentialDescription
                    {
                        CustomSignedAssertionProviderName = "Provider1",
                        SourceType = CredentialSource.CustomSignedAssertion,
                        Skip = false
                    },
                    ExpectedLogLevel = LogLevel.Error,
                    ExpectedLogMessage = CertificateErrorMessage.CustomProviderSourceLoaderNullOrEmpty
                },

                // No provider name given
                new CustomSignedAssertionProviderTheoryData
                {
                    AssertionProviderList = [new SuccessfulCustomSignedAssertionProvider("Provider2")],
                    CredentialDescription = new CredentialDescription
                    {
                        CustomSignedAssertionProviderName = null,
                        SourceType = CredentialSource.CustomSignedAssertion
                    },
                    ExpectedLogLevel = LogLevel.Error,
                    ExpectedLogMessage = CertificateErrorMessage.CustomProviderNameNullOrEmpty
                },

                // Given provider name not found
                new CustomSignedAssertionProviderTheoryData
                {
                    AssertionProviderList = [new SuccessfulCustomSignedAssertionProvider("NotProvider3")],
                    CredentialDescription = new CredentialDescription
                    {
                        CustomSignedAssertionProviderName = "Provider3",
                        SourceType = CredentialSource.CustomSignedAssertion
                    },
                    ExpectedLogLevel = LogLevel.Error,
                    ExpectedLogMessage = string.Format(CultureInfo.InvariantCulture, CertificateErrorMessage.CustomProviderNotFound, "Provider3")
                },

                // Happy path (no logging expected)
                new CustomSignedAssertionProviderTheoryData
                {
                    AssertionProviderList = [new SuccessfulCustomSignedAssertionProvider("Provider4")],
                    CredentialDescription = new CredentialDescription
                    {
                        CustomSignedAssertionProviderName = "Provider4",
                        SourceType = CredentialSource.CustomSignedAssertion
                    }
                },

                // CustomSignedAssertionProvider (i.e. the user's extension) throws an exception
                new CustomSignedAssertionProviderTheoryData
                {
                    AssertionProviderList = [new FailingCustomSignedAssertionProvider("Provider5")],
                    CredentialDescription = new CredentialDescription
                    {
                        CustomSignedAssertionProviderName = "Provider5",
                        SourceType = CredentialSource.CustomSignedAssertion
                    },
                    ExpectedLogLevel = LogLevel.Information,
                    ExpectedLogMessage = string.Format
                    (
                        CultureInfo.InvariantCulture,
                        LoggerExtensions.CustomSignedAssertionProviderLoadingFailureMessage
                        (
                            "Provider5",
                            CredentialSource.CustomSignedAssertion.ToString(),
                            false.ToString()
                        )
                    ),
                    ExpectedExceptionMessage = FailingCustomSignedAssertionProvider.ExceptionMessage
                },

                // Multiple providers with the same name
                new CustomSignedAssertionProviderTheoryData
                {
                    AssertionProviderList = [new SuccessfulCustomSignedAssertionProvider("Provider6"), new SuccessfulCustomSignedAssertionProvider("Provider6")],
                    CredentialDescription = new CredentialDescription
                    {
                        CustomSignedAssertionProviderName = "Provider6",
                        SourceType = CredentialSource.CustomSignedAssertion
                    },
                    ExpectedLogLevel = LogLevel.Warning,
                    ExpectedLogMessage = string.Format(CultureInfo.InvariantCulture, CertificateErrorMessage.CustomProviderNameAlreadyExists, "Provider6")
                }
            ];
        }

    }

    public class CustomSignedAssertionProviderTheoryData 
    {
        public List<ICustomSignedAssertionProvider> AssertionProviderList { get; set; } = [];
        public CredentialDescription CredentialDescription { get; set; } = new CredentialDescription();
        public LogLevel ExpectedLogLevel { get; set; }
        public string? ExpectedLogMessage { get; set; }
        public string? ExpectedExceptionMessage { get; set; }
    }

    // Custom logger implementation
    sealed class CustomMockLogger<T> : ILogger<T>
    {
        public List<LogEntry> LoggedMessages { get; } = [];

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

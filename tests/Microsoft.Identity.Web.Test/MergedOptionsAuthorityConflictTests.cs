// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Microsoft.Identity.Web.Test
{
    public class MergedOptionsAuthorityConflictTests
    {
        private readonly TestLogger _testLogger;

        public MergedOptionsAuthorityConflictTests()
        {
            _testLogger = new TestLogger();
        }

        [Fact]
        public void ParseAuthorityIfNecessary_AuthorityAndInstance_LogsWarning()
        {
            // Arrange
            var mergedOptions = new MergedOptions
            {
                Authority = "https://login.microsoftonline.com/common",
                Instance = "https://login.microsoftonline.com/",
                Logger = _testLogger
            };

            // Act
            MergedOptions.ParseAuthorityIfNecessary(mergedOptions, _testLogger);

            // Assert
            Assert.Single(_testLogger.LogMessages);
            Assert.Contains("Authority", _testLogger.LogMessages[0], StringComparison.OrdinalIgnoreCase);
            Assert.Contains("ignored", _testLogger.LogMessages[0], StringComparison.OrdinalIgnoreCase);
            Assert.Equal(LogLevel.Warning, _testLogger.LogLevel);
        }

        [Fact]
        public void ParseAuthorityIfNecessary_AuthorityAndTenantId_LogsWarning()
        {
            // Arrange
            var mergedOptions = new MergedOptions
            {
                Authority = "https://login.microsoftonline.com/common",
                TenantId = "organizations",
                Logger = _testLogger
            };

            // Act
            MergedOptions.ParseAuthorityIfNecessary(mergedOptions, _testLogger);

            // Assert
            Assert.Single(_testLogger.LogMessages);
            Assert.Contains("Authority", _testLogger.LogMessages[0], StringComparison.OrdinalIgnoreCase);
            Assert.Contains("ignored", _testLogger.LogMessages[0], StringComparison.OrdinalIgnoreCase);
            Assert.Equal(LogLevel.Warning, _testLogger.LogLevel);
        }

        [Fact]
        public void ParseAuthorityIfNecessary_AuthorityAndInstanceAndTenantId_LogsWarning()
        {
            // Arrange
            var mergedOptions = new MergedOptions
            {
                Authority = "https://login.microsoftonline.com/common",
                Instance = "https://login.microsoftonline.com/",
                TenantId = "organizations",
                Logger = _testLogger
            };

            // Act
            MergedOptions.ParseAuthorityIfNecessary(mergedOptions, _testLogger);

            // Assert
            Assert.Single(_testLogger.LogMessages);
            Assert.Contains("Authority", _testLogger.LogMessages[0], StringComparison.OrdinalIgnoreCase);
            Assert.Contains("ignored", _testLogger.LogMessages[0], StringComparison.OrdinalIgnoreCase);
            Assert.Equal(LogLevel.Warning, _testLogger.LogLevel);
        }

        [Fact]
        public void ParseAuthorityIfNecessary_AuthorityOnly_NoWarning()
        {
            // Arrange
            var mergedOptions = new MergedOptions
            {
                Authority = "https://login.microsoftonline.com/common",
                Logger = _testLogger
            };

            // Act
            MergedOptions.ParseAuthorityIfNecessary(mergedOptions, _testLogger);

            // Assert - No warning should be logged, authority should be parsed
            Assert.Empty(_testLogger.LogMessages);
            Assert.Equal("https://login.microsoftonline.com", mergedOptions.Instance);
            Assert.Equal("common", mergedOptions.TenantId);
        }

        [Fact]
        public void ParseAuthorityIfNecessary_InstanceAndTenantIdOnly_NoWarning()
        {
            // Arrange
            var mergedOptions = new MergedOptions
            {
                Instance = "https://login.microsoftonline.com/",
                TenantId = "organizations",
                Logger = _testLogger
            };

            // Act
            MergedOptions.ParseAuthorityIfNecessary(mergedOptions, _testLogger);

            // Assert - No warning should be logged
            Assert.Empty(_testLogger.LogMessages);
            Assert.Equal("https://login.microsoftonline.com/", mergedOptions.Instance);
            Assert.Equal("organizations", mergedOptions.TenantId);
        }

        [Fact]
        public void ParseAuthorityIfNecessary_B2CAuthorityAndInstance_LogsWarning()
        {
            // Arrange - B2C scenario
            var mergedOptions = new MergedOptions
            {
                Authority = "https://fabrikamb2c.b2clogin.com/fabrikamb2c.onmicrosoft.com/b2c_1_susi",
                Instance = "https://fabrikamb2c.b2clogin.com/",
                Logger = _testLogger
            };

            // Act
            MergedOptions.ParseAuthorityIfNecessary(mergedOptions, _testLogger);

            // Assert
            Assert.Single(_testLogger.LogMessages);
            Assert.Contains("Authority", _testLogger.LogMessages[0], StringComparison.OrdinalIgnoreCase);
            Assert.Contains("ignored", _testLogger.LogMessages[0], StringComparison.OrdinalIgnoreCase);
            Assert.Equal(LogLevel.Warning, _testLogger.LogLevel);
        }

        [Fact]
        public void ParseAuthorityIfNecessary_CiamAuthorityAndInstance_LogsWarning()
        {
            // Arrange - CIAM scenario
            var mergedOptions = new MergedOptions
            {
                Authority = "https://contoso.ciamlogin.com/contoso.onmicrosoft.com",
                Instance = "https://contoso.ciamlogin.com/",
                Logger = _testLogger
            };

            // Act
            MergedOptions.ParseAuthorityIfNecessary(mergedOptions, _testLogger);

            // Assert
            Assert.Single(_testLogger.LogMessages);
            Assert.Contains("Authority", _testLogger.LogMessages[0], StringComparison.OrdinalIgnoreCase);
            Assert.Contains("ignored", _testLogger.LogMessages[0], StringComparison.OrdinalIgnoreCase);
            Assert.Equal(LogLevel.Warning, _testLogger.LogLevel);
        }

        [Fact]
        public void ParseAuthorityIfNecessary_CiamPreservedAuthorityWithInstance_LogsWarning()
        {
            // Arrange - CIAM with PreserveAuthority flag
            var mergedOptions = new MergedOptions
            {
                Authority = "https://custom.contoso.com/contoso.onmicrosoft.com",
                Instance = "https://custom.contoso.com/",
                PreserveAuthority = true,
                Logger = _testLogger
            };

            // Act
            MergedOptions.ParseAuthorityIfNecessary(mergedOptions, _testLogger);

            // Assert - Warning should still be logged even with PreserveAuthority
            Assert.Single(_testLogger.LogMessages);
            Assert.Contains("Authority", _testLogger.LogMessages[0], StringComparison.OrdinalIgnoreCase);
            Assert.Contains("ignored", _testLogger.LogMessages[0], StringComparison.OrdinalIgnoreCase);
            Assert.Equal(LogLevel.Warning, _testLogger.LogLevel);
        }

        [Fact]
        public void ParseAuthorityIfNecessary_NoLogger_NoException()
        {
            // Arrange
            var mergedOptions = new MergedOptions
            {
                Authority = "https://login.microsoftonline.com/common",
                Instance = "https://login.microsoftonline.com/",
            };

            // Act & Assert - Should not throw when logger is null
            MergedOptions.ParseAuthorityIfNecessary(mergedOptions, logger: null);
        }

        // Test helper class to capture log messages
        private class TestLogger : ILogger
        {
            public System.Collections.Generic.List<string> LogMessages { get; } = new System.Collections.Generic.List<string>();
            public LogLevel LogLevel { get; private set; }

            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            {
                LogLevel = logLevel;
                LogMessages.Add(formatter(state, exception));
            }
        }
    }
}

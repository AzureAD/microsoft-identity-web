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
        public void ParseAuthorityIfNecessary_AuthorityAndInstance_ThrowsOnConflict()
        {
            // Arrange
            var mergedOptions = new MergedOptions
            {
                Authority = "https://login.microsoftonline.com/common",
                Instance = "https://login.microsoftonline.com/",
                AuthorityExplicitlyConfigured = true,
                Logger = _testLogger
            };

            // Act & Assert
            var ex = Assert.Throws<InvalidOperationException>(
                () => MergedOptions.ParseAuthorityIfNecessary(mergedOptions, _testLogger));
            Assert.Contains("Authority", ex.Message, StringComparison.Ordinal);
            Assert.Contains("conflict", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void ParseAuthorityIfNecessary_AuthorityAndTenantId_ThrowsOnConflict()
        {
            // Arrange
            var mergedOptions = new MergedOptions
            {
                Authority = "https://login.microsoftonline.com/common",
                TenantId = "organizations",
                AuthorityExplicitlyConfigured = true,
                Logger = _testLogger
            };

            // Act & Assert
            var ex = Assert.Throws<InvalidOperationException>(
                () => MergedOptions.ParseAuthorityIfNecessary(mergedOptions, _testLogger));
            Assert.Contains("Authority", ex.Message, StringComparison.Ordinal);
            Assert.Contains("conflict", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void ParseAuthorityIfNecessary_AuthorityAndInstanceAndTenantId_ThrowsOnConflict()
        {
            // Arrange
            var mergedOptions = new MergedOptions
            {
                Authority = "https://login.microsoftonline.com/common",
                Instance = "https://login.microsoftonline.com/",
                TenantId = "organizations",
                AuthorityExplicitlyConfigured = true,
                Logger = _testLogger
            };

            // Act & Assert
            var ex = Assert.Throws<InvalidOperationException>(
                () => MergedOptions.ParseAuthorityIfNecessary(mergedOptions, _testLogger));
            Assert.Contains("Authority", ex.Message, StringComparison.Ordinal);
            Assert.Contains("conflict", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void ParseAuthorityIfNecessary_AuthorityOnly_LogsAuthorityUsedHint()
        {
            // Arrange
            var mergedOptions = new MergedOptions
            {
                Authority = "https://login.microsoftonline.com/common",
                Logger = _testLogger
            };

            // Act
            MergedOptions.ParseAuthorityIfNecessary(mergedOptions, _testLogger);

            // Assert
            // Whenever the single-string 'Authority' option is being used to derive Instance/TenantId,
            // Id.Web emits a warning hinting that first-party (1P) callers
            // (e.g. MISE) should configure Instance + TenantId separately instead. Third-party (3P)
            // callers using CIAM / ADFS / generic OIDC can ignore the warning.
            // The Authority must still be parsed into Instance + TenantId for the legitimate 3P case.
            Assert.Single(_testLogger.LogMessages);
            Assert.Contains("Authority", _testLogger.LogMessages[0], StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Instance", _testLogger.LogMessages[0], StringComparison.OrdinalIgnoreCase);
            Assert.Contains("TenantId", _testLogger.LogMessages[0], StringComparison.OrdinalIgnoreCase);
            Assert.Contains("1P", _testLogger.LogMessages[0], StringComparison.OrdinalIgnoreCase);
            Assert.Equal(LogLevel.Warning, _testLogger.LogLevel);
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
        public void ParseAuthorityIfNecessary_B2CAuthorityAndInstance_ThrowsOnConflict()
        {
            // Arrange - B2C scenario
            var mergedOptions = new MergedOptions
            {
                Authority = "https://fabrikamb2c.b2clogin.com/fabrikamb2c.onmicrosoft.com/b2c_1_susi",
                Instance = "https://fabrikamb2c.b2clogin.com/",
                AuthorityExplicitlyConfigured = true,
                Logger = _testLogger
            };

            // Act & Assert
            var ex = Assert.Throws<InvalidOperationException>(
                () => MergedOptions.ParseAuthorityIfNecessary(mergedOptions, _testLogger));
            Assert.Contains("Authority", ex.Message, StringComparison.Ordinal);
            Assert.Contains("conflict", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void ParseAuthorityIfNecessary_CiamAuthorityAndInstance_ThrowsOnConflict()
        {
            // Arrange - CIAM scenario
            var mergedOptions = new MergedOptions
            {
                Authority = "https://contoso.ciamlogin.com/contoso.onmicrosoft.com",
                Instance = "https://contoso.ciamlogin.com/",
                AuthorityExplicitlyConfigured = true,
                Logger = _testLogger
            };

            // Act & Assert
            var ex = Assert.Throws<InvalidOperationException>(
                () => MergedOptions.ParseAuthorityIfNecessary(mergedOptions, _testLogger));
            Assert.Contains("Authority", ex.Message, StringComparison.Ordinal);
            Assert.Contains("conflict", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void ParseAuthorityIfNecessary_CiamPreservedAuthorityWithInstance_ThrowsOnConflict()
        {
            // Arrange - CIAM with PreserveAuthority flag
            var mergedOptions = new MergedOptions
            {
                Authority = "https://custom.contoso.com/contoso.onmicrosoft.com",
                Instance = "https://custom.contoso.com/",
                PreserveAuthority = true,
                AuthorityExplicitlyConfigured = true,
                Logger = _testLogger
            };

            // Act & Assert
            var ex = Assert.Throws<InvalidOperationException>(
                () => MergedOptions.ParseAuthorityIfNecessary(mergedOptions, _testLogger));
            Assert.Contains("Authority", ex.Message, StringComparison.Ordinal);
            Assert.Contains("conflict", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void ParseAuthorityIfNecessary_NoLogger_StillThrowsOnConflict()
        {
            // Arrange
            var mergedOptions = new MergedOptions
            {
                Authority = "https://login.microsoftonline.com/common",
                Instance = "https://login.microsoftonline.com/",
                AuthorityExplicitlyConfigured = true,
            };

            // Act & Assert - Throws regardless of logger presence
            Assert.Throws<InvalidOperationException>(
                () => MergedOptions.ParseAuthorityIfNecessary(mergedOptions, logger: null));
        }

        [Fact]
        public void ParseAuthorityIfNecessary_SyntheticAuthority_NoThrow()
        {
            // When Authority was backfilled by the computed getter (latch not set),
            // there is no real conflict -- should not throw.
            var mergedOptions = new MergedOptions
            {
                Authority = "https://login.microsoftonline.com/common",
                Instance = "https://login.microsoftonline.com/",
                // AuthorityExplicitlyConfigured intentionally NOT set
                Logger = _testLogger
            };

            // Act & Assert - no exception
            var ex = Record.Exception(() => MergedOptions.ParseAuthorityIfNecessary(mergedOptions, _testLogger));
            Assert.Null(ex);
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

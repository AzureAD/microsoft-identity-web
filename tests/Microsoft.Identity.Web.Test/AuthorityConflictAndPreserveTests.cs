// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.Test.Common;
using Xunit;

namespace Microsoft.Identity.Web.Test
{
    /// <summary>
    /// Tests for authority conflict detection and PreserveAuthority flag behavior.
    /// Issue #3610: Comprehensive conflict warning tests for AAD, B2C, CIAM scenarios.
    /// </summary>
    public class AuthorityConflictAndPreserveTests
    {
        private readonly TestLogger _testLogger;

        public AuthorityConflictAndPreserveTests()
        {
            _testLogger = new TestLogger();
        }

        [Fact]
        public void Conflict_AAD_EmitsSingleWarning()
        {
            // Issue #3610: AAD authority + Instance should emit exactly one warning
            // Arrange
            var mergedOptions = new MergedOptions
            {
                Authority = "https://login.microsoftonline.com/common/v2.0",
                Instance = "https://login.microsoftonline.com/",
                Logger = _testLogger
            };

            // Act
            MergedOptions.ParseAuthorityIfNecessary(mergedOptions, _testLogger);

            // Assert - Should emit exactly one warning
            Assert.Single(_testLogger.LogMessages);
            Assert.Contains("Authority", _testLogger.LogMessages[0], StringComparison.OrdinalIgnoreCase);
            Assert.Contains("ignored", _testLogger.LogMessages[0], StringComparison.OrdinalIgnoreCase);
            Assert.Equal(LogLevel.Warning, _testLogger.LogLevel);
        }

        [Fact]
        public void Conflict_B2C_EmitsSingleWarning()
        {
            // Issue #3610: B2C authority + Instance should emit exactly one warning
            // Arrange
            var mergedOptions = new MergedOptions
            {
                Authority = "https://fabrikamb2c.b2clogin.com/fabrikamb2c.onmicrosoft.com/B2C_1_susi/v2.0",
                Instance = "https://fabrikamb2c.b2clogin.com/",
                Logger = _testLogger
            };

            // Act
            MergedOptions.ParseAuthorityIfNecessary(mergedOptions, _testLogger);

            // Assert - Should emit exactly one warning
            Assert.Single(_testLogger.LogMessages);
            Assert.Contains("Authority", _testLogger.LogMessages[0], StringComparison.OrdinalIgnoreCase);
            Assert.Contains("ignored", _testLogger.LogMessages[0], StringComparison.OrdinalIgnoreCase);
            Assert.Equal(LogLevel.Warning, _testLogger.LogLevel);
        }

        [Fact]
        public void Conflict_CIAM_PreserveAuthority_StillWarnsWhenInstancePresent()
        {
            // Issue #3610: CIAM with PreserveAuthority + Instance should still warn
            // PreserveAuthority affects parsing behavior but doesn't suppress conflict warnings
            // Arrange
            var mergedOptions = new MergedOptions
            {
                Authority = "https://contoso.ciamlogin.com/contoso.onmicrosoft.com",
                Instance = "https://contoso.ciamlogin.com/",
                PreserveAuthority = true,
                Logger = _testLogger
            };

            // Act
            MergedOptions.ParseAuthorityIfNecessary(mergedOptions, _testLogger);

            // Assert - Should emit warning even with PreserveAuthority
            Assert.Single(_testLogger.LogMessages);
            Assert.Contains("Authority", _testLogger.LogMessages[0], StringComparison.OrdinalIgnoreCase);
            Assert.Contains("ignored", _testLogger.LogMessages[0], StringComparison.OrdinalIgnoreCase);
            Assert.Equal(LogLevel.Warning, _testLogger.LogLevel);
        }

        [Fact]
        public void NoConflict_InstanceTenantOnly_NoWarning()
        {
            // Issue #3610: Instance + TenantId without Authority should not warn
            // Arrange
            var mergedOptions = new MergedOptions
            {
                Instance = "https://login.microsoftonline.com/",
                TenantId = "common",
                Logger = _testLogger
            };

            // Act
            MergedOptions.ParseAuthorityIfNecessary(mergedOptions, _testLogger);

            // Assert - No warning should be emitted
            Assert.Empty(_testLogger.LogMessages);
        }

        [Fact]
        public void NoConflict_AuthorityOnly_NoWarning()
        {
            // Issue #3610: Authority only (no Instance or TenantId) should not warn
            // Arrange
            var mergedOptions = new MergedOptions
            {
                Authority = "https://login.microsoftonline.com/common/v2.0",
                Logger = _testLogger
            };

            // Act
            MergedOptions.ParseAuthorityIfNecessary(mergedOptions, _testLogger);

            // Assert - No warning should be emitted, authority should be parsed
            Assert.Empty(_testLogger.LogMessages);
            Assert.Equal("https://login.microsoftonline.com", mergedOptions.Instance);
            Assert.Equal("common", mergedOptions.TenantId);
        }

        [Fact]
        public void Conflict_AuthorityAndTenantId_EmitsSingleWarning()
        {
            // Issue #3610: Authority + TenantId should emit exactly one warning
            // Arrange
            var mergedOptions = new MergedOptions
            {
                Authority = "https://login.microsoftonline.com/common/v2.0",
                TenantId = "organizations",
                Logger = _testLogger
            };

            // Act
            MergedOptions.ParseAuthorityIfNecessary(mergedOptions, _testLogger);

            // Assert - Should emit exactly one warning
            Assert.Single(_testLogger.LogMessages);
            Assert.Contains("Authority", _testLogger.LogMessages[0], StringComparison.OrdinalIgnoreCase);
            Assert.Contains("ignored", _testLogger.LogMessages[0], StringComparison.OrdinalIgnoreCase);
            Assert.Equal(LogLevel.Warning, _testLogger.LogLevel);
        }

        [Fact]
        public void Conflict_AuthorityAndInstanceAndTenantId_EmitsSingleWarning()
        {
            // Issue #3610: All three properties should emit exactly one warning
            // Arrange
            var mergedOptions = new MergedOptions
            {
                Authority = "https://login.microsoftonline.com/common/v2.0",
                Instance = "https://login.microsoftonline.com/",
                TenantId = "organizations",
                Logger = _testLogger
            };

            // Act
            MergedOptions.ParseAuthorityIfNecessary(mergedOptions, _testLogger);

            // Assert - Should emit exactly one warning (not multiple)
            Assert.Single(_testLogger.LogMessages);
            Assert.Contains("Authority", _testLogger.LogMessages[0], StringComparison.OrdinalIgnoreCase);
            Assert.Contains("ignored", _testLogger.LogMessages[0], StringComparison.OrdinalIgnoreCase);
            Assert.Equal(LogLevel.Warning, _testLogger.LogLevel);
        }

        // Test helper class to capture log messages
        private class TestLogger : ILogger
        {
            public List<string> LogMessages { get; } = new List<string>();
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

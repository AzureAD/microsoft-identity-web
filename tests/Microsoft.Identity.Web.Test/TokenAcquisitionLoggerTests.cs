// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Microsoft.Identity.Web.Test
{
    public class TokenAcquisitionLoggerTests
    {
        private readonly TestLogger<TokenAcquisition> _logger;

        public TokenAcquisitionLoggerTests()
        {
            _logger = new TestLogger<TokenAcquisition>();
        }

        [Fact]
        public void TokenAcquisitionError_LogsCorrectMessage()
        {
            // Arrange
            string errorMessage = "Test error message";
            var exception = new Exception("Test exception");

            // Act
            TokenAcquisition.Logger.TokenAcquisitionError(_logger, errorMessage, exception);

            // Assert
            var logEntry = Assert.Single(_logger.LogEntries);
            Assert.Equal(LogLevel.Information, logEntry.LogLevel);
            Assert.Contains(errorMessage, logEntry.Message, StringComparison.OrdinalIgnoreCase);
            Assert.Same(exception, logEntry.Exception);
        }

        [Fact]
        public void TokenAcquisitionMsalAuthenticationResultTime_LogsCorrectMessage()
        {
            // Arrange
            long durationTotalInMs = 100;
            long durationInHttpInMs = 70;
            long durationInCacheInMs = 30;
            string tokenSource = "cache";
            string correlationId = "corr-id-123";
            string cacheRefreshReason = "refresh reason";
            var exception = new Exception("Test exception");

            // Act
            TokenAcquisition.Logger.TokenAcquisitionMsalAuthenticationResultTime(
                _logger,
                durationTotalInMs,
                durationInHttpInMs,
                durationInCacheInMs,
                tokenSource,
                correlationId,
                cacheRefreshReason,
                exception);

            // Assert
            var logEntry = Assert.Single(_logger.LogEntries);
            Assert.Equal(LogLevel.Debug, logEntry.LogLevel);

            // Verify all values are included in the log message
            Assert.Contains($"DurationTotalInMs: {durationTotalInMs}", logEntry.Message, StringComparison.OrdinalIgnoreCase);
            Assert.Contains($"DurationInHttpInMs: {durationInHttpInMs}", logEntry.Message, StringComparison.OrdinalIgnoreCase);
            Assert.Contains($"DurationInCacheInMs: {durationInCacheInMs}", logEntry.Message, StringComparison.OrdinalIgnoreCase);
            Assert.Contains($"TokenSource: {tokenSource}", logEntry.Message, StringComparison.OrdinalIgnoreCase);
            Assert.Contains($"CorrelationId: {correlationId}", logEntry.Message, StringComparison.OrdinalIgnoreCase);
            Assert.Contains($"CacheRefreshReason: {cacheRefreshReason}", logEntry.Message, StringComparison.OrdinalIgnoreCase);
            Assert.Same(exception, logEntry.Exception);
        }

        [Fact]
        public void TokenAcquisitionError_WithNullException_LogsCorrectly()
        {
            // Arrange
            string errorMessage = "Test error message with null exception";

            // Act
            TokenAcquisition.Logger.TokenAcquisitionError(_logger, errorMessage, null);

            // Assert
            var logEntry = Assert.Single(_logger.LogEntries);
            Assert.Equal(LogLevel.Information, logEntry.LogLevel);
            Assert.Contains(errorMessage, logEntry.Message, StringComparison.OrdinalIgnoreCase);
            Assert.Null(logEntry.Exception);
        }

        [Fact]
        public void TokenAcquisitionMsalAuthenticationResultTime_WithNullException_LogsCorrectly()
        {
            // Arrange
            long durationTotalInMs = 100;
            long durationInHttpInMs = 70;
            long durationInCacheInMs = 30;
            string tokenSource = "cache";
            string correlationId = "corr-id-123";
            string cacheRefreshReason = "refresh reason";

            // Act
            TokenAcquisition.Logger.TokenAcquisitionMsalAuthenticationResultTime(
                _logger,
                durationTotalInMs,
                durationInHttpInMs,
                durationInCacheInMs,
                tokenSource,
                correlationId,
                cacheRefreshReason,
                null);

            // Assert
            var logEntry = Assert.Single(_logger.LogEntries);
            Assert.Equal(LogLevel.Debug, logEntry.LogLevel);
            Assert.Contains($"TokenSource: {tokenSource}", logEntry.Message, StringComparison.OrdinalIgnoreCase);
            Assert.Null(logEntry.Exception);
        }

        // Simple logger implementation for testing
        private class TestLogger<T> : ILogger<T>, ILogger
        {
            public List<LogEntry> LogEntries { get; } = new List<LogEntry>();

            IDisposable ILogger.BeginScope<TState>(TState state) => null!;

            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(
                LogLevel logLevel,
                EventId eventId,
                TState state,
                Exception? exception,
                Func<TState, Exception?, string> formatter)
            {
                LogEntries.Add(new LogEntry
                {
                    LogLevel = logLevel,
                    EventId = eventId,
                    Message = formatter(state, exception),
                    Exception = exception
                });
            }
        }

        private class LogEntry
        {
            public LogLevel LogLevel { get; set; }
            public EventId EventId { get; set; }
            public string Message { get; set; } = string.Empty;
            public Exception? Exception { get; set; }
        }
    }
}

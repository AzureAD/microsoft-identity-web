// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Microsoft.Extensions.Logging;

namespace Microsoft.Identity.Web.Test.Common.Mocks
{
    /// <summary>
    /// Wrapper class around the ASP.NET Core ILogger to enable assertion with NSubstitute because ILogger internally uses an internal struct FormattedLogValues which can't be mocked.
    /// In the Log method, the generic type parameter is forced to be an Object, which can then be matched by NSubstitute.
    /// </summary>
    /// <remarks>
    /// <see cref="Microsoft.Extensions.Logging.ILogger"/> has only one logging method - Log.
    /// LogDebug, LogInformation, LogError, etc. are actually extension methods located in <see cref="Microsoft.Extensions.Logging.LoggerExtensions"/>, which all end up calling the Log method in the ILogger.
    /// Since extension methods are concrete, NSubstitute cannot mock them.
    /// Subsequently Log method should be mocked; however, LoggerExtensions class passes a FormattedLogValues internal struct to the Log method.
    /// This prevents NSubstitute from successfully matching the method calls by parameters.
    /// More information:
    /// https://github.com/dotnet/extensions/issues?q=FormattedLogValues
    /// https://github.com/dotnet/extensions/issues/1319
    /// https://github.com/nsubstitute/NSubstitute/issues/597
    /// https://github.com/moq/moq4/issues/918.
    /// </remarks>
#pragma warning disable CS8603 // Possible null reference return.
#pragma warning disable CS8714 // The type cannot be used as type parameter in the generic type or method. Nullability of type argument doesn't match 'notnull' constraint.
#pragma warning disable CS8633 // Nullability in constraints for type parameter doesn't match the constraints for type parameter in implicitly implemented interface method'.
#pragma warning disable CS8604 // Possible null reference argument.
#pragma warning disable CS8767 // Nullability of reference types in type of parameter doesn't match implicitly implemented member (possibly because of nullability attributes).
    public class LoggerMock<T> : ILogger<T>
    {
        private ILogger<T> _logger;

        public LoggerMock(ILogger<T> logger) => _logger = logger;

        public IDisposable BeginScope<TState>(TState state) => _logger.BeginScope<TState>(state);

        public bool IsEnabled(LogLevel logLevel) => _logger.IsEnabled(logLevel);

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
            => _logger.Log<object>(logLevel, eventId, state, exception, (s, e) => string.Empty);
    }
#pragma warning restore CS8603 // Possible null reference return.
#pragma warning restore CS8714 // The type cannot be used as type parameter in the generic type or method. Nullability of type argument doesn't match 'notnull' constraint.
#pragma warning restore CS8633 // Nullability in constraints for type parameter doesn't match the constraints for type parameter in implicitly implemented interface method'.
#pragma warning restore CS8604 // Possible null reference argument.
#pragma warning restore CS8767 // Nullability of reference types in type of parameter doesn't match implicitly implemented member (possibly because of nullability attributes).
}

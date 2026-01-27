// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Extensions.Configuration;

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// LoggingOptions class for passing in Identity specific logging options.
    /// </summary>
    internal sealed class LoggingOptions
    {
        /// <summary>
        /// Enable Pii Logging from configuration.
        /// Default is false.
        /// </summary>
        public bool EnablePiiLogging { get; set; }

        /// <summary>
        /// Creates a <see cref="LoggingOptions"/> instance from the specified configuration section.
        /// </summary>
        /// <param name="configurationSection">The configuration section containing the values.</param>
        /// <returns>A new <see cref="LoggingOptions"/> instance with values bound from configuration.</returns>
        internal static LoggingOptions FromConfiguration(IConfigurationSection? configurationSection)
        {
            var options = new LoggingOptions();
            if (configurationSection == null)
            {
                return options;
            }

            var enablePiiLoggingValue = configurationSection[nameof(EnablePiiLogging)];
            if (!string.IsNullOrEmpty(enablePiiLoggingValue) && bool.TryParse(enablePiiLoggingValue, out bool enablePiiLogging))
            {
                options.EnablePiiLogging = enablePiiLogging;
            }

            return options;
        }
    }
}

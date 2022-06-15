// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// LoggingOptions class for passing in Identity specific logging options.
    /// </summary>
    public class LoggingOptions
    {
        /// <summary>
        /// Enable Pii Logging from configuration.
        /// Default is false.
        /// </summary>
        public bool EnablePiiLogging { get; set; }
    }
}

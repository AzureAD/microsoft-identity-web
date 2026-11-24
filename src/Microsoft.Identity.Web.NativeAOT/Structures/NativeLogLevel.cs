// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Identity.Web.NativeAOT;

/// <summary>
/// Native diagnostic severity values matching Microsoft.Extensions.Logging.LogLevel.
/// </summary>
public enum NativeLogLevel
{
    /// <summary>
    /// The most detailed diagnostic severity.
    /// </summary>
    Trace = 0,

    /// <summary>
    /// Diagnostic information intended for development and investigation.
    /// </summary>
    Debug = 1,

    /// <summary>
    /// Normal configuration and operation outcomes.
    /// </summary>
    Information = 2,

    /// <summary>
    /// An abnormal event that does not stop the current operation.
    /// </summary>
    Warning = 3,

    /// <summary>
    /// A failure of the current configuration or operation.
    /// </summary>
    Error = 4,

    /// <summary>
    /// A catastrophic failure requiring immediate attention.
    /// </summary>
    Critical = 5,

    /// <summary>
    /// A filtering sentinel that is never emitted as a diagnostic severity.
    /// </summary>
    None = 6,
}

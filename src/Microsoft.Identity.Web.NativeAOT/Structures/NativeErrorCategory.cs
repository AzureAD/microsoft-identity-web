// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Identity.Web.NativeAOT;

/// <summary>
/// Stable native operation and submission outcome categories.
/// </summary>
public enum NativeErrorCategory
{
    /// <summary>
    /// The operation or submission succeeded.
    /// </summary>
    None = 0,

    /// <summary>
    /// Acquisition configuration or client initialization is invalid.
    /// </summary>
    Configuration = 1,

    /// <summary>
    /// A configured credential could not be loaded or used.
    /// </summary>
    Credential = 2,

    /// <summary>
    /// Authority, token-endpoint or downstream communication failed.
    /// </summary>
    Transport = 3,

    /// <summary>
    /// An observed token-endpoint response rejected the request.
    /// </summary>
    TokenEndpoint = 4,

    /// <summary>
    /// The host must handle required interaction or a claims challenge.
    /// </summary>
    InteractionRequired = 5,

    /// <summary>
    /// Cancellation or the operation deadline stopped the request.
    /// </summary>
    Cancelled = 6,

    /// <summary>
    /// The requested protocol or operation is unsupported.
    /// </summary>
    Unsupported = 7,

    /// <summary>
    /// An unexpected recoverable internal failure occurred.
    /// </summary>
    Internal = 8,

    /// <summary>
    /// No successful configuration is available.
    /// </summary>
    NotConfigured = 9,

    /// <summary>
    /// An input value, request envelope or required context is invalid.
    /// </summary>
    InvalidRequest = 10,

    /// <summary>
    /// The downstream API returned a non-success status; inspect its separate status and body.
    /// </summary>
    DownstreamApi = 11,
}

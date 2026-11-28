// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Runtime.InteropServices;

namespace Microsoft.Identity.Web.NativeAOT;

/// <summary>
/// Owned HTTP outcome; embedded data shares the result allocation and must not be freed separately.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public unsafe struct NativeDownstreamApiResult
{
    /// <summary>
    /// Operation HTTP status for the host response; available even when no downstream response was received.
    /// </summary>
    public int HttpResponseStatusCode;

    /// <summary>
    /// Correlation GUID for the operation.
    /// </summary>
    public byte* CorrelationId;

    /// <summary>
    /// Optional managed exception type/message or downstream error body; may contain sensitive data.
    /// </summary>
    public byte* ErrorDescription;

    /// <summary>
    /// Optional UTF-8 WWW-Authenticate header for an acquisition challenge or downstream response.
    /// </summary>
    public byte* WWWAuthenticate;

    /// <summary>
    /// Optional zero-terminated UTF-8 response body, limited to 16 MiB.
    /// </summary>
    public byte* DownstreamApiResult;
}

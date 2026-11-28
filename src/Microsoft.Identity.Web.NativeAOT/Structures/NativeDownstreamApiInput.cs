// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Runtime.InteropServices;

namespace Microsoft.Identity.Web.NativeAOT;

/// <summary>
/// Borrowed HTTPS request with UTF-8 strings and an optional text body for a named downstream service.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public unsafe struct NativeDownstreamApiInput
{
    /// <summary>
    /// Optional caller correlation GUID; NULL/blank inherits valid host context, while invalid nonblank values generate an ID.
    /// </summary>
    public byte* CorrelationId;

    /// <summary>
    /// Optional request media type; content defaults to application/json.
    /// </summary>
    public byte* ContentType;

    /// <summary>
    /// Optional borrowed zero-terminated UTF-8 request body, limited to 1 MiB; NULL omits the body.
    /// </summary>
    public byte* InputBody;

    /// <summary>
    /// Optional Accept header value.
    /// </summary>
    public byte* AcceptHeader;

    /// <summary>
    /// Token and acquisition overrides shared with authorization-header inputs.
    /// </summary>
    public NativeDownstreamApiOptionsOverrides Overrides;
}

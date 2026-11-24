// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Runtime.InteropServices;

namespace Microsoft.Identity.Web.NativeAOT;

/// <summary>
/// Borrowed HTTPS request with UTF-8 strings and an optional binary body for a named downstream service.
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
    /// Borrowed binary request bytes, limited to 1 MiB.
    /// </summary>
    public byte* Body;

    /// <summary>
    /// Body byte count; must be zero when the body pointer is NULL.
    /// </summary>
    public uint BodyLength;

    /// <summary>
    /// Optional Accept header value.
    /// </summary>
    public byte* AcceptHeader;

    /// <summary>
    /// Optional path override constrained to the configured HTTPS origin.
    /// </summary>
    public byte* RelativePath;

    /// <summary>
    /// Optional HTTP method override; otherwise the configured method or GET.
    /// </summary>
    public byte* Method;

    /// <summary>
    /// Token and acquisition overrides shared with authorization-header inputs.
    /// </summary>
    public NativeDownstreamApiOptionsOverrides Overrides;
}

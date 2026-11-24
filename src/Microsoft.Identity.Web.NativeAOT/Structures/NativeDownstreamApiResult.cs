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
    /// Complete operation outcome and token acquisition metadata.
    /// </summary>
    public NativeAcquireTokenResult Token;

    /// <summary>
    /// One when a downstream HTTP status was observed; otherwise zero.
    /// </summary>
    public uint HasHttpResponseStatus;

    /// <summary>
    /// Downstream status code, valid only when its availability flag is one.
    /// </summary>
    public int HttpResponseStatusCode;

    /// <summary>
    /// Binary response bytes, limited to 16 MiB; not zero-terminated.
    /// </summary>
    public byte* Body;

    /// <summary>
    /// Number of available response bytes.
    /// </summary>
    public uint BodyLength;

    /// <summary>
    /// Optional zero-terminated UTF-8 response media type.
    /// </summary>
    public byte* ContentType;

    /// <summary>
    /// Optional untrusted UTF-8 downstream authentication challenge.
    /// </summary>
    public byte* WwwAuthenticate;

    /// <summary>
    /// Optional UTF-8 acquisition and HTTP response metadata JSON; may contain secrets, never log.
    /// </summary>
    public byte* MetadataJson;
}

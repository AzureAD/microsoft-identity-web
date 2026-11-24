// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Runtime.InteropServices;

namespace Microsoft.Identity.Web.NativeAOT;

/// <summary>
/// Borrowed UTF-8 settings for an SDK-signed proof-of-possession HTTP request.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public unsafe struct NativePoPOptions
{
    /// <summary>
    /// HTTP method bound into the proof.
    /// </summary>
    public byte* HttpMethod;

    /// <summary>
    /// Destination host bound into the proof.
    /// </summary>
    public byte* HttpHost;

    /// <summary>
    /// Destination path bound into the proof.
    /// </summary>
    public byte* HttpPath;

    /// <summary>
    /// Optional server nonce bound into the proof.
    /// </summary>
    public byte* Nonce;

    /// <summary>
    /// Whether to sign the request rather than return the unsigned bound token.
    /// </summary>
    public bool SignHttpRequest;
}

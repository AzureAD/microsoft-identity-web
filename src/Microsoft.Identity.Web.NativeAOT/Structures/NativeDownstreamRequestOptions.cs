// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Runtime.InteropServices;

namespace Microsoft.Identity.Web.NativeAOT;

/// <summary>
/// Borrowed request-local HTTP overrides, not valid for header-only acquisition.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public unsafe struct NativeDownstreamRequestOptions
{
    /// <summary>
    /// Whether to enable Graph authentication, overriding the service default when this structure is supplied.
    /// </summary>
    public bool GraphAuthentication;

    /// <summary>
    /// Additional downstream HTTP headers, distinct from token-endpoint headers.
    /// </summary>
    public NativeStringParameters ExtraHeaderParameters;

    /// <summary>
    /// Additional downstream URL query parameters, distinct from token-endpoint parameters.
    /// </summary>
    public NativeStringParameters ExtraQueryParameters;
}

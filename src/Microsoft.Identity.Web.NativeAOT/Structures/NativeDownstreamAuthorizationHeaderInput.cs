// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Runtime.InteropServices;

namespace Microsoft.Identity.Web.NativeAOT;

/// <summary>
/// Borrowed UTF-8 header request with correlation and embedded acquisition overrides.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public unsafe struct NativeDownstreamAuthorizationHeaderInput
{
    /// <summary>
    /// Optional caller correlation GUID; NULL/blank inherits valid host context, while invalid nonblank values generate an ID.
    /// </summary>
    public byte* CorrelationId;

    /// <summary>
    /// Token and acquisition overrides shared with downstream HTTP inputs.
    /// </summary>
    public NativeDownstreamApiOptionsOverrides Overrides;
}

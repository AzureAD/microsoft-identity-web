// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Runtime.InteropServices;

namespace Microsoft.Identity.Web.NativeAOT;

/// <summary>
/// Owned outcome of configuration, logging registration or operation control.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public unsafe struct NativeControlResult
{
    /// <summary>
    /// Control outcome: zero succeeds, 100 indicates a configuration error and 200 a logging callback error.
    /// </summary>
    public int ErrorCode;

    /// <summary>
    /// Optional redacted zero-terminated UTF-8 explanation stored in the result allocation.
    /// </summary>
    public byte* ErrorDescription;
}

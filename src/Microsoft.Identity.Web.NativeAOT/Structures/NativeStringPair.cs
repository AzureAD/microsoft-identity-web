// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Runtime.InteropServices;

namespace Microsoft.Identity.Web.NativeAOT;

/// <summary>
/// A borrowed pair of required zero-terminated UTF-8 parameter strings.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public unsafe struct NativeStringPair
{
    /// <summary>
    /// The non-NULL parameter name.
    /// </summary>
    public byte* Name;

    /// <summary>
    /// The non-NULL parameter value.
    /// </summary>
    public byte* Value;
}

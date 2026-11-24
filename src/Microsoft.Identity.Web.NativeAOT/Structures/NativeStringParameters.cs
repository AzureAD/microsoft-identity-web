// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Runtime.InteropServices;

namespace Microsoft.Identity.Web.NativeAOT;

/// <summary>
/// Borrowed parameter dictionary; NULL/zero inherits defaults and non-NULL/zero selects an empty dictionary.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public unsafe struct NativeStringParameters
{
    /// <summary>
    /// Pointer to the parameter pairs; required when the count is nonzero.
    /// </summary>
    public NativeStringPair* Entries;

    /// <summary>
    /// Number of pairs, limited to 128.
    /// </summary>
    public uint Count;
}

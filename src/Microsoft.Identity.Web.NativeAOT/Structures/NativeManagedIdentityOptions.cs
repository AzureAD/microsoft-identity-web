// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Runtime.InteropServices;

namespace Microsoft.Identity.Web.NativeAOT;

/// <summary>
/// Borrowed UTF-8 selector for system-assigned or user-assigned managed identity.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public unsafe struct NativeManagedIdentityOptions
{
    /// <summary>
    /// Optional user-assigned client ID; NULL selects system-assigned identity.
    /// </summary>
    public byte* UserAssignedClientId;
}

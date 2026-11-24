// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Runtime.InteropServices;

namespace Microsoft.Identity.Web.NativeAOT;

/// <summary>
/// Owned native log message transferred to a host callback.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public unsafe struct NativeLoggingCallbackContext
{
    /// <summary>
    /// Redacted zero-terminated UTF-8 diagnostic text stored in the context allocation.
    /// </summary>
    public byte* Message;

    /// <summary>
    /// Diagnostic severity; <see cref="NativeLogLevel.None"/> is never emitted.
    /// </summary>
    public NativeLogLevel Level;
}

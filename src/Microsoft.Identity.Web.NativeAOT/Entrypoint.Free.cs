// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Runtime.InteropServices;

namespace Microsoft.Identity.Web.NativeAOT;

public static unsafe partial class Entrypoint
{
    /// <summary>
    /// Releases an owned authorization header result; a null pointer is permitted.
    /// </summary>
    /// <param name="authorizationHeaderResult">Owned result to release exactly once.</param>
    [UnmanagedCallersOnly(EntryPoint = nameof(EntraAuthFreeDownstreamAuthorizationHeaderResult))]
    public static void EntraAuthFreeDownstreamAuthorizationHeaderResult(NativeDownstreamAuthorizationHeaderResult* authorizationHeaderResult)
    {
        FreeResult(authorizationHeaderResult);
    }

    /// <summary>
    /// Releases an owned downstream call result; a null pointer is permitted.
    /// </summary>
    /// <param name="downstreamApiResult">Owned result to release exactly once.</param>
    [UnmanagedCallersOnly(EntryPoint = nameof(EntraAuthFreeDownstreamApiResult))]
    public static void EntraAuthFreeDownstreamApiResult(NativeDownstreamApiResult* downstreamApiResult)
    {
        FreeResult(downstreamApiResult);
    }

    /// <summary>
    /// Releases an owned control result; a null pointer is permitted.
    /// </summary>
    /// <param name="controlResult">Owned result to release exactly once.</param>
    [UnmanagedCallersOnly(EntryPoint = nameof(EntraAuthFreeControlResult))]
    public static void EntraAuthFreeControlResult(NativeControlResult* controlResult)
    {
        FreeResult(controlResult);
    }

    /// <summary>
    /// Releases an owned log message context; a null pointer is permitted.
    /// </summary>
    /// <param name="callbackContext">Owned context to release exactly once.</param>
    [UnmanagedCallersOnly(EntryPoint = nameof(EntraAuthFreeLogMessageCallbackContext))]
    public static void EntraAuthFreeLogMessageCallbackContext(NativeLogMessageCallbackContext* callbackContext)
    {
        FreeResult(callbackContext);
    }

    private static void FreeResult<T>(T* result) where T : unmanaged
    {
        if (result is not null)
        {
            new Span<byte>(result, sizeof(T)).Clear();
            Marshal.FreeCoTaskMem((IntPtr)result);
        }
    }
}

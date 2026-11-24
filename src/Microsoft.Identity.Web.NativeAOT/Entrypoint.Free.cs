// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Runtime.InteropServices;

namespace Microsoft.Identity.Web.NativeAOT;

internal static unsafe partial class Entrypoint
{
    [UnmanagedCallersOnly(EntryPoint = nameof(EntraAuthFreeAuthorizationHeaderResult))]
    private static void EntraAuthFreeAuthorizationHeaderResult(NativeAuthorizationHeaderResult* result)
    {
        FreeResult(result);
    }

    [UnmanagedCallersOnly(EntryPoint = nameof(EntraAuthFreeDownstreamApiResult))]
    private static void EntraAuthFreeDownstreamApiResult(NativeDownstreamApiResult* result)
    {
        FreeResult(result);
    }

    [UnmanagedCallersOnly(EntryPoint = nameof(EntraAuthFreeAsyncSubmissionResult))]
    private static void EntraAuthFreeAsyncSubmissionResult(NativeAsyncSubmissionResult* result)
    {
        FreeResult(result);
    }

    [UnmanagedCallersOnly(EntryPoint = nameof(EntraAuthFreeControlResult))]
    private static void EntraAuthFreeControlResult(NativeControlResult* result)
    {
        FreeResult(result);
    }

    [UnmanagedCallersOnly(EntryPoint = nameof(EntraAuthFreeLoggingCallbackContext))]
    private static void EntraAuthFreeLoggingCallbackContext(NativeLoggingCallbackContext* result)
    {
        FreeResult(result);
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

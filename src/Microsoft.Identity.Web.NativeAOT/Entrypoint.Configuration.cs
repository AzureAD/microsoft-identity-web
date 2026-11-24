// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Runtime.InteropServices;

namespace Microsoft.Identity.Web.NativeAOT;

internal static unsafe partial class Entrypoint
{
    private static readonly NativeRuntime s_runtime = new();

    [UnmanagedCallersOnly(EntryPoint = nameof(EntraAuthConfigure))]
    private static NativeControlResult* EntraAuthConfigure(byte* configurationJson, byte* sectionName)
    {
        try
        {
            s_runtime.Configure();
            return AllocateControl(success: true);
        }
        catch (Exception exception)
        {
            Environment.FailFast("Native AOT configuration failed.", exception);
            return null;
        }
    }

    [UnmanagedCallersOnly(EntryPoint = nameof(EntraAuthAssignLogMessageCallback))]
    private static NativeControlResult* EntraAuthAssignLogMessageCallback(
        delegate* unmanaged<NativeLoggingCallbackContext*, void*, void> callback, void* context)
    {
        return AllocateControl(success: false);
    }

    [UnmanagedCallersOnly(EntryPoint = nameof(EntraAuthUnassignLogMessageCallback))]
    private static NativeControlResult* EntraAuthUnassignLogMessageCallback()
    {
        return AllocateControl(success: false);
    }

    [UnmanagedCallersOnly(EntryPoint = nameof(EntraAuthCancelOperation))]
    private static NativeControlResult* EntraAuthCancelOperation(ulong operationId)
    {
        return AllocateControl(success: false);
    }

    private static NativeControlResult* AllocateControl(bool success)
    {
        try
        {
            ReadOnlySpan<byte> message = "Not implemented."u8;
            int size = sizeof(NativeControlResult) + (success ? 0 : message.Length + 1);
            var result = (NativeControlResult*)Marshal.AllocCoTaskMem(size);
            new Span<byte>(result, size).Clear();
            result->ErrorCode = success ? 0 : 2;
            if (!success)
            {
                result->ErrorDescription = (byte*)(result + 1);
                message.CopyTo(new Span<byte>(result->ErrorDescription, message.Length));
            }

            return result;
        }
        catch (Exception exception)
        {
            Environment.FailFast("Cannot allocate a Native AOT control result.", exception);
            return null;
        }
    }
}

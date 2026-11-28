// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Runtime.InteropServices;

namespace Microsoft.Identity.Web.NativeAOT;

public static unsafe partial class Entrypoint
{
    private const int CallbackErrorCode = 200;
    private static readonly NativeRuntime s_runtime = new();

    /// <summary>
    /// Entry point for configuring the native acquisition environment.
    /// </summary>
    /// <param name="configurationJson">Borrowed zero-terminated UTF-8 JSON configuration.</param>
    /// <param name="sectionName">Borrowed zero-terminated UTF-8 configuration section name.</param>
    /// <returns>An owned control result released with <see cref="EntraAuthFreeControlResult"/>.</returns>
    [UnmanagedCallersOnly(EntryPoint = nameof(EntraAuthConfigure))]
    public static NativeControlResult* EntraAuthConfigure(byte* configurationJson, byte* sectionName)
    {
        try
        {
            s_runtime.Configure();
            return AllocateControl(errorCode: 0, errorDescription: default);
        }
        catch (Exception exception)
        {
            Environment.FailFast("Native AOT configuration failed.", exception);
            return null;
        }
    }

    /// <summary>
    /// Entry point for assigning a native logging callback.
    /// </summary>
    /// <param name="callback">Host callback receiving ownership of each log message context.</param>
    /// <param name="context">Opaque host context supplied to the logging callback.</param>
    /// <returns>An owned control result released with <see cref="EntraAuthFreeControlResult"/>.</returns>
    [UnmanagedCallersOnly(EntryPoint = nameof(EntraAuthAssignLogMessageCallback))]
    public static NativeControlResult* EntraAuthAssignLogMessageCallback(
        delegate* unmanaged<NativeLogMessageCallbackContext*, void*, void> callback, void* context)
    {
        return AllocateControl(CallbackErrorCode, "Not implemented."u8);
    }

    /// <summary>
    /// Entry point for unassigning the native logging callback.
    /// </summary>
    /// <returns>An owned control result released with <see cref="EntraAuthFreeControlResult"/>.</returns>
    [UnmanagedCallersOnly(EntryPoint = nameof(EntraAuthUnassignLogMessageCallback))]
    public static NativeControlResult* EntraAuthUnassignLogMessageCallback()
    {
        return AllocateControl(CallbackErrorCode, "Not implemented."u8);
    }

    private static NativeControlResult* AllocateControl(int errorCode, ReadOnlySpan<byte> errorDescription)
    {
        try
        {
            int size = sizeof(NativeControlResult) + (errorDescription.IsEmpty ? 0 : errorDescription.Length + 1);
            var result = (NativeControlResult*)Marshal.AllocCoTaskMem(size);
            new Span<byte>(result, size).Clear();
            result->ErrorCode = errorCode;
            if (!errorDescription.IsEmpty)
            {
                result->ErrorDescription = (byte*)(result + 1);
                errorDescription.CopyTo(new Span<byte>(result->ErrorDescription, errorDescription.Length));
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

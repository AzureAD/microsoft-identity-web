// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Runtime.InteropServices;

namespace Microsoft.Identity.Web.NativeAOT;

/// <summary>
/// Unmanaged entry points for acquisition, downstream calls, configuration and result ownership.
/// </summary>
/// <remarks>
/// These methods cannot be called directly from managed code. Invoke them from native code
/// or through unmanaged function pointers.
/// </remarks>
public static unsafe partial class Entrypoint
{
    // Inputs are deliberately not read until acquisition is implemented.
    /// <summary>
    /// Entry point for authorization header acquisition with a host-supplied user context.
    /// </summary>
    /// <param name="validationResult">Borrowed host authentication context containing the subject token.</param>
    /// <param name="downstreamApiInput">Borrowed per-request header acquisition overrides.</param>
    /// <param name="downstreamApiName">Borrowed zero-terminated UTF-8 service name.</param>
    /// <returns>An owned result released with <see cref="EntraAuthFreeDownstreamAuthorizationHeaderResult"/>.</returns>
    [UnmanagedCallersOnly(EntryPoint = nameof(EntraAuthGetAuthorizationHeader))]
    public static NativeDownstreamAuthorizationHeaderResult* EntraAuthGetAuthorizationHeader(
        NativeValidationResult* validationResult, NativeDownstreamAuthorizationHeaderInput* downstreamApiInput, byte* downstreamApiName)
    {
        return AllocateHeaderResult();
    }

    /// <summary>
    /// Entry point for authorization header acquisition without a user context.
    /// </summary>
    /// <param name="downstreamApiInput">Borrowed per-request header acquisition overrides.</param>
    /// <param name="downstreamApiName">Borrowed zero-terminated UTF-8 service name.</param>
    /// <returns>An owned result released with <see cref="EntraAuthFreeDownstreamAuthorizationHeaderResult"/>.</returns>
    [UnmanagedCallersOnly(EntryPoint = nameof(EntraAuthGetAuthorizationHeaderUnauthenticated))]
    public static NativeDownstreamAuthorizationHeaderResult* EntraAuthGetAuthorizationHeaderUnauthenticated(
        NativeDownstreamAuthorizationHeaderInput* downstreamApiInput, byte* downstreamApiName)
    {
        return AllocateHeaderResult();
    }

    /// <summary>
    /// Entry point for a downstream call with a host-supplied user context.
    /// </summary>
    /// <param name="validationResult">Borrowed host authentication context containing the subject token.</param>
    /// <param name="downstreamApiInput">Borrowed per-request HTTP input and acquisition overrides.</param>
    /// <param name="downstreamApiName">Borrowed zero-terminated UTF-8 service name.</param>
    /// <returns>An owned result released with <see cref="EntraAuthFreeDownstreamApiResult"/>.</returns>
    [UnmanagedCallersOnly(EntryPoint = nameof(EntraAuthCallDownstreamApi))]
    public static NativeDownstreamApiResult* EntraAuthCallDownstreamApi(
        NativeValidationResult* validationResult, NativeDownstreamApiInput* downstreamApiInput, byte* downstreamApiName)
    {
        return AllocateDownstreamResult();
    }

    /// <summary>
    /// Entry point for a downstream call without a user context.
    /// </summary>
    /// <param name="downstreamApiInput">Borrowed per-request HTTP input and acquisition overrides.</param>
    /// <param name="downstreamApiName">Borrowed zero-terminated UTF-8 service name.</param>
    /// <returns>An owned result released with <see cref="EntraAuthFreeDownstreamApiResult"/>.</returns>
    [UnmanagedCallersOnly(EntryPoint = nameof(EntraAuthCallDownstreamApiUnauthenticated))]
    public static NativeDownstreamApiResult* EntraAuthCallDownstreamApiUnauthenticated(
        NativeDownstreamApiInput* downstreamApiInput, byte* downstreamApiName)
    {
        return AllocateDownstreamResult();
    }

    /// <summary>
    /// Entry point for callback-based header acquisition with a host-supplied user context.
    /// </summary>
    /// <param name="validationResult">Borrowed host authentication context containing the subject token.</param>
    /// <param name="downstreamApiInput">Borrowed per-request header acquisition overrides.</param>
    /// <param name="downstreamApiName">Borrowed zero-terminated UTF-8 service name.</param>
    /// <param name="callback">Required completion callback receiving ownership of the result; may run inline.</param>
    /// <param name="callerContext">Opaque host context passed unchanged to the callback.</param>
    [UnmanagedCallersOnly(EntryPoint = nameof(EntraAuthGetAuthorizationHeaderAsync))]
    public static void EntraAuthGetAuthorizationHeaderAsync(
        NativeValidationResult* validationResult, NativeDownstreamAuthorizationHeaderInput* downstreamApiInput, byte* downstreamApiName,
        delegate* unmanaged<NativeDownstreamAuthorizationHeaderResult*, void*, void> callback, void* callerContext)
    {
        Complete(callback, AllocateHeaderResult(), callerContext);
    }

    /// <summary>
    /// Entry point for callback-based header acquisition without a user context.
    /// </summary>
    /// <param name="downstreamApiInput">Borrowed per-request header acquisition overrides.</param>
    /// <param name="downstreamApiName">Borrowed zero-terminated UTF-8 service name.</param>
    /// <param name="callback">Required completion callback receiving ownership of the result; may run inline.</param>
    /// <param name="callerContext">Opaque host context passed unchanged to the callback.</param>
    [UnmanagedCallersOnly(EntryPoint = nameof(EntraAuthGetAuthorizationHeaderUnauthenticatedAsync))]
    public static void EntraAuthGetAuthorizationHeaderUnauthenticatedAsync(
        NativeDownstreamAuthorizationHeaderInput* downstreamApiInput, byte* downstreamApiName,
        delegate* unmanaged<NativeDownstreamAuthorizationHeaderResult*, void*, void> callback, void* callerContext)
    {
        Complete(callback, AllocateHeaderResult(), callerContext);
    }

    /// <summary>
    /// Entry point for a callback-based downstream call with a host-supplied user context.
    /// </summary>
    /// <param name="validationResult">Borrowed host authentication context containing the subject token.</param>
    /// <param name="downstreamApiInput">Borrowed per-request HTTP input and acquisition overrides.</param>
    /// <param name="downstreamApiName">Borrowed zero-terminated UTF-8 service name.</param>
    /// <param name="callback">Required completion callback receiving ownership of the result; may run inline.</param>
    /// <param name="callerContext">Opaque host context passed unchanged to the callback.</param>
    [UnmanagedCallersOnly(EntryPoint = nameof(EntraAuthCallDownstreamApiAsync))]
    public static void EntraAuthCallDownstreamApiAsync(
        NativeValidationResult* validationResult, NativeDownstreamApiInput* downstreamApiInput, byte* downstreamApiName,
        delegate* unmanaged<NativeDownstreamApiResult*, void*, void> callback, void* callerContext)
    {
        Complete(callback, AllocateDownstreamResult(), callerContext);
    }

    /// <summary>
    /// Entry point for a callback-based downstream call without a user context.
    /// </summary>
    /// <param name="downstreamApiInput">Borrowed per-request HTTP input and acquisition overrides.</param>
    /// <param name="downstreamApiName">Borrowed zero-terminated UTF-8 service name.</param>
    /// <param name="callback">Required completion callback receiving ownership of the result; may run inline.</param>
    /// <param name="callerContext">Opaque host context passed unchanged to the callback.</param>
    [UnmanagedCallersOnly(EntryPoint = nameof(EntraAuthCallDownstreamApiUnauthenticatedAsync))]
    public static void EntraAuthCallDownstreamApiUnauthenticatedAsync(
        NativeDownstreamApiInput* downstreamApiInput, byte* downstreamApiName,
        delegate* unmanaged<NativeDownstreamApiResult*, void*, void> callback, void* callerContext)
    {
        Complete(callback, AllocateDownstreamResult(), callerContext);
    }

    private static NativeDownstreamAuthorizationHeaderResult* AllocateHeaderResult()
    {
        NativeDownstreamAuthorizationHeaderResult* result = AllocateResult<NativeDownstreamAuthorizationHeaderResult>("Not implemented."u8);
        result->HttpResponseStatusCode = 501;
        result->ErrorDescription = (byte*)(result + 1);
        return result;
    }

    private static NativeDownstreamApiResult* AllocateDownstreamResult()
    {
        NativeDownstreamApiResult* result = AllocateResult<NativeDownstreamApiResult>("Not implemented."u8);
        result->HttpResponseStatusCode = 501;
        result->ErrorDescription = (byte*)(result + 1);
        return result;
    }

    private static void Complete<T>(delegate* unmanaged<T*, void*, void> callback, T* result, void* callerContext)
        where T : unmanaged
    {
        if (callback is null)
        {
            FreeResult(result);
            Environment.FailFast("A Native AOT completion callback is required.");
            return;
        }

        try
        {
            callback(result, callerContext);
        }
        catch (Exception exception)
        {
            Environment.FailFast("Native AOT completion callback failed.", exception);
        }
    }

    private static T* AllocateResult<T>(ReadOnlySpan<byte> errorDescription) where T : unmanaged
    {
        try
        {
            int size = sizeof(T) + errorDescription.Length + 1;
            var result = (T*)Marshal.AllocCoTaskMem(size);
            new Span<byte>(result, size).Clear();
            errorDescription.CopyTo(new Span<byte>(result + 1, errorDescription.Length));
            return result;
        }
        catch (Exception exception)
        {
            Environment.FailFast("Cannot allocate a Native AOT result.", exception);
            return null;
        }
    }
}

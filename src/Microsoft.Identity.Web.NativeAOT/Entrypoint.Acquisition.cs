// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Runtime.InteropServices;

namespace Microsoft.Identity.Web.NativeAOT;

internal static unsafe partial class Entrypoint
{
    // Inputs are deliberately not read until acquisition is implemented.
    [UnmanagedCallersOnly(EntryPoint = nameof(EntraAuthGetAuthorizationHeader))]
    private static NativeAuthorizationHeaderResult* EntraAuthGetAuthorizationHeader(
        NativeRequestContext* context, NativeAuthorizationHeaderInput* input, byte* downstreamApiName)
    {
        return AllocateResult<NativeAuthorizationHeaderResult>();
    }

    [UnmanagedCallersOnly(EntryPoint = nameof(EntraAuthGetAuthorizationHeaderUnauthenticated))]
    private static NativeAuthorizationHeaderResult* EntraAuthGetAuthorizationHeaderUnauthenticated(
        NativeAuthorizationHeaderInput* input, byte* downstreamApiName)
    {
        return AllocateResult<NativeAuthorizationHeaderResult>();
    }

    [UnmanagedCallersOnly(EntryPoint = nameof(EntraAuthCallDownstreamApi))]
    private static NativeDownstreamApiResult* EntraAuthCallDownstreamApi(
        NativeRequestContext* context, NativeDownstreamApiInput* input, byte* downstreamApiName)
    {
        return AllocateResult<NativeDownstreamApiResult>();
    }

    [UnmanagedCallersOnly(EntryPoint = nameof(EntraAuthCallDownstreamApiUnauthenticated))]
    private static NativeDownstreamApiResult* EntraAuthCallDownstreamApiUnauthenticated(
        NativeDownstreamApiInput* input, byte* downstreamApiName)
    {
        return AllocateResult<NativeDownstreamApiResult>();
    }

    [UnmanagedCallersOnly(EntryPoint = nameof(EntraAuthGetAuthorizationHeaderAsync))]
    private static NativeAsyncSubmissionResult* EntraAuthGetAuthorizationHeaderAsync(
        NativeRequestContext* context, NativeAuthorizationHeaderInput* input, byte* downstreamApiName,
        delegate* unmanaged<NativeAuthorizationHeaderResult*, ulong, void*, void> callback, void* callerContext)
    {
        return AllocateResult<NativeAsyncSubmissionResult>();
    }

    [UnmanagedCallersOnly(EntryPoint = nameof(EntraAuthGetAuthorizationHeaderUnauthenticatedAsync))]
    private static NativeAsyncSubmissionResult* EntraAuthGetAuthorizationHeaderUnauthenticatedAsync(
        NativeAuthorizationHeaderInput* input, byte* downstreamApiName,
        delegate* unmanaged<NativeAuthorizationHeaderResult*, ulong, void*, void> callback, void* callerContext)
    {
        return AllocateResult<NativeAsyncSubmissionResult>();
    }

    [UnmanagedCallersOnly(EntryPoint = nameof(EntraAuthCallDownstreamApiAsync))]
    private static NativeAsyncSubmissionResult* EntraAuthCallDownstreamApiAsync(
        NativeRequestContext* context, NativeDownstreamApiInput* input, byte* downstreamApiName,
        delegate* unmanaged<NativeDownstreamApiResult*, ulong, void*, void> callback, void* callerContext)
    {
        return AllocateResult<NativeAsyncSubmissionResult>();
    }

    [UnmanagedCallersOnly(EntryPoint = nameof(EntraAuthCallDownstreamApiUnauthenticatedAsync))]
    private static NativeAsyncSubmissionResult* EntraAuthCallDownstreamApiUnauthenticatedAsync(
        NativeDownstreamApiInput* input, byte* downstreamApiName,
        delegate* unmanaged<NativeDownstreamApiResult*, ulong, void*, void> callback, void* callerContext)
    {
        return AllocateResult<NativeAsyncSubmissionResult>();
    }

    private static T* AllocateResult<T>() where T : unmanaged
    {
        try
        {
            var result = (T*)Marshal.AllocCoTaskMem(sizeof(T));
            new Span<byte>(result, sizeof(T)).Clear();
            *(NativeErrorCategory*)result = NativeErrorCategory.Unsupported;
            return result;
        }
        catch (Exception exception)
        {
            Environment.FailFast("Cannot allocate a Native AOT result.", exception);
            return null;
        }
    }
}

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Runtime.InteropServices;

namespace Microsoft.Identity.Web.NativeAOT;

/// <summary>
/// Owned async submission receipt, independent of the eventual callback result.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public unsafe struct NativeAsyncSubmissionResult
{
    /// <summary>
    /// Submission outcome, not the eventual operation outcome.
    /// </summary>
    public NativeErrorCategory ErrorCategory;

    /// <summary>
    /// One when accepted with an operation ID; zero when rejected without a callback.
    /// </summary>
    public uint HasOperationId;

    /// <summary>
    /// Nonzero process-local opaque ID for an accepted operation; invalid after callback return.
    /// </summary>
    public ulong OperationId;

    /// <summary>
    /// Correlation GUID stored in the receipt allocation.
    /// </summary>
    public byte* CorrelationId;

    /// <summary>
    /// Optional redacted rejection explanation stored in the receipt allocation.
    /// </summary>
    public byte* ErrorDescription;
}

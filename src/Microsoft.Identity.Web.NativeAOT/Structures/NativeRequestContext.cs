// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Runtime.InteropServices;

namespace Microsoft.Identity.Web.NativeAOT;

/// <summary>
/// Borrowed user exchange context; the host remains responsible for inbound authentication and authorization.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public unsafe struct NativeRequestContext
{
    /// <summary>
    /// Required NULL-terminated UTF-8 compact JWT, parsed but not authenticated by the facade.
    /// </summary>
    public byte* UserAssertion;

    /// <summary>
    /// Optional preceding host correlation GUID; used only when caller correlation is omitted or blank.
    /// </summary>
    public byte* CorrelationId;
}

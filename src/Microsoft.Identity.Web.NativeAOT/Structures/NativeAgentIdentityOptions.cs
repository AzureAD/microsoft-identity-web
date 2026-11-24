// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Runtime.InteropServices;

namespace Microsoft.Identity.Web.NativeAOT;

/// <summary>
/// Borrowed UTF-8 agent application selector with at most one optional user selector.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public unsafe struct NativeAgentIdentityOptions
{
    /// <summary>
    /// Required nonempty agent application GUID.
    /// </summary>
    public byte* ClientId;

    /// <summary>
    /// Optional agent user principal name, mutually exclusive with the user object ID.
    /// </summary>
    public byte* UserPrincipalName;

    /// <summary>
    /// Optional nonempty agent user GUID, mutually exclusive with the principal name.
    /// </summary>
    public byte* UserObjectId;
}

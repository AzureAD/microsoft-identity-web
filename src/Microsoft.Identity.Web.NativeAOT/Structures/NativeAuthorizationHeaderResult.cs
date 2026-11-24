// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Runtime.InteropServices;

namespace Microsoft.Identity.Web.NativeAOT;

/// <summary>
/// Owned header outcome; UTF-8 strings share the result allocation, but any certificate is separately owned.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public unsafe struct NativeAuthorizationHeaderResult
{
    /// <summary>
    /// Operation outcome and token acquisition metadata, not independently allocated.
    /// </summary>
    public NativeAcquireTokenResult Token;

    /// <summary>
    /// Authorization header on success; otherwise NULL. Contains a secret, never log.
    /// </summary>
    public byte* AuthorizationHeader;

    /// <summary>
    /// Optional caller-owned Windows PCCERT_CONTEXT; release separately with CertFreeCertificateContext.
    /// </summary>
    public IntPtr BindingCertificate;

    /// <summary>
    /// Effective OBO session key, including an automatically allocated key; never log.
    /// </summary>
    public byte* LongRunningWebApiSessionKey;

    /// <summary>
    /// Optional acquisition metadata JSON; may contain secrets, never log.
    /// </summary>
    public byte* MetadataJson;
}

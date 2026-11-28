// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Runtime.InteropServices;

namespace Microsoft.Identity.Web.NativeAOT;

/// <summary>
/// Owned header outcome; UTF-8 strings share the result allocation, but any certificate is separately owned.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public unsafe struct NativeDownstreamAuthorizationHeaderResult
{
    /// <summary>
    /// Operation HTTP status for the host response, not an observed token-endpoint status.
    /// </summary>
    public int HttpResponseStatusCode;

    /// <summary>
    /// Correlation GUID for the operation.
    /// </summary>
    public byte* CorrelationId;

    /// <summary>
    /// Optional managed exception type/message; may contain sensitive provider data.
    /// </summary>
    public byte* ErrorDescription;

    /// <summary>
    /// Optional UTF-8 WWW-Authenticate header for forwarding an acquisition claims challenge upstream.
    /// </summary>
    public byte* WWWAuthenticate;

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
    /// Optional acquisition metadata JSON; format is not contractual and may contain secrets, never log.
    /// </summary>
    public byte* Metadata;

    /// <summary>
    /// Optional UTF-8 failure diagnostics JSON; format is not contractual and may contain sensitive data.
    /// </summary>
    public byte* FailureDetails;
}

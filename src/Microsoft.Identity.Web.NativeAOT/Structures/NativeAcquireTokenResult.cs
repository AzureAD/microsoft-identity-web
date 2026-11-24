// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Runtime.InteropServices;

namespace Microsoft.Identity.Web.NativeAOT;

/// <summary>
/// Token metadata embedded in a header or HTTP result; never free its UTF-8 strings or this prefix separately.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public unsafe struct NativeAcquireTokenResult
{
    /// <summary>
    /// Complete operation outcome.
    /// </summary>
    public NativeErrorCategory ErrorCategory;

    /// <summary>
    /// One when authoritative token expiration is available; otherwise zero.
    /// </summary>
    public uint HasExpiresOn;

    /// <summary>
    /// UTC Unix-epoch expiration in milliseconds, valid only when its availability flag is one.
    /// </summary>
    public long ExpiresOnUnixTimeMilliseconds;

    /// <summary>
    /// One when a token-endpoint HTTP status was observed; otherwise zero.
    /// </summary>
    public uint HasTokenEndpointHttpStatus;

    /// <summary>
    /// Token-endpoint status, not downstream status; valid only when its availability flag is one.
    /// </summary>
    public int TokenEndpointHttpStatus;

    /// <summary>
    /// Acquired secret token, or NULL when unavailable; never log or parse to infer expiration.
    /// </summary>
    public byte* AccessToken;

    /// <summary>
    /// Acquired token type, or NULL when unavailable.
    /// </summary>
    public byte* TokenType;

    /// <summary>
    /// Correlation GUID for the operation.
    /// </summary>
    public byte* CorrelationId;

    /// <summary>
    /// Optional provider error code.
    /// </summary>
    public byte* ProviderError;

    /// <summary>
    /// Optional provider suberror code.
    /// </summary>
    public byte* ProviderSubError;

    /// <summary>
    /// Managed exception type/message or downstream error body; may contain sensitive provider data.
    /// </summary>
    public byte* ErrorDescription;

    /// <summary>
    /// Optional provider claims challenge for host handling.
    /// </summary>
    public byte* ClaimsChallenge;
}

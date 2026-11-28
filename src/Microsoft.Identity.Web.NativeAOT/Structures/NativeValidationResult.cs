// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Runtime.InteropServices;

namespace Microsoft.Identity.Web.NativeAOT;

/// <summary>
/// Borrowed host-supplied validation context; the host remains responsible for inbound authentication and authorization.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public unsafe struct NativeValidationResult
{
    /// <summary>
    /// HTTP response status reported by the host's preceding authentication operation.
    /// </summary>
    public int HttpResponseStatusCode;

    /// <summary>
    /// Optional preceding host correlation GUID; used only when caller correlation is omitted or blank.
    /// </summary>
    public byte* CorrelationId;

    /// <summary>
    /// Optional zero-terminated UTF-8 description of the host's authentication error.
    /// </summary>
    public byte* ErrorDescription;

    /// <summary>
    /// Optional zero-terminated UTF-8 WWW-Authenticate header from the host's authentication operation.
    /// </summary>
    public byte* WWWAuthenticate;

    /// <summary>
    /// Required zero-terminated UTF-8 compact JWT, parsed but not authenticated by the facade.
    /// </summary>
    public byte* SubjectToken;

    /// <summary>
    /// Optional zero-terminated UTF-8 subject token type supplied by the host.
    /// </summary>
    public byte* SubjectTokenType;

    /// <summary>
    /// Reserved; must be null.
    /// </summary>
    public byte* Reserved1;

    /// <summary>
    /// Reserved; must be null.
    /// </summary>
    public byte* Reserved2;

    /// <summary>
    /// Reserved; must be null.
    /// </summary>
    public byte* Reserved3;

    /// <summary>
    /// Reserved opaque pointer; must be null.
    /// </summary>
    public void* Reserved4;

    /// <summary>
    /// Reserved; must be zero.
    /// </summary>
    public uint Reserved5;

    /// <summary>
    /// Reserved opaque pointer; must be null.
    /// </summary>
    public void* Reserved6;

    /// <summary>
    /// Reserved; must be zero.
    /// </summary>
    public uint Reserved7;

    /// <summary>
    /// Reserved; must be null.
    /// </summary>
    public byte* Reserved8;

    /// <summary>
    /// Reserved opaque pointer; must be null.
    /// </summary>
    public void* Reserved9;

    /// <summary>
    /// Reserved opaque pointer; must be null.
    /// </summary>
    public void* Reserved10;

    /// <summary>
    /// Reserved; must be zero.
    /// </summary>
    public uint Reserved11;

    /// <summary>
    /// Reserved opaque pointer; must be null.
    /// </summary>
    public void* Reserved12;

    /// <summary>
    /// Reserved opaque pointer; must be null.
    /// </summary>
    public void* Reserved13;

    /// <summary>
    /// Reserved; must be zero.
    /// </summary>
    public uint Reserved14;

    /// <summary>
    /// Reserved; must be null.
    /// </summary>
    public byte** Reserved15;

    /// <summary>
    /// Reserved; must be zero.
    /// </summary>
    public uint Reserved16;
}

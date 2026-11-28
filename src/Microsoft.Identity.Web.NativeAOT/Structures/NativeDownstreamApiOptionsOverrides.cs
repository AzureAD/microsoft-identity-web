// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Runtime.InteropServices;

namespace Microsoft.Identity.Web.NativeAOT;

/// <summary>
/// Shared token and acquisition overrides embedded by value in header and HTTP inputs.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public unsafe struct NativeDownstreamApiOptionsOverrides
{
    /// <summary>
    /// Scope string pointers; NULL with a zero count inherits named service scopes.
    /// </summary>
    public byte** Scopes;

    /// <summary>
    /// Number of scopes, limited to 64; application acquisition requires one resource/.default scope.
    /// </summary>
    public int ScopesCount;

    /// <summary>
    /// Optional tenant GUID or DNS-name override.
    /// </summary>
    public byte* Tenant;

    /// <summary>
    /// Optional URL-encoded JSON-object claims request, limited to 1 MiB before decoding.
    /// </summary>
    public byte* ClaimsJson;

    /// <summary>
    /// Optional confidential-client identity override; requires a certificate or signed client assertion.
    /// </summary>
    public byte* ManagedIdentityClientId;

    /// <summary>
    /// Optional base64-encoded certificate with a private key, limited to 1 MiB; never log.
    /// </summary>
    public byte* ManagedIdentityBase64encodedCertificate;

    /// <summary>
    /// Optional region for the overridden confidential client.
    /// </summary>
    public byte* AzureRegion;

    /// <summary>
    /// Optional host-signed client assertion JWT, limited to 1 MiB; never log.
    /// </summary>
    public byte* SignedClientAssertion;

    /// <summary>
    /// Optional FMI path for the acquired application's token.
    /// </summary>
    public byte* FmiPath;

    /// <summary>
    /// False permits cached tokens; true forces refresh. Overrides the service default.
    /// </summary>
    public bool ForceRefresh { get; set; }

    /// <summary>
    /// Reserved; must be null.
    /// </summary>
    public byte* Reserved1;

    /// <summary>
    /// Reserved; must be null.
    /// </summary>
    public byte* Reserved2;

    /// <summary>
    /// Optional HTTP path override constrained to the configured HTTPS origin.
    /// </summary>
    public byte* RelativePath;

    /// <summary>
    /// Reserved; must be null.
    /// </summary>
    public byte** Reserved3;

    /// <summary>
    /// Reserved; must be zero.
    /// </summary>
    public int Reserved4;

    /// <summary>
    /// Reserved; must be null.
    /// </summary>
    public byte** Reserved5;

    /// <summary>
    /// Reserved; must be zero.
    /// </summary>
    public int Reserved6;

    /// <summary>
    /// Reserved; must be null.
    /// </summary>
    public byte* Reserved7;

    /// <summary>
    /// Optional long-running OBO session key; never log.
    /// </summary>
    public byte* LongRunningWebApiSessionKey;

    /// <summary>
    /// Optional JSON claims forwarded to IdWeb for the client assertion, limited to 1 MiB.
    /// </summary>
    public byte* ClientAssertionClaims;

    /// <summary>
    /// Reserved; must be null.
    /// </summary>
    public byte* Reserved8;
}

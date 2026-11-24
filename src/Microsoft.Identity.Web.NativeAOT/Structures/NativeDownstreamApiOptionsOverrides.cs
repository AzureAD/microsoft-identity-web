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
    public uint ScopesCount;

    /// <summary>
    /// Whether to force refresh instead of permitting cached tokens; overrides the service default.
    /// </summary>
    public bool ForceRefresh;

    /// <summary>
    /// Optional tenant GUID or DNS-name override.
    /// </summary>
    public byte* Tenant;

    /// <summary>
    /// Optional URL-encoded JSON-object claims request, limited to 1 MiB before decoding.
    /// </summary>
    public byte* Claims;

    /// <summary>
    /// Shared operation deadline: zero selects 60000 ms; otherwise one through 600000 ms.
    /// </summary>
    public uint TimeoutMilliseconds;

    /// <summary>
    /// Optional host-signed client assertion JWT, limited to 1 MiB; never log.
    /// </summary>
    public byte* ClientAssertion;

    /// <summary>
    /// Optional protocol override; NULL inherits the service or default Bearer scheme.
    /// </summary>
    public byte* ProtocolScheme;

    /// <summary>
    /// Optional configured application name used for acquisition.
    /// </summary>
    public byte* AuthenticationOptionsName;

    /// <summary>
    /// Optional B2C user flow.
    /// </summary>
    public byte* UserFlow;

    /// <summary>
    /// Optional cached user account identifier for this request.
    /// </summary>
    public byte* HomeAccountId;

    /// <summary>
    /// Optional username, supplied together with a password.
    /// </summary>
    public byte* Username;

    /// <summary>
    /// Optional password, supplied together with a username; never log.
    /// </summary>
    public byte* Password;

    /// <summary>
    /// Additional token-endpoint URL query parameters.
    /// </summary>
    public NativeStringParameters ExtraQueryParameters;

    /// <summary>
    /// Additional token-endpoint HTTP headers.
    /// </summary>
    public NativeStringParameters ExtraHeadersParameters;

    /// <summary>
    /// Additional token-endpoint request body parameters.
    /// </summary>
    public NativeStringParameters ExtraBodyParameters;

    /// <summary>
    /// Optional long-running OBO session key; never log.
    /// </summary>
    public byte* LongRunningWebApiSessionKey;

    /// <summary>
    /// Optional managed identity selector.
    /// </summary>
    public NativeManagedIdentityOptions* ManagedIdentity;

    /// <summary>
    /// Optional external proof-of-possession key identifier.
    /// </summary>
    public byte* PopPublicKey;

    /// <summary>
    /// Optional JSON confirmation claim for externally bound proof-of-possession acquisition.
    /// </summary>
    public byte* PopClaim;

    /// <summary>
    /// Optional SDK-managed proof-of-possession signing settings.
    /// </summary>
    public NativePoPOptions* PoPConfiguration;

    /// <summary>
    /// Optional FMI path for the acquired application's token.
    /// </summary>
    public byte* FmiPath;

    /// <summary>
    /// Optional FMI path used when acquiring the client assertion.
    /// </summary>
    public byte* ClientAssertionFmiPath;

    /// <summary>
    /// Optional agent application or agent user selector.
    /// </summary>
    public NativeAgentIdentityOptions* AgentIdentity;

    /// <summary>
    /// Optional request-local downstream HTTP overrides.
    /// </summary>
    public NativeDownstreamRequestOptions* DownstreamApiOptions;

    /// <summary>
    /// Optional JSON claims forwarded to IdWeb for the client assertion, limited to 1 MiB.
    /// </summary>
    public byte* ClientAssertionClaims;

    /// <summary>
    /// Optional confidential-client identity override; requires a certificate or signed client assertion.
    /// </summary>
    public byte* ClientId;

    /// <summary>
    /// Optional base64-encoded certificate with a private key, limited to 1 MiB; never log.
    /// </summary>
    public byte* ClientCertificate;

    /// <summary>
    /// Optional region for the overridden confidential client.
    /// </summary>
    public byte* AzureRegion;
}

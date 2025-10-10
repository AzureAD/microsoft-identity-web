// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Identity.Web.Analyzers;

/// <summary>
/// Diagnostic IDs for Microsoft.Identity.Web v4 migration analyzers.
/// </summary>
public static class DiagnosticIds
{
    /// <summary>
    /// IDW4001: TokenAcquirerTokenCredential is obsolete.
    /// </summary>
    public const string TokenAcquirerTokenCredentialObsolete = "IDW4001";

    /// <summary>
    /// IDW4002: TokenAcquirerAppTokenCredential is obsolete.
    /// </summary>
    public const string TokenAcquirerAppTokenCredentialObsolete = "IDW4002";

    /// <summary>
    /// IDW4003: AddDownstreamWebApi is obsolete.
    /// </summary>
    public const string AddDownstreamWebApiObsolete = "IDW4003";

    /// <summary>
    /// IDW4004: IDownstreamWebApi is obsolete.
    /// </summary>
    public const string IDownstreamWebApiObsolete = "IDW4004";
}

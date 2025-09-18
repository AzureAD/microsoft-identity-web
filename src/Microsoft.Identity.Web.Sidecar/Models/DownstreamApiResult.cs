// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Identity.Web.Sidecar.Models;

/// <summary>
/// The result of requesting an authorization header.
/// </summary>
/// <param name="AuthorizationHeader">The authorization header.</param>
public record DownstreamApiResult(
    int StatusCode,
    Dictionary<string, IEnumerable<string>> Headers,
    string? Content);

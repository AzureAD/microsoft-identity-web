// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Identity.Web.Sidecar.Models;

/// <summary>
/// The result of calling a downstream api.
/// </summary>
/// <param name="StatusCode">The status code of the response.</param>
/// <param name="Headers">The headers of the response.</param>
/// <param name="Content">Optional. The content of the response.</param>
public record DownstreamApiResult(
    int StatusCode,
    Dictionary<string, IEnumerable<string>> Headers,
    string? Content);

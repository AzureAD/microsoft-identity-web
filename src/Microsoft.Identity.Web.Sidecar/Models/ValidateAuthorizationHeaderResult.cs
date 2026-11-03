// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json.Nodes;

namespace Microsoft.Identity.Web.Sidecar.Models;

/// <summary>
/// The result of validating an authorization header.
/// </summary>
/// <param name="Protocol">The authentication protocol the credential was validated with: <c>Bearer</c> or <c>PoP</c>.</param>
/// <param name="Token">The token validated.</param>
/// <param name="Claims">The claims parsed from the token.</param>
public record ValidateAuthorizationHeaderResult(string Protocol, string Token, JsonNode Claims);

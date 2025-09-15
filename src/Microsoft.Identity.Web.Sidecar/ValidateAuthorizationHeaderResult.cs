// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json.Nodes;

internal record ValidateAuthorizationHeaderResult(string Protocol, string Token, JsonNode Claims);

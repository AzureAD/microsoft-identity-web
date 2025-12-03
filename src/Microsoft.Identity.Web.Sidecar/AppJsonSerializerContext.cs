// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json.Serialization;
using Microsoft.Identity.Web.Sidecar.Models;

namespace Microsoft.Identity.Web.Sidecar;

[JsonSerializable(typeof(AuthorizationHeaderResult))]
[JsonSerializable(typeof(DownstreamApiResult))]
[JsonSerializable(typeof(ValidateAuthorizationHeaderResult))]
internal partial class AppJsonSerializerContext : JsonSerializerContext
{

}

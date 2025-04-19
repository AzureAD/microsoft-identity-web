// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Identity.Web.Util;

namespace Microsoft.Identity.Web
{
    internal class ClientInfo
    {
        [JsonPropertyName(ClaimConstants.UniqueObjectIdentifier)]
        public string? UniqueObjectIdentifier { get; set; } = null;

        [JsonPropertyName(ClaimConstants.UniqueTenantIdentifier)]
        public string? UniqueTenantIdentifier { get; set; } = null;

        public static ClientInfo? CreateFromJson(string? clientInfo)
        {
            if (string.IsNullOrEmpty(clientInfo))
            {
                throw new ArgumentNullException(nameof(clientInfo), IDWebErrorMessage.ClientInfoReturnedFromServerIsNull);
            }

            var bytes = Base64UrlHelpers.DecodeBytes(clientInfo);
            return bytes != null ? DeserializeFromJson(bytes) : null;
        }

        internal static ClientInfo? DeserializeFromJson(byte[]? jsonByteArray)
        {
            if (jsonByteArray is null || jsonByteArray.Length == 0)
            {
                return default;
            }

            return JsonSerializer.Deserialize<ClientInfo>(jsonByteArray, ClientInfoJsonSerializerContext.Default.ClientInfo);
        }
    }

    [JsonSerializable(typeof(ClientInfo))]
    [JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true)]
    partial class ClientInfoJsonSerializerContext : JsonSerializerContext
    {
    }
}

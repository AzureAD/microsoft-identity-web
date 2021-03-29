// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microsoft.Identity.Web
{
    internal class ClientInfo
    {
        [JsonPropertyName(ClaimConstants.UniqueObjectIdentifier)]
        public string? UniqueObjectIdentifier { get; set; } = null;

        [JsonPropertyName(ClaimConstants.UniqueTenantIdentifier)]
        public string? UniqueTenantIdentifier { get; set; } = null;

        public static ClientInfo? CreateFromJson(string clientInfo)
        {
            if (string.IsNullOrEmpty(clientInfo))
            {
                throw new ArgumentNullException(nameof(clientInfo), IDWebErrorMessage.ClientInfoReturnedFromServerIsNull);
            }

            return DeserializeFromJson(Base64UrlHelpers.DecodeToBytes(clientInfo));
        }

        internal static ClientInfo? DeserializeFromJson(byte[] jsonByteArray)
        {
            if (jsonByteArray == null || jsonByteArray.Length == 0)
            {
                return default;
            }

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
            };

            return JsonSerializer.Deserialize<ClientInfo>(jsonByteArray, options);
        }
    }
}

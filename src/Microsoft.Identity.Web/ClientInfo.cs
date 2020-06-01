// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Runtime.Serialization;
using System.Text;
using Newtonsoft.Json;

namespace Microsoft.Identity.Web
{
    [DataContract]
    internal class ClientInfo
    {
        [DataMember(Name = "uid", IsRequired = false)]
        public string? UniqueObjectIdentifier { get; set; }

        [DataMember(Name = "utid", IsRequired = false)]
        public string? UniqueTenantIdentifier { get; set; }

        public static ClientInfo CreateFromJson(string clientInfo)
        {
            if (string.IsNullOrEmpty(clientInfo))
            {
                throw new ArgumentNullException(nameof(clientInfo), $"client info returned from the server is null");
            }

            var jsonByteArray = Base64UrlHelpers.DecodeToBytes(clientInfo);

            using MemoryStream stream = new MemoryStream(jsonByteArray);
            using StreamReader reader = new StreamReader(stream, Encoding.UTF8);
            return (ClientInfo)JsonSerializer.Create().Deserialize(reader, typeof(ClientInfo));
        }
    }
}

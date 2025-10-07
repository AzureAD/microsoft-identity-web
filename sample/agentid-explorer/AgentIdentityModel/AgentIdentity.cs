// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace AgentIdentityModel
{
    /// <summary>
    /// 
    /// </summary>
	public class AgentIdentity
	{
        /// <summary>
        /// 
        /// </summary>
		[JsonPropertyName("@odata.type")]
		public string @odata_type { get; set; } = "#Microsoft.Graph.AgentIdentity";

        /// <summary>
        /// 
        /// </summary>
		[JsonPropertyName("displayName")]
		public string? displayName { get; set; }

        /// <summary>
        /// 
        /// </summary>
		[JsonPropertyName("agentAppId")]
		public string? agentAppId { get; set; }

        /// <summary>
        /// 
        /// </summary>
		[JsonPropertyName("id")]
		public string? id { get; set; }

        /// <summary>
        /// Owners
        /// </summary>
        [JsonPropertyName("owners@odata.bind")]
        public string[] Owners { get; set; } = new string[0];

    }
}

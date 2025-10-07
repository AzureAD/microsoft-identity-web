// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace AgentIdentityModel
{
	/// <summary>
    /// Agent identity model used for CRUD operations
    /// </summary>
	public class AgentIdUser
	{
        /// <summary>
        /// 
        /// </summary>
		[JsonPropertyName("@odata.type")]
		public string @odata_type { get; set; } = "Microsoft.Graph.AgentUser";

        /// <summary>
        /// 
        /// </summary>
		[JsonPropertyName("displayName")]
		public string? displayName { get; set; }

        /// <summary>
        /// /
        /// </summary>
		[JsonPropertyName("userPrincipalName")]
		public string? userPrincipalName { get; set; }

        /// <summary>
        /// 
        /// </summary>
		[JsonPropertyName("mailNickname")]
		public string? mailNickname { get; set; }

        /// <summary>
        /// 
        /// </summary>
		[JsonPropertyName("accountEnabled")]
		public bool? accountEnabled { get; set; }

        /// <summary>
        /// 
        /// </summary>
		[JsonPropertyName("identityParentId")]
		public string? identityParentId { get; set; }

        /// <summary>
        /// 
        /// </summary>
		[JsonPropertyName("id")]
		public string? id { get; set; }
	}

}

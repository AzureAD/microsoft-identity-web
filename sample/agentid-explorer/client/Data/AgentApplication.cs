// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace client.Data
{
    /// <summary>
    /// Represents a request to create an agent application.
    /// </summary>
    public class AgentApplicationRequest
    {
        /// <summary>
        /// Gets or sets the OData type identifier for the agent identity blueprint.
        /// </summary>
        [JsonPropertyName("@odata.type")]
        public string @odata_type { get; set; } = "#Microsoft.Graph.AgentIdentityBlueprint";

        /// <summary>
        /// Gets or sets the display name of the agent application.
        /// </summary>
        [JsonPropertyName("displayName")]
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>
        /// Owners property to bind the application to the current user creating it.
        /// Needs to be of the shape [$"https://graph.microsoft.com/v1.0/users/{objectId}]"
        /// </summary>
        [JsonPropertyName("owners@odata.bind")]
        public string[] Owners { get; set; } = new string[0];

    }

    /// <summary>
    /// Represents an agent application with extended properties including identity and creation information.
    /// </summary>
    public class AgentApplication : AgentApplicationRequest
    {
        /// <summary>
        /// Gets or sets the unique identifier of the agent application.
        /// </summary>
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the application (client) ID of the agent application.
        /// </summary>
        [JsonPropertyName("appid")]
        public string AppId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the date and time when the agent application was created.
        /// </summary>
        [JsonPropertyName("createdDateTime")]
        public DateTimeOffset CreatedDateTime { get; set; }

        /// <summary>
        /// Gets or sets the application ID that created this agent application, if applicable.
        /// </summary>
        [JsonPropertyName("createdByApp")]
        public string? CreatedByApp { get; set; }
    }

    /// <summary>
    /// Represents a request to create or reference a service principal.
    /// </summary>
    public class ServicePrincipalRequest
    {
        /// <summary>
        /// Gets or sets the application (client) ID associated with the service principal.
        /// </summary>
        [JsonPropertyName("appid")]
        public string AppId { get; set; } = string.Empty;
    }
}

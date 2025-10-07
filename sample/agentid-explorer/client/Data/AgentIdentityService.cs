// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using AgentIdentityModel;
using Microsoft.Identity.Abstractions;

namespace client.Data
{
	/// <summary>
	/// Manages agent identities using the Microsoft Graph Beta API and the Agent application web API
	/// </summary>
	public class AgentIdentityService
	{
		private readonly IDownstreamApi _downstreamWebApi;
		private readonly ILogger<AgentIdentityService> _logger;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="downstreamWebApi"></param>
        /// <param name="logger"></param>
		public AgentIdentityService(IDownstreamApi downstreamWebApi, ILogger<AgentIdentityService> logger)
		{
			_downstreamWebApi = downstreamWebApi;
			_logger = logger;
		}

		/// <summary>
		/// List agent identities with the current user context. This is a call to the Microsoft Graph Beta API
		/// The Graph SDK does not support the Agent identities yet.
		/// </summary>
		/// <returns></returns>
		public async Task<List<AgentIdentity>> GetAgentIdentitiesAsync()
		{
			try
			{
				var result = await _downstreamWebApi.GetForUserAsync<Response<AgentIdentity>>("", options =>
				{
					options.BaseUrl = "https://graph.microsoft.com/beta/";
					options.RelativePath = "/serviceprincipals/Microsoft.Graph.AgentIdentity";
					options.Scopes = new[] { "https://graph.microsoft.com/.default" };
				});

				return result!.Value.ToList(); // Change result.Value to result.ToList() to match return type
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error retrieving agent identities");
				throw;
			}
		}

		/// <summary>
		/// List agent identities with the current user context. This is a call to the Microsoft Graph Beta API
		/// The Graph SDK does not support the Agent identities yet.
		/// </summary>
		/// <returns></returns>
		public async Task<List<AgentIdentity>> GetAgentIdentitiesAsync(string agentApplicationClientId)
		{
			return (await GetAgentIdentitiesAsync()).Where(i => i.agentAppId == agentApplicationClientId).ToList();
		}

		/// <summary>
		/// Create a new agent identity with the current user context. This needs to happen in the agent application
		/// (only way to create an agent identity).
		/// </summary>
		/// <param name="displayName"></param>
		/// <returns></returns>
		public async Task<AgentIdentity> CreateAgentIdentityAsync(string displayName)
		{
			try
			{
				// Call the AgentIdentity endpoint to create a new agent identity
				// We need to pass the display name as a JSON string
				var response = await _downstreamWebApi.PostForUserAsync<string, AgentIdentity>(
					"AgentApp",
					displayName,
					options => options.RelativePath = "/api/AgentIdentity");

#pragma warning disable CA2201 // Do not raise reserved exception types
                return response ?? throw new Exception("Failed to create agent identity");
#pragma warning restore CA2201 // Do not raise reserved exception types
            }
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error creating agent identity");
				throw;
			}
		}

		/// <summary>
		/// Delete an agent identity by its ID. This needs to happen in the agent application
		/// </summary>
		/// <param name="id">Id of the agent identity to delete</param>
		/// <returns>Message indicating the result of the deletion</returns>
		/// <exception cref="ArgumentNullException"></exception>
		public async Task<string> DeleteAgentIdentityAsync(string id)
		{
			if (string.IsNullOrEmpty(id))
			{
				throw new ArgumentNullException(nameof(id), "Agent identity ID cannot be null or empty");
			}

			try
			{
				// Call the AgentIdentity endpoint to delete an agent identity
				var response = await _downstreamWebApi.DeleteForUserAsync<string?, string>(
					"AgentApp",
					null,
					options =>
					{
						options.RelativePath = $"/api/AgentIdentity/{id}";
					});

				return response ?? "Identity deleted successfully";
			}
			catch (Exception ex)
			{
				// Log specific error information
				_logger.LogError(ex, "Error deleting agent identity with ID {Id}", id);

				// Rethrow for UI to handle
				throw;
			}
		}
	}
}

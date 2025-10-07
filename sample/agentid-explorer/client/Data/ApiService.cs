// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Web;
using System.Text.Json;

namespace client.Data
{
    /// <summary>
    /// Service for calling downstream APIs using agent identities.
    /// Provides methods to call APIs on behalf of users or applications using agent identity authentication.
    /// </summary>
    public class ApiService
    {
        private readonly IDownstreamApi _downstreamWebApi;
        private readonly ILogger<ApiService> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="ApiService"/> class.
        /// </summary>
        /// <param name="downstreamWebApi">The downstream API service for making HTTP requests.</param>
        /// <param name="logger">The logger instance.</param>
        public ApiService(IDownstreamApi downstreamWebApi, ILogger<ApiService> logger)
        {
            _downstreamWebApi = downstreamWebApi;
            _logger = logger;
        }

        /// <summary>
        /// Calls the Microsoft Graph API on behalf of the user using an agent identity.
        /// </summary>
        /// <param name="agentIdentityId">The unique identifier of the agent identity to use for authentication.</param>
        /// <returns>An <see cref="ApiCallResponse"/> containing the result of the API call.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="agentIdentityId"/> is null or empty.</exception>
        public async Task<ApiCallResponse> CallApiOnBehalfOfUserAsync(string agentIdentityId)
        {
            if (string.IsNullOrEmpty(agentIdentityId))
            {
                throw new ArgumentNullException(nameof(agentIdentityId), "Agent identity ID cannot be null or empty");
            }

            try
            {
                // Call the API to get data using the agent identity
                var response = await _downstreamWebApi.GetForUserAsync<ApiCallResponse>(
                    "AgentApp",
                    options =>
                    {
                        options.RelativePath = $"/api/CallGraph/agent-obo-user?agentIdentityId={agentIdentityId}";
                    });
                
                return response ?? new ApiCallResponse { Success = false, Message = "No response received from API" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling API with agent identity {AgentIdentityId}", agentIdentityId);
                return new ApiCallResponse 
                { 
                    Success = false, 
                    Message = $"Error calling API: {ex.Message}",
                    Error = ex.ToString()
                };
            }
        }

        /// <summary>
        /// Calls a REST API on behalf of the user using an agent identity.
        /// </summary>
        /// <param name="agentIdentityId">The unique identifier of the agent identity to use for authentication.</param>
        /// <returns>An <see cref="ApiCallResponse"/> containing the result of the API call.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="agentIdentityId"/> is null or empty.</exception>
        public async Task<ApiCallResponse> CallApiRestOnBehalfOfUserAsync(string agentIdentityId)
		{
			if (string.IsNullOrEmpty(agentIdentityId))
			{
				throw new ArgumentNullException(nameof(agentIdentityId), "Agent identity ID cannot be null or empty");
			}

			try
			{
				// Call the API to get data using the agent identity
				var response = await _downstreamWebApi.GetForUserAsync<ApiCallResponse>(
					"AgentApp",
					options =>
					{
						options.RelativePath = $"/api/CallAnyApi/agent-obo-user?agentIdentityId={agentIdentityId}";
					});

				return response ?? new ApiCallResponse { Success = false, Message = "No response received from API" };
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error calling API with agent identity {AgentIdentityId}", agentIdentityId);
				return new ApiCallResponse
				{
					Success = false,
					Message = $"Error calling API: {ex.Message}",
					Error = ex.ToString()
				};
			}
		}

        /// <summary>
        /// Calls the Microsoft Graph API on behalf of the application using an agent identity.
        /// </summary>
        /// <param name="agentIdentityId">The unique identifier of the agent identity to use for authentication.</param>
        /// <returns>An <see cref="ApiCallResponse"/> containing the result of the API call.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="agentIdentityId"/> is null or empty.</exception>
        public async Task<ApiCallResponse> CallApiOnBehalfOfAppAsync(string agentIdentityId)
		{
			if (string.IsNullOrEmpty(agentIdentityId))
			{
				throw new ArgumentNullException(nameof(agentIdentityId), "Agent identity ID cannot be null or empty");
			}

			try
			{
				// Call the API to get data using the agent identity
				var response = await _downstreamWebApi.GetForUserAsync<ApiCallResponse>(
					"AgentApp",
					options =>
					{
						options.RelativePath = $"/api/CallGraph/agent-obo-app?agentIdentityId={agentIdentityId}";
					});

				return response ?? new ApiCallResponse { Success = false, Message = "No response received from API" };
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error calling API with agent identity {AgentIdentityId}", agentIdentityId);
				return new ApiCallResponse
				{
					Success = false,
					Message = $"Error calling API: {ex.Message}",
					Error = ex.ToString()
				};
			}
		}

        /// <summary>
        /// Calls Azure Resource Manager (ARM) API on behalf of the application using an agent identity.
        /// This enables autonomous agent operations against Azure resources.
        /// </summary>
        /// <param name="agentIdentityId">The unique identifier of the agent identity to use for authentication.</param>
        /// <returns>An <see cref="ApiCallResponse"/> containing the result of the ARM API call.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="agentIdentityId"/> is null or empty.</exception>
        public async Task<ApiCallResponse> CallArmOnBehalfOfAppAsync(string agentIdentityId)
		{
			if (string.IsNullOrEmpty(agentIdentityId))
			{
				throw new ArgumentNullException(nameof(agentIdentityId), "Agent identity ID cannot be null or empty");
			}

			try
			{
				// Call the API to get data using the agent identity
				var response = await _downstreamWebApi.GetForUserAsync<ApiCallResponse>(
					"AgentApp",
					options =>
					{
						options.RelativePath = $"/api/CallAzure/autonomous-agent-calls-arm?agentIdentityId={agentIdentityId}";
					});

				return response ?? new ApiCallResponse { Success = false, Message = "No response received from API" };
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error calling ARM API with agent identity {AgentIdentityId}", agentIdentityId);
				return new ApiCallResponse
				{
					Success = false,
					Message = $"Error calling API: {ex.Message}",
					Error = ex.ToString()
				};
			}
		}

        /// <summary>
        /// Calls an API using an agent user identity.
        /// Agent user identities represent specific users within the context of an agent application.
        /// </summary>
        /// <param name="agentIdentityId">The unique identifier of the agent identity.</param>
        /// <param name="userPrincipalName">The user principal name (UPN) of the user.</param>
        /// <returns>An <see cref="ApiCallResponse"/> containing the result of the API call.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="agentIdentityId"/> or <paramref name="userPrincipalName"/> is null or empty.</exception>
        public async Task<ApiCallResponse> CallApiWithAgentUserIdentityAsync(string agentIdentityId, string userPrincipalName)
        {
            if (string.IsNullOrEmpty(agentIdentityId))
            {
                throw new ArgumentNullException(nameof(agentIdentityId), "Agent identity ID cannot be null or empty");
            }

            if (string.IsNullOrEmpty(userPrincipalName))
            {
                throw new ArgumentNullException(nameof(userPrincipalName), "User Principal Name cannot be null or empty");
            }

            try
            {
                // Call the API to get data using the agent user identity
                var response = await _downstreamWebApi.GetForUserAsync<ApiCallResponse>(
                    "AgentApp",
                    options =>
                    {
                        options.RelativePath = $"/api/AgentUserIdentity?agentIdentityId={agentIdentityId}&upn={userPrincipalName}";
                    });
                
                return response ?? new ApiCallResponse { Success = false, Message = "No response received from API" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling API with agent user identity {AgentIdentityId}, {UserPrincipalName}", 
                    agentIdentityId, userPrincipalName);
                return new ApiCallResponse 
                { 
                    Success = false, 
                    Message = $"Error calling API: {ex.Message}",
                    Error = ex.ToString()
                };
            }
        }

        /// <summary>
        /// Creates a new agent user identity for a specific user within an agent application.
        /// This allows the agent to perform operations on behalf of a specific user.
        /// </summary>
        /// <param name="agentIdentityId">The unique identifier of the agent identity.</param>
        /// <param name="userPrincipalName">The user principal name (UPN) of the user for whom to create the agent user identity.</param>
        /// <returns>A <see cref="CreateAgentUserIdentityResponse"/> containing the details of the created agent user identity.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="agentIdentityId"/> or <paramref name="userPrincipalName"/> is null or empty.</exception>
        public async Task<CreateAgentUserIdentityResponse> CreateAgentUserIdentityAsync(string agentIdentityId, string userPrincipalName)
        {
            if (string.IsNullOrEmpty(agentIdentityId))
            {
                throw new ArgumentNullException(nameof(agentIdentityId), "Agent identity ID cannot be null or empty");
            }

            if (string.IsNullOrEmpty(userPrincipalName))
            {
                throw new ArgumentNullException(nameof(userPrincipalName), "User Principal Name cannot be null or empty");
            }

            try
            {
                // Call the API to create a new agent user identity
                var response = await _downstreamWebApi.PostForUserAsync<object, CreateAgentUserIdentityResponse>(
                    "AgentApp",
                    null!, // No request body needed since parameters are in query string
                    options =>
                    {
                        options.RelativePath = $"/api/AgentUserIdentity?agentIdentityId={agentIdentityId}&upn={userPrincipalName}";
                    });
                
                return response ?? new CreateAgentUserIdentityResponse 
                { 
                    Success = true, 
                    Message = "Agent user identity created but no response data received" 
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating agent user identity {AgentIdentityId}, {UserPrincipalName}", 
                    agentIdentityId, userPrincipalName);
                return new CreateAgentUserIdentityResponse 
                { 
                    Success = false, 
                    Message = $"Error creating agent user identity: {ex.Message}",
                    Error = ex.ToString()
                };
            }
        }
	}

    /// <summary>
    /// Represents the response from an API call made using an agent identity.
    /// </summary>
    public class ApiCallResponse
    {
        /// <summary>
        /// Gets or sets a value indicating whether the API call was successful.
        /// </summary>
        public bool Success { get; set; }
        
        /// <summary>
        /// Gets or sets a message describing the result of the API call.
        /// </summary>
        public string Message { get; set; } = string.Empty;
        
        /// <summary>
        /// Gets or sets the user principal name returned from the API.
        /// </summary>
        public string UserPrincipalName { get; set; } = string.Empty;
        
        /// <summary>
        /// Gets or sets the display name returned from the API.
        /// </summary>
        public string DisplayName { get; set; } = string.Empty;
        
        /// <summary>
        /// Gets or sets the job title returned from the API.
        /// </summary>
        public string JobTitle { get; set; } = string.Empty;
        
        /// <summary>
        /// Gets or sets error details if the API call failed.
        /// </summary>
        public string Error { get; set; } = string.Empty;
        
        /// <summary>
        /// Gets or sets the profile photo data in base64 format.
        /// </summary>
        public string ProfilePhoto { get; set; } = string.Empty;
        
        /// <summary>
        /// Gets or sets a value indicating whether a profile photo is available.
        /// </summary>
        public bool HasPhoto { get; set; }
        
        /// <summary>
        /// Gets or sets additional data returned from the API call.
        /// </summary>
        public Dictionary<string, object> AdditionalData { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Represents the response from creating an agent user identity.
    /// </summary>
    public class CreateAgentUserIdentityResponse
    {
        /// <summary>
        /// Gets or sets a value indicating whether the agent user identity was created successfully.
        /// </summary>
        public bool Success { get; set; } = true;
        
        /// <summary>
        /// Gets or sets a message describing the result of the operation.
        /// </summary>
        public string Message { get; set; } = string.Empty;
        
        /// <summary>
        /// Gets or sets error details if the operation failed.
        /// </summary>
        public string Error { get; set; } = string.Empty;
        
        /// <summary>
        /// Gets or sets the display name of the created agent user identity.
        /// </summary>
        public string? DisplayName { get; set; }
        
        /// <summary>
        /// Gets or sets the mail nickname of the created agent user identity.
        /// </summary>
        public string? MailNickname { get; set; }
        
        /// <summary>
        /// Gets or sets the user principal name of the created agent user identity.
        /// </summary>
        public string? UserPrincipalName { get; set; }
        
        /// <summary>
        /// Gets or sets a value indicating whether the account is enabled.
        /// </summary>
        public bool? AccountEnabled { get; set; }
        
        /// <summary>
        /// Gets or sets the parent identity ID for the agent user identity.
        /// </summary>
        public string? IdentityParentId { get; set; }
        
        /// <summary>
        /// Gets or sets the unique identifier of the created agent user identity.
        /// </summary>
        public string? Id { get; set; }
    }
}

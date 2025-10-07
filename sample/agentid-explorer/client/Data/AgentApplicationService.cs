// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using Microsoft.Graph.Beta;
using Microsoft.Graph.Beta.Models;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Web;
using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;

namespace client.Data
{
	/// <summary>
	/// Management of Agent applications
	/// </summary>
	[Authorize]
	[AuthorizeForScopes(Scopes = ["Applications.ReadWrite.All", "AgentIdentityBlueprint.Create", "AgentIdentityBlueprint.CreateAsManager"])]
	public class AgentApplicationService
	{
		private readonly GraphServiceClient _graphServiceClient;
		private readonly ILogger<AgentApplicationService> _logger;
		private readonly IAuthorizationHeaderProvider _authorizationHeaderProvider;
		private readonly AuthenticationStateProvider _authenticationStateProvider;

        /// <summary>
        /// Gets the downstream API service for making HTTP requests.
        /// </summary>
        public IDownstreamApi DownstreamApi { get; }
		
		/// <summary>
		/// Gets the Microsoft Identity configuration options for the agent ID explorer.
		/// </summary>
		public MicrosoftIdentityOptions AgentIdExplorerOptions { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="AgentApplicationService"/> class.
        /// </summary>
        /// <param name="graphServiceClient">The Microsoft Graph service client.</param>
        /// <param name="logger">The logger instance.</param>
        /// <param name="authorizationHeaderProvider">The authorization header provider.</param>
        /// <param name="downstreamApi">The downstream API service.</param>
        /// <param name="agentIdExplorerOptions">The Microsoft Identity options monitor.</param>
        /// <param name="authenticationStateProvider"></param>
        public AgentApplicationService(
			GraphServiceClient graphServiceClient,
			ILogger<AgentApplicationService> logger,
			IAuthorizationHeaderProvider authorizationHeaderProvider,
			IDownstreamApi downstreamApi,
			IOptionsMonitor<MicrosoftIdentityOptions> agentIdExplorerOptions,
			AuthenticationStateProvider authenticationStateProvider)
		{
			_graphServiceClient = graphServiceClient;
			_logger = logger;
			_authorizationHeaderProvider = authorizationHeaderProvider;
			DownstreamApi = downstreamApi;
            AgentIdExplorerOptions = agentIdExplorerOptions.Get(OpenIdConnectDefaults.AuthenticationScheme);
			_authenticationStateProvider = authenticationStateProvider;
		}

		/// <summary>
		/// List the Agent applications with the current user context. 
		/// This is a call to the Microsoft Graph Beta API but the Graph SDK does not support the Agent applications yet.
		/// </summary>
		/// <returns>A list of agent applications.</returns>
		/// <exception cref="Exception">Thrown when an error occurs retrieving agent applications.</exception>
		public async Task<List<AgentApplication>> GetAgentApplicationsAsync()
		{
			try
			{
				var result = await DownstreamApi.GetForUserAsync<Response<AgentApplication>>("", options =>
				{
					options.BaseUrl = "https://graph.microsoft.com/beta/";
					options.RelativePath = "/applications/Microsoft.Graph.AgentIdentityBlueprint";
					options.Scopes = new[] { "https://graph.microsoft.com/.default" };
				});

				return result!.Value.ToList(); // Change result.Value to result.ToList() to match return type
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error retrieving agent applications");
				throw;
			}
		}

		/// <summary>
		/// Creates a new agent application with the specified display name and optional managed identity principal ID.
		/// </summary>
		/// <param name="displayName">The display name for the agent application.</param>
		/// <param name="msiPrincipalId">The managed service identity principal ID. Optional.</param>
		/// <returns>The created agent application.</returns>
		/// <exception cref="Exception">Thrown when an error occurs creating the agent application.</exception>
		public async Task<AgentApplication> CreateAgentApplicationAsync(string displayName, string msiPrincipalId)
		{
			try
			{
                // Create the agent application (context of the signed-in user, who needs to have Application.ReadWrite.All
                // permissions in the tenant)
                string? ownerOid = await GetCurrentUserOidAsync();
                var agentApplication = await DownstreamApi.PostForUserAsync<AgentApplicationRequest, AgentApplication>("",
                    new AgentApplicationRequest { DisplayName = displayName,
                     Owners = new[] { $"https://graph.microsoft.com/v1.0/users/{ownerOid}" }
                    },
					options =>
					{
						options.BaseUrl = "https://graph.microsoft.com/";
						options.RelativePath = "/beta/applications/";
						options.Scopes = ["AgentIdentityBlueprint.Create"];
                        options.ExtraHeaderParameters = new Dictionary<string, string>() {
                            { "OData-Version", "4.0" }
                        };
					});

				// Create the service principal for the agent application
				var agentServicePrincipal = await DownstreamApi.PostForUserAsync<ServicePrincipalRequest, ServicePrincipal>("",
					new ServicePrincipalRequest { AppId = agentApplication!.AppId },
					options =>
					{
						options.BaseUrl = "https://graph.microsoft.com/";
						options.RelativePath = "beta/serviceprincipals/graph.agentIdentityBlueprintPrincipal";
						options.Scopes = ["Application.ReadWrite.All"];
					});


				// Add a scope (access_agent) to the agent application
				await ConfigureIdentifierUriAndScopeAsync(agentApplication.Id, agentApplication.AppId);

				// Add a federated identity credential to the agent application if provided
				if (!string.IsNullOrEmpty(msiPrincipalId))
				{
					await AddFederatedIdentityCredentialAsync(agentApplication.Id, AgentIdExplorerOptions.TenantId, msiPrincipalId);
				}

				return agentApplication;
			}

			catch (Exception ex)
			{
				_logger.LogError(ex, "Error creating agent application");

				throw;
			}
		}

		/// <summary>
		/// Deletes an agent application by its unique identifier.
		/// </summary>
		/// <param name="id">The unique identifier of the agent application to delete.</param>
		/// <returns>A task representing the asynchronous operation.</returns>
		/// <exception cref="Exception">Thrown when an error occurs deleting the agent application.</exception>
		public async Task DeleteAgentApplicationAsync(string id)
		{
			try
			{
				await _graphServiceClient.Applications[id]
					.DeleteAsync();
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error deleting agent application");
				throw;
			}
		}

		/// <summary>
		/// Configures the identifier URI and OAuth2 permission scope for an agent application.
		/// Adds an 'access_agent' scope that allows applications to access the agent on behalf of the signed-in user.
		/// </summary>
		/// <param name="objectId">The object ID of the application.</param>
		/// <param name="appId">The application (client) ID.</param>
		/// <returns>A task representing the asynchronous operation.</returns>
		/// <exception cref="Exception">Thrown when an error occurs configuring the identifier URI and scope.</exception>
		public async Task ConfigureIdentifierUriAndScopeAsync(string objectId, string appId)
		{
			try
			{
				var scopeGuid = Guid.NewGuid();
				var identifierUri = $"api://{appId}";

				var permissionScope = new PermissionScope
				{
					AdminConsentDescription = "Allow the application to access the agent on behalf of the signed-in user.",
					AdminConsentDisplayName = "Access agent",
					Id = scopeGuid,
					IsEnabled = true,
					Type = "User",
					Value = "access_agent"
				};

				var requestBody = new Application
				{
					IdentifierUris = new List<string> { identifierUri },
					Api = new ApiApplication
					{
						Oauth2PermissionScopes = new List<PermissionScope> { permissionScope }
					}
				};

				await _graphServiceClient.Applications[objectId]
					.PatchAsync(requestBody, requestConfiguration =>
					{
						requestConfiguration.Headers.Add("OData-Version", "4.0");
					});
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error configuring identifier URI and scope");
				throw;
			}
		}

		/// <summary>
		/// Adds a federated identity credential to an agent application for workload identity federation.
		/// This enables the application to authenticate using a managed identity.
		/// </summary>
		/// <param name="objectId">The object ID of the application.</param>
		/// <param name="tenantId">The Azure AD tenant ID.</param>
		/// <param name="msiPrincipalId">The managed service identity principal ID.</param>
		/// <param name="credentialName">The name for the federated identity credential. Defaults to "FIC_MSI".</param>
		/// <returns>A task representing the asynchronous operation.</returns>
		/// <exception cref="Exception">Thrown when an error occurs adding the federated identity credential.</exception>
		public async Task AddFederatedIdentityCredentialAsync(string objectId, string tenantId, string msiPrincipalId, string credentialName = "FIC_MSI")
		{
			try
			{
				// Use the SDK's direct model for federated identity credentials
				var credential = new FederatedIdentityCredential
				{
					Name = credentialName,
					Issuer = $"https://login.microsoftonline.com/{tenantId}/v2.0",
					Subject = msiPrincipalId,
					Audiences = new List<string> { "api://AzureADTokenExchange" }
				};

				await _graphServiceClient.Applications[objectId].FederatedIdentityCredentials
					.PostAsync(credential);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error adding federated identity credential");
				throw;
			}
		}

		private async Task<string?> GetCurrentUserOidAsync()
		{
			var authState = await _authenticationStateProvider.GetAuthenticationStateAsync();
			var user = authState.User;
			
			// Get the OID claim
			return user.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier")?.Value
				?? user.FindFirst("oid")?.Value;
		}
	}
}

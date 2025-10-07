using AgentIdentityModel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.Graph;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Web;

namespace api.Controllers
{
	/// <summary>
	/// Controller for managing agent user identities.
	/// </summary>
	[Route("api/[controller]")]
	[ApiController]
	public class AgentUserIdentityController : ControllerBase
	{
		private readonly IDownstreamApi _downstreamApi;
		private readonly GraphServiceClient _graphServiceClient;
		private readonly ILogger<AgentUserIdentityController> _logger;

		/// <summary>
		/// Initializes a new instance of the <see cref="AgentUserIdentityController"/> class.
		/// </summary>
		/// <param name="downstreamApi">The downstream API service for making API calls.</param>
		/// <param name="graphServiceClient">The Microsoft Graph service client.</param>
		/// <param name="logger">The logger instance.</param>
		public AgentUserIdentityController(
			IDownstreamApi downstreamApi,
			GraphServiceClient graphServiceClient,
			ILogger<AgentUserIdentityController> logger)
		{
			_downstreamApi = downstreamApi;
			_graphServiceClient = graphServiceClient;
			_logger = logger;
		}


		/// <summary>
		/// Creates a new Agent User Identity under the specified Agent Identity.
		/// </summary>
		/// <param name="agentIdentityId">The ID of the agent identity to create the user under.</param>
		/// <param name="upn">The User Principal Name (UPN) for the new agent user identity.</param>
		/// <returns>An action result containing the newly created agent user identity.</returns>
		[HttpPost]
		public async Task<IActionResult> PostAsync(string agentIdentityId, string upn)
		{
			if (string.IsNullOrEmpty(agentIdentityId))
			{
				return BadRequest("Agent identity ID is required");
			}

			if (string.IsNullOrEmpty(upn))
			{
				return BadRequest("User Principal Name (UPN) is required");
			}

			_logger.LogInformation("Creating Agent user indentity of: {UPN} using agent identity: {AgentIdentityId}",
									upn, agentIdentityId);

			try
			{
				// Get the service to call the downstream API (preconfigured in the appsettings.json file)
				var requestBody = new AgentIdUser
				{
					displayName = upn.Substring(0, upn.IndexOf('@', StringComparison.Ordinal)),
					mailNickname = upn.Substring(0, upn.IndexOf('@', StringComparison.Ordinal)),
					userPrincipalName = upn,
					accountEnabled = true,
					identityParentId = agentIdentityId
				};

				// Call the downstream API (canary Graph) with a POST request to create an Agent Identity
				var newAgentId = await _downstreamApi.PostForAppAsync<AgentIdUser, AgentIdUser>(
					"msGraphAgentApplication",
					requestBody,
					options =>
					{
						options.RelativePath = "/beta/users";
					}
				);

				_logger.LogInformation("Successfully retrieved user information for UPN: {UPN}", upn);

				return Ok(newAgentId);
			}
			catch (Exception ex)
			{
				return Problem(
					detail: $"Unexpected error: {ex.Message}",
					statusCode: 500);
			}
		}


		/// <summary>
		/// Gets user details for a specific agent user identity.
		/// </summary>
		/// <param name="agentIdentityId">The ID of the agent identity.</param>
		/// <param name="upn">The User Principal Name (UPN) of the user to retrieve.</param>
		/// <returns>An action result containing the user details and profile photo if available.</returns>
		[HttpGet]
		public async Task<IActionResult> GetAsync(string agentIdentityId, string upn)
		{
			if (string.IsNullOrEmpty(agentIdentityId))
			{
				return BadRequest("Agent identity ID is required");
			}

			if (string.IsNullOrEmpty(upn))
			{
				return BadRequest("User Principal Name (UPN) is required");
			}

			try
			{
				_logger.LogInformation("Getting user details for UPN: {UPN} using agent identity: {AgentIdentityId}",
					upn, agentIdentityId);

				// Get user details from Microsoft Graph using agent user identity
				var user = await _graphServiceClient.Me
					.GetAsync(requestConfiguration =>
					{
						// Configure to use agent user identity
						requestConfiguration.Options.WithAuthenticationOptions(options =>
							options.WithAgentUserIdentity(agentIdentityId, upn));
					});

				if (user == null)
				{
					_logger.LogWarning("User with UPN '{UPN}' not found", upn);
					return NotFound($"User with UPN '{upn}' not found");
				}

				_logger.LogInformation("Successfully retrieved user information for UPN: {UPN}", upn);

				// Try to get the user's profile photo
				string? photoBase64 = null;
				try
				{
					// Get the photo stream using the Graph SDK
					var photoStream = await _graphServiceClient.Me
						.Photo
						.Content
						.GetAsync(requestConfiguration =>
						{
							// Configure to use agent user identity
							requestConfiguration.Options.WithAuthenticationOptions(options =>
								options.WithAgentUserIdentity(agentIdentityId, upn));
						});

					if (photoStream != null)
					{
						// Convert the photo to a base64 string for transmission to the client
						using (var memoryStream = new MemoryStream())
						{
							await photoStream.CopyToAsync(memoryStream);
							var photoBytes = memoryStream.ToArray();
							photoBase64 = Convert.ToBase64String(photoBytes);
						}
						_logger.LogInformation("Successfully retrieved profile photo for UPN: {UPN}", upn);
					}
				}
				catch (Exception photoEx)
				{
					// Don't fail the entire request if the photo couldn't be retrieved
					_logger.LogWarning(photoEx, "Failed to retrieve profile photo for UPN: {UPN}", upn);
				}

				// Create response with user information
				var response = new
				{
					Success = true,
					Message = "Successfully retrieved user information",
					UserPrincipalName = user.UserPrincipalName ?? upn,
					DisplayName = user.DisplayName ?? "N/A",
					JobTitle = user.JobTitle ?? "N/A",
					ProfilePhoto = photoBase64,
					HasPhoto = !string.IsNullOrEmpty(photoBase64)
				};

				return Ok(response);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error retrieving user information using agent user identity. AgentIdentityId: {AgentIdentityId}, UPN: {UPN}",
					agentIdentityId, upn);

				return StatusCode(500, new
				{
					Success = false,
					Message = "Error retrieving user information",
					Error = ex.ToString()
				});
			}
		}
	}
}

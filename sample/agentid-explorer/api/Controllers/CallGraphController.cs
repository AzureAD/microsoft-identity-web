using Azure.ResourceManager;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Graph;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Web;
using System.Security.Claims;

namespace api.Controllers
{
	/// <summary>
	/// Controller for calling Microsoft Graph using agent identities.
	/// </summary>
	[Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class CallGraphController : ControllerBase
    {
        private readonly ILogger<CallGraphController> _logger;
        private readonly IAuthorizationHeaderProvider _authorizationHeaderProvider;
        private readonly GraphServiceClient _graphServiceClient;

		/// <summary>
		/// Initializes a new instance of the <see cref="CallGraphController"/> class.
		/// </summary>
		/// <param name="logger">The logger instance.</param>
		/// <param name="authorizationHeaderProvider">The authorization header provider.</param>
		/// <param name="graphServiceClient">The Microsoft Graph service client.</param>
		public CallGraphController(
            ILogger<CallGraphController> logger, 
            IAuthorizationHeaderProvider authorizationHeaderProvider,
            GraphServiceClient graphServiceClient)
		{
            _logger = logger;
            _authorizationHeaderProvider = authorizationHeaderProvider;
            _graphServiceClient = graphServiceClient;
		}

		/// <summary>
		/// Gets user information from Microsoft Graph on behalf of the user using an agent identity.
		/// </summary>
		/// <param name="agentIdentityId">The agent identity ID to use for authentication.</param>
		/// <returns>User information from Microsoft Graph.</returns>
		[HttpGet("agent-obo-user")]
        public async Task<IActionResult> GetInfoOnBehalfOfUserAsync([FromQuery] string agentIdentityId)
        {
            if (string.IsNullOrEmpty(agentIdentityId))
            {
                return BadRequest("Agent identity ID is required");
            }

            try
            {
                // Call Microsoft Graph API using the agent identity on behalf of the user
                var me = await _graphServiceClient.Me.GetAsync(r => 
                {
					r.Options.WithAuthenticationOptions(o => 
                    { 
                        o.WithAgentIdentity(agentIdentityId);
					});
                    r.QueryParameters.Select = new[] { "displayName", "userPrincipalName", "jobTitle" };
				});

                if (me == null)
                {
                    return StatusCode(500, new { 
                        Success = false, 
                        Message = "Failed to get user information from Microsoft Graph" 
                    });
                }

                // Return the user information
                return Ok(new
                {
                    Success = true,
                    Message = "Successfully called Microsoft Graph API using agent identity",
                    AdditionalData = new Dictionary<string, object?>
                    {
						{ "UserPrincipalName", me.UserPrincipalName },
						{ "DisplayName", me.DisplayName },
						{ "JobTitle", me.JobTitle ?? "No job title specified" },
						{ "Agent Identity ID", agentIdentityId },
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred when calling Microsoft Graph with agent identity {AgentIdentityId}", agentIdentityId);
                return StatusCode(500, new { 
                    Success = false, 
                    Message = $"Error calling Microsoft Graph: {ex.Message}",
#if DEBUG
					// Don't return exception details in production!
					Error = ex.ToString()
#endif                
				});
				}

		}


		/// <summary>
		/// Gets applications from Microsoft Graph on behalf of the agent application using an agent identity.
		/// </summary>
		/// <param name="agentIdentityId">The agent identity ID to use for authentication.</param>
		/// <returns>Application information from Microsoft Graph.</returns>
		[HttpGet("agent-obo-app")]
		public async Task<IActionResult> AutonomousAgentCallsGraphAsync([FromQuery] string agentIdentityId)
		{
			if (string.IsNullOrEmpty(agentIdentityId))
			{
				return BadRequest("Agent identity ID is required");
			}

			try
			{
				// Call Microsoft Graph API using the agent identity on behalf of the agent identity itself.
				var apps = await _graphServiceClient.Applications.GetAsync(r =>
				{
					r.Options.WithAuthenticationOptions(o =>
					{
						o.WithAgentIdentity(agentIdentityId);
						o.RequestAppToken = true;
					});
				});

				if (apps == null || apps.Value == null || !apps.Value.Any())
				{
					return StatusCode(500, new
					{
						Success = false,
						Message = "Failed to get applications information from Microsoft Graph"
					});
				}

				var firstApp = apps.Value.FirstOrDefault();

				// Return the user information
				return Ok(new
				{
					Success = true,
					Message = $"Successfully called Microsoft Graph API using agent identity {agentIdentityId}",
					AdditionalData = new Dictionary<string, object?>
					{
						{ "Agent Identity ID", agentIdentityId },
						{ "First app display name", firstApp?.DisplayName },
						{ "First app AppId", firstApp?.AppId },
					}
				});
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error occurred when calling Microsoft Graph with agent identity {AgentIdentityId}", agentIdentityId);
				return StatusCode(500, new
				{
					Success = false,
					Message = $"Error calling Microsoft Graph: {ex.Message}",
#if DEBUG
					// Don't return exception details in production!
					Error = ex.ToString()
#endif				
				});
				}
		}
	}
}

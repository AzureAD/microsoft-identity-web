using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Web;
using System.Net.Http.Headers;

namespace api.Controllers
{
	/// <summary>
	/// Controller for calling any API using agent identities via REST.
	/// </summary>
	[Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class CallAnyApiController : ControllerBase
    {
        private readonly ILogger<CallAnyApiController> _logger;
        private readonly IAuthorizationHeaderProvider _authorizationHeaderProvider;

		/// <summary>
		/// Initializes a new instance of the <see cref="CallAnyApiController"/> class.
		/// </summary>
		/// <param name="logger">The logger instance.</param>
		/// <param name="authorizationHeaderProvider">The authorization header provider.</param>
		public CallAnyApiController(
            ILogger<CallAnyApiController> logger, 
            IAuthorizationHeaderProvider authorizationHeaderProvider)
		{
            _logger = logger;
            _authorizationHeaderProvider = authorizationHeaderProvider;
		}

		/// <summary>
		/// Gets user information from Microsoft Graph on behalf of the user using an agent identity (REST).
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

                // Create authorization header provider options with agent identity
                var options = new AuthorizationHeaderProviderOptions().WithAgentIdentity(agentIdentityId);

				// Request user token for the agent identity
				string authorizationHeaderWithUserToken = await _authorizationHeaderProvider.CreateAuthorizationHeaderForUserAsync(
                    new[] { "https://graph.microsoft.com/.default" }, 
                    options);

				if (string.IsNullOrEmpty(authorizationHeaderWithUserToken))
				{
					return StatusCode(500, new
					{
						Success = false,
						Message = "Failed to get authorization token for app"
					});
				}

				HttpClient httpClient = new HttpClient();
				httpClient.DefaultRequestHeaders.Authorization = AuthenticationHeaderValue.Parse(authorizationHeaderWithUserToken);
				var result = await httpClient.GetAsync("https://graph.microsoft.com/v1.0/me");
				string data = await result.Content.ReadAsStringAsync();

				// Return the user information
				return Ok(new
                {
                    Success = true,
                    Message = "Successfully called Microsoft Graph API using agent identity (REST)",
                    AdditionalData = new Dictionary<string, object>
                    {
						{ "Data", data},
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
		/// Gets applications from Microsoft Graph on behalf of the agent application using an agent identity (REST).
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
				// Create authorization header provider options with agent identity
				var options = new AuthorizationHeaderProviderOptions().WithAgentIdentity(agentIdentityId);

				// Request user token for the agent identity
				string authorizationHeaderWithAppToken = await _authorizationHeaderProvider.CreateAuthorizationHeaderForAppAsync(
					 "https://graph.microsoft.com/.default",
					options);

				if (string.IsNullOrEmpty(authorizationHeaderWithAppToken))
				{
					return StatusCode(500, new
					{
						Success = false,
						Message = "Failed to get authorization token for app"
					});
				}
				HttpClient httpClient = new HttpClient();
				httpClient.DefaultRequestHeaders.Authorization = AuthenticationHeaderValue.Parse(authorizationHeaderWithAppToken);
				var result = await httpClient.GetAsync("https://graph.microsoft.com/v1.0/applications");
				string data = await result.Content.ReadAsStringAsync();

				// Return the user information
				return Ok(new
				{
					Success = true,
					Message = "Successfully called Microsoft Graph API using agent identity (REST)",
					AdditionalData = new Dictionary<string, object>
					{
						{ "Data", data},
					}
				});
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error occurred when calling Microsoft Graph (REST) with agent identity {AgentIdentityId}", agentIdentityId);
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

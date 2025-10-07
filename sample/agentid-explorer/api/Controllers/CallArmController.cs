using Azure.ResourceManager;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Web;

namespace api.Controllers
{
	/// <summary>
	/// Controller for calling Azure Resource Manager using agent identities.
	/// </summary>
	[Route("api/[controller]")]
	[ApiController]
	[Authorize]
	public class CallAzureController : ControllerBase
	{
		private readonly ILogger<CallAzureController> _logger;

        /// <summary>
        /// Gets the token credential used for authentication with Azure SDKs.
        /// </summary>
		public MicrosoftIdentityTokenCredential TokenCredential { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="CallAzureController"/> class.
		/// </summary>
		/// <param name="azureTokenCredential">The Azure token credential.</param>
		/// <param name="logger">The logger instance.</param>
		public CallAzureController(
			MicrosoftIdentityTokenCredential azureTokenCredential,
			ILogger<CallAzureController> logger)
		{
			// Inject a MiseAzureTokenCredential instance
			TokenCredential = azureTokenCredential;
			_logger = logger;
		}

		/// <summary>
		/// Calls ARM (Azure Resource Manager) using the specified agent identity.
		/// </summary>
		/// <param name="agentIdentityId">The agent identity ID to use for authentication.</param>
		/// <returns>Information about the subscription.</returns>
		[HttpGet("autonomous-agent-calls-arm")]
		public async Task<IActionResult> CallArmAsync([FromQuery] string agentIdentityId)
		{
			if (string.IsNullOrEmpty(agentIdentityId))
			{
				return BadRequest("Agent identity ID is required");
			}

			try
			{
				// Update the token credential options to use the specified agent identity
				TokenCredential.Options.WithAgentIdentity(agentIdentityId);

				// Call the Azure SDK of your choice, e.g., Azure Resource Manager (ARM)
				TokenCredential.Options.RequestAppToken = true;
				ArmClient armClient = new ArmClient(TokenCredential);
				var subscription = await armClient.GetDefaultSubscriptionAsync();

				// Return the subscription information
				return Ok(new
				{
					Success = true,
					Message = $"Successfully called ARM using agent identity {agentIdentityId}",

					AdditionalData = new Dictionary<string, object?>
					{
						["Subscription Name"] = subscription.Data.DisplayName,
						["Subscription ID"] = subscription.Data.Id.SubscriptionId,
						["Subscription Data"] = subscription.Data.Id?.ToString(),
						["Agent Identity ID"] = agentIdentityId,
					}
				}
			);

			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error occurred when calling ARM with agent identity {AgentIdentityId}", agentIdentityId);
				return StatusCode(500, new
				{
					Success = false,
					Message = $"Error calling ARM: {ex.Message}",
#if DEBUG
					// Don't return exception details in production!
					Error = ex.ToString()
#endif
				});
			}
		}

	}
}

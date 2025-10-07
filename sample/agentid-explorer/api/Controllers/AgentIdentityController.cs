using AgentIdentityModel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Graph.Models;
using Microsoft.Identity.Abstractions;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace api.Controllers
{
    /// <summary>
    /// Controller for agent identities
    /// </summary>
	[Route("api/[controller]")]
    [ApiController]
    public class AgentIdentityController : ControllerBase
    {
        private string agentApplicationId;
        private IDownstreamApi DownstreamApi { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="AgentIdentityController"/> class.
        /// </summary>
        /// <param name="configuration">The configuration containing Azure AD settings.</param>
        /// <param name="downstreamApi">The downstream API service for making API calls.</param>
		public AgentIdentityController(IConfiguration configuration, IDownstreamApi downstreamApi)
        {
            agentApplicationId = configuration["AzureAd:ClientId"] ?? "Define ClientId in the configuration.";
            DownstreamApi = downstreamApi;
        }

        /// <summary>
        /// Creates a new Agent Identity with the specified name (for the current Agent Application).
        /// </summary>
        /// <param name="agentIdentityName">The display name for the new agent identity.</param>
        /// <returns>The newly created agent identity.</returns>
        [HttpPost]
        public async Task<AgentIdentity> PostAsync([FromBody] string agentIdentityName)
        {
            string? ownerOid = HttpContext.User.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier")?.Value
                ?? HttpContext.User.FindFirst("oid")?.Value;
            // Call the downstream API with a POST request to create an Agent Identity
            var newAgentIdentity = await DownstreamApi.PostForAppAsync<AgentIdentity, AgentIdentity>(
                "msGraphAgentIdentity",
                new AgentIdentity
                {
                    displayName = agentIdentityName,
                    agentAppId = agentApplicationId,
                    Owners = [$"https://graph.microsoft.com/v1.0/users/{ownerOid}"]
                }
            );
            return newAgentIdentity!;
        }

        /// <summary>
        /// Gets the list of Agent Identities associated with the current agent application (on behalf of the current user).
        /// </summary>
        /// <returns>A collection of agent identities.</returns>
        [HttpGet]
        public async Task<IEnumerable<AgentIdentity>> GetAsync()
        {
            return await DownstreamApi.GetForAppAsync<IEnumerable<AgentIdentity>>("msGraphAgentIdentity") ?? Enumerable.Empty<AgentIdentity>();
        }


        /// <summary>
        /// Deletes an Agent Identity by ID (on behalf of the agent application).
        /// </summary>
        /// <param name="id">The unique identifier of the agent identity to delete.</param>
        /// <returns>The result of the delete operation.</returns>
        [HttpDelete("{id}")]
        public async Task<string> DeleteAsync(string id)
        {
            var result = await DownstreamApi.DeleteForAppAsync<string, object>(
                "msGraphAgentIdentity",
                input: null!,
                  options =>
                {
                    options.RelativePath += $"/{id}"; // Specify the ID of the agent identity to delete
                });
            return result?.ToString() ?? string.Empty;
        }
    }
}

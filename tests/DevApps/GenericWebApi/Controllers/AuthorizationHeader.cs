using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Web.Resource;

namespace webApi.Controllers;

[Authorize]
[ApiController]
[Route("[controller]")]
[RequiredScope(RequiredScopesConfigurationKey = "AzureAd:Scopes")]
public class AuthorizationHeader : ControllerBase
{
    private readonly ILogger<AuthorizationHeader> _logger;

    private readonly IAuthorizationHeaderProvider _authorizationHeaderProvider;

    private readonly IConfiguration _configuration;

    public AuthorizationHeader(ILogger<AuthorizationHeader> logger,
                                     IAuthorizationHeaderProvider authorizationHeaderProvider,
                                     IConfiguration configuration)
    {
        _logger = logger;
        _authorizationHeaderProvider = authorizationHeaderProvider;
        _configuration = configuration;
    }


    [HttpGet(Name = "GetAuthorizationHeader")]
    public async Task<string> GetAuthorizationHeader(string serviceName)
    {
        Dictionary<string, DownstreamApiOptions> downstreamApiOptions = new Dictionary<string, DownstreamApiOptions>();
        _configuration.GetSection("DownstreamApis").Bind(downstreamApiOptions);

        if (!downstreamApiOptions.ContainsKey(serviceName))
        {
            throw new ArgumentException($"The downstream API {serviceName} is not configured.");
        }

        var serviceOptions = downstreamApiOptions[serviceName];
        if (serviceOptions.RequestAppToken)
        {
            return await _authorizationHeaderProvider.CreateAuthorizationHeaderForAppAsync(serviceOptions.Scopes?.FirstOrDefault()!, serviceOptions);
        }
        else
        {
            return await _authorizationHeaderProvider.CreateAuthorizationHeaderForUserAsync(serviceOptions.Scopes!, serviceOptions);
        }
    }

}

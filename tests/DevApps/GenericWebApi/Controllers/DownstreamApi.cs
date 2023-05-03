using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Web.Resource;

namespace webApi.Controllers;

[Authorize]
[ApiController]
[Route("[controller]")]
[RequiredScope(RequiredScopesConfigurationKey = "AzureAd:Scopes")]
public class DownstreamApi : ControllerBase
{
    private readonly ILogger<DownstreamApi> _logger;

    private readonly IDownstreamApi _downstreamApi;

    public DownstreamApi(ILogger<DownstreamApi> logger,
                                     IDownstreamApi downstreamApi)
    {
        _logger = logger;
        _downstreamApi = downstreamApi;
    }

    /// <summary>
    /// Call downstream API
    /// </summary>
    /// <param name="serviceName">Name of the service to call. This is the name of the downstream API
    /// options in the appsettings.json file.</param>
    /// <param name="input"></param>
    /// <returns></returns>
    /// <exception cref="HttpRequestException"></exception>
    [HttpGet(Name = "CallDownstreamWebApi")]
    public async Task<string> CallDownstreamWebApi(string serviceName, string input)
    {
        using var response = await _downstreamApi.CallApiAsync(serviceName, content:new StringContent(input)).ConfigureAwait(false);
        if (response.StatusCode == System.Net.HttpStatusCode.OK)
        {
            var apiResult = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            return apiResult;
        }
        else
        {
            var error = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            throw new HttpRequestException($"Invalid status code in the HttpResponseMessage: {response.StatusCode}: {error}");
        }
    }
}

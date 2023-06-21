using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Web;

namespace OTelClient.Pages;

[AuthorizeForScopes(ScopeKeySection = "DownstreamApi:Scopes")]
public class IndexModel : PageModel
{
    private readonly ILogger<IndexModel> _logger;

    private readonly IDownstreamApi _downstreamApi;

    public IndexModel(ILogger<IndexModel> logger,
                        IDownstreamApi downstreamWebApi)
    {
        _logger = logger;
        _downstreamApi = downstreamWebApi;
    }

    public async Task OnGet()
    {
        DiagnosticsConfig.AppTokenRequestCounter.Add(1,
            new KeyValuePair<string, object?>("Action", nameof(OnGet)));

        using var response = await _downstreamApi.CallApiForUserAsync("DownstreamApi").ConfigureAwait(false);
        if (response.StatusCode == System.Net.HttpStatusCode.OK)
        {
            var apiResult = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            ViewData["ApiResult"] = apiResult;
        }
        else
        {
            var error = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            throw new HttpRequestException($"Invalid status code in the HttpResponseMessage: {response.StatusCode}: {error}");
        }
    }
}

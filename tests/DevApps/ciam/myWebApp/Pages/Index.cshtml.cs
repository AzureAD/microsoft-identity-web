using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Web;

namespace myWebApp.Pages;

[AuthorizeForScopes(ScopeKeySection = "DownstreamApi:Scopes")]
public class IndexModel : PageModel
{
    private readonly ILogger<IndexModel> _logger;

    private readonly IDownstreamApi _downstreamWebApi;

    public IndexModel(ILogger<IndexModel> logger,
                        IDownstreamApi downstreamWebApi)
    {
            _logger = logger;
        _downstreamWebApi = downstreamWebApi;
    }

    public async Task OnGet()
    {
        using var response = await _downstreamWebApi.CallApiForUserAsync("DownstreamApi").ConfigureAwait(false);
        if (response.StatusCode == System.Net.HttpStatusCode.OK)
        {
            var apiResult = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            ViewData["ApiResult"] = apiResult;
        }
        else
        {
            var error = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            ViewData["ApiResult"] = $"Invalid status code in the HttpResponseMessage: {response.StatusCode}: {error}";
        }
    }
}

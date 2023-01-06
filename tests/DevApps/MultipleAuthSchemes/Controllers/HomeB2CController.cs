using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Identity.Web;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using mvcwebapp_graph.Models;
using Microsoft.Identity.Abstractions;
using Microsoft.Extensions.Options;

namespace mvcwebapp_graph.Controllers
{
    [Authorize(AuthenticationSchemes = "B2C")]
    public class HomeB2CController : Controller
    {
        private class Item
        {
            public string name { get; set; }
        }

        private readonly ILogger<HomeController> _logger;
        private readonly IDownstreamRestApi _downstreamWebApi;

        public HomeB2CController(ILogger<HomeController> logger, IDownstreamRestApi downstreamWebApi)
        {
            _downstreamWebApi = downstreamWebApi;
            _logger = logger;
        }

        [AuthorizeForScopes(
            ScopeKeySection = "DownstreamB2CApi:Scopes", UserFlow = "b2c_1_susi", AuthenticationScheme = "B2C")]
        public async Task<IActionResult> Index()
        {
            var value = await _downstreamWebApi.GetForUserAsync<Item>("DownstreamB2CApi",
                options => options.AcquireTokenOptions.AuthenticationOptionsName = "B2C");
            ViewData["ApiResult"] = value.name;
            return View();
        }
        public IActionResult Privacy()
        {
            return View();
        }

        [AllowAnonymous]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}

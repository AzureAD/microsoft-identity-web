using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Identity.Web;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using mvcwebapp_graph.Models;

namespace mvcwebapp_graph.Controllers
{
    [Authorize(AuthenticationSchemes = "B2C")]
    public class HomeB2CController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IDownstreamWebApi _downstreamWebApi;

        public HomeB2CController(ILogger<HomeController> logger, IDownstreamWebApi downstreamWebApi)
        {
            _downstreamWebApi = downstreamWebApi;
            _logger = logger;
        }

        [AuthorizeForScopes(
            ScopeKeySection = "DownstreamB2CApi:Scopes", UserFlow = "b2c_1_susi")]
        public async Task<IActionResult> Index()
        {
            var value = await _downstreamWebApi.GetForUserAsync<Task>("DownstreamB2CApi", "", null, null, "B2C");
            return View(value);
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

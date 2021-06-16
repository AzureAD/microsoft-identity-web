using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Identity.Web;
using Microsoft.Graph;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using mvcwebapp_graph.Models;

namespace mvcwebapp_graph.Controllers
{
    [Authorize(AuthenticationSchemes = "openid2")]
    public class HomeController : Controller
    {
        private const string OpenIdScheme = "openid2";
        private readonly ILogger<HomeController> _logger;

        private readonly GraphServiceClient _graphServiceClient;

        public HomeController(ILogger<HomeController> logger, GraphServiceClient graphServiceClient)
        {
             _logger = logger;
            _graphServiceClient = graphServiceClient;
       }

        [AuthorizeForScopes(ScopeKeySection = "DownstreamApi:Scopes", AuthenticationScheme = OpenIdScheme)]
        public async Task<IActionResult> Index()
        {
            var user = await _graphServiceClient.Me.Request()
                .WithAuthenticationScheme(OpenIdScheme).GetAsync();
            ViewData["ApiResult"] = user.DisplayName;

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

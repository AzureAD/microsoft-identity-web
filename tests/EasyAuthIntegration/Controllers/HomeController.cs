using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Microsoft.Identity.Web;
using WebApplication1.Models;

namespace WebApplication1.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;


        /*
        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }
        */

        /*
        private readonly ITokenAcquisition _tokenAcquision;

        public HomeController(ILogger<HomeController> logger, ITokenAcquisition tokenAcquision)
        {
            _logger = logger;
            _tokenAcquision = tokenAcquision;
        }

        public async Task<IActionResult> Index()
        {
            string token = await _tokenAcquision.GetAccessTokenForUserAsync(new string[] { "user.read" });
            ViewData["name"] = token;
            return View();
        }
        */

        /*
        private readonly IDownstreamWebApi _downstreamWebApi;

        public HomeController(ILogger<HomeController> logger, IDownstreamWebApi downstreamWebApi)
        {
            _logger = logger;
            _downstreamWebApi = downstreamWebApi;
        }

        public async Task<IActionResult> Index()
        {
            HttpResponseMessage response = await _downstreamWebApi.CallWebApiForUserAsync("GraphBeta");
            ViewData["name"] = await response.Content.ReadAsStringAsync();
            return View();
        }
        */


        private readonly GraphServiceClient _graphServiceClient;

                public HomeController(ILogger<HomeController> logger, GraphServiceClient graphServiceClient)
                {
                    _logger = logger;
                    _graphServiceClient = graphServiceClient;
                }

                public async Task<IActionResult> Index()
                {
                    var user = await _graphServiceClient.Me.Request().GetAsync();

                    try
                    {
                        using (var photoStream = await _graphServiceClient.Me.Photo.Content.Request().GetAsync())
                        {
                            byte[] photoByte = ((MemoryStream)photoStream).ToArray();
                            ViewData["photo"] = Convert.ToBase64String(photoByte);
                        }
                        ViewData["name"] = user.DisplayName;
                    }
                    catch (Exception)
                    {
                        ViewData["photo"] = null;
                    }

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

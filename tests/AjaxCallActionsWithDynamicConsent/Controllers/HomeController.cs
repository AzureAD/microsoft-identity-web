using AjaxCallActionsWithDynamicConsent.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Web;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace AjaxCallActionsWithDynamicConsent.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ITokenAcquisition _tokenAcquisition;

        public HomeController(ILogger<HomeController> logger, ITokenAcquisition tokenAcquisition)
        {
            _logger = logger;
            _tokenAcquisition = tokenAcquisition;
        }

        public IActionResult Index()
        {
            return View();
        }

        [AuthorizeForScopes(Scopes = new[] { "https://ccbcc.sharepoint.com/AllSites.Read" })]
        public async Task<IActionResult> AjaxAction()
        {
            var aToken = await GetAccessTokenforResource("https://ccbcc.sharepoint.com/AllSites.Read");
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        /// <summary>
        /// Private: Gets and returns an access token for the provided resource.
        /// </summary>
        /// <param name="resource">Resource to obtain access token for</param>
        /// <returns></returns>
        private async Task<string> GetAccessTokenforResource(string scope)
        {
            // Get the access token for the resource.
            string accessToken = await _tokenAcquisition.GetAccessTokenForUserAsync(new[] { scope });
            return accessToken;
        }
    }
}

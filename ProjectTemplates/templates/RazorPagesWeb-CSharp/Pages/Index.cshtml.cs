using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
#if (GenerateApi)
using Microsoft.Extensions.Configuration;
using Microsoft.Identity.Web;
using System.Net;
using System.Net.Http;
#endif
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace Company.WebApplication1.Pages
{
#if (GenerateApi)
    using Services;

#endif 
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;

#if (GenerateApi)
        private readonly IDownstreamWebApi _downstreamWebApi;

        public IndexModel(ILogger<HomeController> logger,
                          IDownstreamWebApi downstreamWebApi)
        {
             _logger = logger;
            _downstreamWebApi = downstreamWebApi;
       }

        [AuthorizeForScopes(ScopeKeySection = "CalledApi:CalledApiScopes")]
        public void OnGet()
        {
            ViewData["ApiResult"] = await _downstreamWebApi.CallWebApi();

            // You can also specify the relative endpoint and the scopes
            // ViewData["ApiResult"] = await _downstreamWebApi.CallWebApi("me", new string[] {"user.read"});

            return View();
        }
#else
        public IndexModel(ILogger<IndexModel> logger)
        {
            _logger = logger;
        }
#endif

        public void OnGet()
        {

        }
    }
}

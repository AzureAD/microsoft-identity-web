// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using grpc;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Web;
using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;
using WebApp_OpenIDConnect_DotNet.Models;

namespace WebApp_OpenIDConnect_DotNet.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private ITokenAcquisition _tokenAcquisition;

        private IDownstreamApi _downstreamApi;

        public HomeController(
            ITokenAcquisition tokenAcquisition,
            IDownstreamApi downstreamApi)
        {
            _tokenAcquisition = tokenAcquisition;
            _downstreamApi = downstreamApi;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        [AllowAnonymous]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        [HttpGet]
        [AuthorizeForScopes(ScopeKeySection = "SayHello:Scopes")]
        public async Task<ActionResult> SayHello()
        {
            var channel = GrpcChannel.ForAddress("https://localhost:5001");
            var client = new Greeter.GreeterClient(channel);

            string token = await _tokenAcquisition.GetAccessTokenForUserAsync(new string[] { "api://a021aff4-57ad-453a-bae8-e4192e5860f3/access_as_user" }).ConfigureAwait(false);

            var headers = new Metadata();
            headers.Add("Authorization", $"Bearer {token}");

            var reply = await client.SayHelloAsync(
            new HelloRequest { Name = "GreeterClient" }, headers);
            ViewBag.reply = reply.Message;
            return View();
        }

        [HttpGet]
        [AuthorizeForScopes(ScopeKeySection = "AzureFunction:Scopes")]
        public async Task<ActionResult> CallAzureFunction()
        {
            string message = await _downstreamApi.CallApiForUserAsync<string>(
                "AzureFunction");
            ViewBag.reply = message;
            return View();
        }
    }
}

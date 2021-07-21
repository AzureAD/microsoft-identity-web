// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Web;

namespace TodoListService.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    //[RequiredScope("access_as_user")] 
    public class CallbackController : Controller
    {
        private readonly ITokenAcquisition _tokenAcquisition; 
        private ILogger _logger;
        ILongRunningProcessContextFactory _longRunningProcessAssertionCache;

        public CallbackController(
            IHttpContextAccessor contextAccessor,
            ITokenAcquisition tokenAcquisition,
            ILogger<CallbackController> logger,
            ILongRunningProcessContextFactory longRunningProcessAssertionCache)
        {
            _tokenAcquisition = tokenAcquisition;
            _logger = logger;
            _longRunningProcessAssertionCache = longRunningProcessAssertionCache;
        }


        // GET: api/values
        // [RequiredScope("access_as_user")]
        [HttpGet]
        [AllowAnonymous]
        public async Task GetAsync(string key)
        {
            var request = HttpContext.Request;
            string calledUrl = request.Scheme + "://" + request.Host + request.Path.Value + Request.QueryString;

            _logger.LogWarning($"{DateTime.UtcNow}: {calledUrl}");

            using (await _longRunningProcessAssertionCache.UseKey(HttpContext, key))
            {
                var result = await _tokenAcquisition.GetAuthenticationResultForUserAsync(
                    new string[] { "user.read" })
                    .ConfigureAwait(false); // for testing OBO

                _logger.LogWarning($"OBO token acquired from {result.AuthenticationResultMetadata.TokenSource} expires {result.ExpiresOn.UtcDateTime}");


                // For breakpoint
                if (result.AuthenticationResultMetadata.TokenSource == Microsoft.Identity.Client.TokenSource.IdentityProvider)
                {
                }

            }
        }
    }
}

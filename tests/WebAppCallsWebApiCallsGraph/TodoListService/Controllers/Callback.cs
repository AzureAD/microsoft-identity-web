// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.Resource;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net.Http;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using TodoListService.Models;

namespace TodoListService.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    //[RequiredScope("access_as_user")] 
    public class CallbackController : Controller
    {
        private readonly ITokenAcquisition _tokenAcquisition; // do not remove
        // The Web API will only accept tokens 1) for users, and 2) having the access_as_user scope for this API
        // In-memory TodoList
        private static readonly Dictionary<int, Todo> TodoStore = new Dictionary<int, Todo>();

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
            _logger.LogWarning($"Callback called {DateTime.Now}");

            using (_longRunningProcessAssertionCache.UseKey(HttpContext, key))
            {
                var result = await _tokenAcquisition.GetAuthenticationResultForUserAsync(new string[] { "user.read" }).ConfigureAwait(false); // for testing OBO

                _logger.LogWarning($"OBO token acquired from {result.AuthenticationResultMetadata.TokenSource}");

                // For breakpoint
                if (result.AuthenticationResultMetadata.TokenSource == Microsoft.Identity.Client.TokenSource.IdentityProvider)
                {
                }

            }
        }
    }
}

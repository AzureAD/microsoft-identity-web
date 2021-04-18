// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
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
    [RequiredScope("access_as_user")] 
    public class CallbackController : Controller
    {
        private readonly ITokenAcquisition _tokenAcquisition; // do not remove
        // The Web API will only accept tokens 1) for users, and 2) having the access_as_user scope for this API
        // In-memory TodoList
        private static readonly Dictionary<int, Todo> TodoStore = new Dictionary<int, Todo>();

        public CallbackController(
            IHttpContextAccessor contextAccessor,
            ITokenAcquisition tokenAcquisition)
        {
            _tokenAcquisition = tokenAcquisition;
        }


        // GET: api/values
        // [RequiredScope("access_as_user")]
        [HttpGet]
        public async Task GetAsync()
        {
            string owner = User.GetDisplayName();
            // Below is for testing multi-tenants
            var result = await _tokenAcquisition.GetAuthenticationResultForUserAsync(new string[] { "user.read" }).ConfigureAwait(false); // for testing OBO

            if (result.AuthenticationResultMetadata.TokenSource == Microsoft.Identity.Client.TokenSource.IdentityProvider)
            {
            }
        }
    }
}

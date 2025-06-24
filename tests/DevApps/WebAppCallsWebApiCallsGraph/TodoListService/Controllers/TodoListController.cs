// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.Resource;
using TodoListService.Models;

namespace TodoListService.Controllers
{
    /* equivalent 
    [Authorize(Policy = "RequiredScope(|AzureAd:Scope")]
    [Authorize(Policy = "RequiredScope(User.Read")]
    */
    //[Authorize(Policy = "foo")]
    [Route("api/[controller]")]
    [RequiredScope("access_as_user")]
    [Authorize]
    //[RequiredScope(RequiredScopesConfigurationKey = "AzureAd:Scopes")]
    public class TodoListController : Controller
    {
        private readonly ITokenAcquisition _tokenAcquisition; // do not remove
        // The Web API will only accept tokens 1) for users, and 2) having the access_as_user scope for this API
        // In-memory TodoList
        private static readonly Dictionary<int, Todo> TodoStore = new Dictionary<int, Todo>();

        public TodoListController(
            IHttpContextAccessor contextAccessor,
            ITokenAcquisition tokenAcquisition)
        {
            _tokenAcquisition = tokenAcquisition;

            // Pre-populate with sample data
            if (TodoStore.Count == 0)
            {
                TodoStore.Add(1, new Todo() { Id = 1, Owner = $"{contextAccessor.HttpContext.User.Identity.Name}", Title = "Pick up groceries" });
                TodoStore.Add(2, new Todo() { Id = 2, Owner = $"{contextAccessor.HttpContext.User.Identity.Name}", Title = "Finish invoice report" });
            }
        }

        // GET: api/values
        // [RequiredScope("access_as_user")]
        [HttpGet]
        public async Task<IEnumerable<Todo>> GetAsync()
        {
            string owner = User.GetDisplayName();

            // Below is for testing multi-tenants
            var result = await _tokenAcquisition.GetAccessTokenForUserAsync(new string[] { "user.read" }).ConfigureAwait(false); // for testing OBO

            var result2 = await _tokenAcquisition.GetAccessTokenForUserAsync(new string[] { "user.read.all" },
            tokenAcquisitionOptions: new TokenAcquisitionOptions { ForceRefresh = true }).ConfigureAwait(false); // for testing OBO

            await RegisterPeriodicCallbackForLongProcessing(null);

            // string token1 = await _tokenAcquisition.GetAccessTokenForUserAsync(new string[] { "user.read" }, "f645ad92-e38d-4d1a-b510-d1b09a74a8ca").ConfigureAwait(false);
            // string token2 = await _tokenAcquisition.GetAccessTokenForUserAsync(new string[] { "user.read" }, "3ebb7dbb-24a5-4083-b60c-5a5977aabf3d").ConfigureAwait(false);

            await Task.FromResult(0); // fix CS1998 while the lines about the 2 tokens are commented out.
            return TodoStore.Values.Where(x => x.Owner == owner);
        }

        /// <summary>
        /// This methods the processing of user data where the web API periodically checks the user
        /// date (think of OneDrive producing albums)
        /// </summary>
        private async Task RegisterPeriodicCallbackForLongProcessing(string keyHint)
        {
            // Get the token incoming to the web API - we could do better here.
            TokenAcquisitionOptions tokenAcquisitionOptions = new TokenAcquisitionOptions()
            {
                LongRunningWebApiSessionKey = keyHint ?? TokenAcquisitionOptions.LongRunningWebApiSessionKeyAuto
            };

            _= await _tokenAcquisition.GetAuthenticationResultForUserAsync(new string[] { "user.read" }, tokenAcquisitionOptions: tokenAcquisitionOptions);
            string key = tokenAcquisitionOptions.LongRunningWebApiSessionKey;

            // Build the URL to the callback controller, based on the request.
            var request = HttpContext.Request;
            string endpointPath = request.Path.Value.Replace("todolist", "callback", StringComparison.OrdinalIgnoreCase);
            string url = $"{request.Scheme}://{request.Host}{endpointPath}?key={key}";

            // Setup a timer so that the API calls back the callback every 10 mins.
            Timer timer = new Timer(async (state) =>
            {
                HttpClient httpClient = new HttpClient();
                
                var message = await httpClient.GetAsync(url); // CodeQL [SM03781] Requests are made to a sample controller on localhost
            }, null, 1000, 1000 * 60 * 1);  // Callback every minute
        }


        // GET: api/values
        // [RequiredScope("Weather.Write")]
        [HttpGet("{id}", Name = "Get")]
        public Todo Get(int id)
        {
            return TodoStore.Values.FirstOrDefault(t => t.Id == id);
        }

        // [RequiredScope("Weather.Write")]
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
            TodoStore.Remove(id);
        }

        // POST api/values
        // [RequiredScope("access_as_user")]
        [HttpPost]
        public IActionResult Post([FromBody] Todo todo)
        {
            int id = TodoStore.Values.OrderByDescending(x => x.Id).FirstOrDefault().Id + 1;
            Todo todonew = new Todo() { Id = id, Owner = User.GetDisplayName(), Title = todo.Title };
            TodoStore.Add(id, todonew);

            return Ok(todo);
        }

        // PATCH api/values
        // [RequiredScope("access_as_user")]
        [HttpPatch("{id}")]
        public IActionResult Patch(int id, [FromBody] Todo todo)
        {
            if (id != todo.Id)
            {
                return NotFound();
            }

            if (TodoStore.Values.FirstOrDefault(x => x.Id == id) == null)
            {
                return NotFound();
            }

            TodoStore.Remove(id);
            TodoStore.Add(id, todo);

            return Ok(todo);
        }
    }
}

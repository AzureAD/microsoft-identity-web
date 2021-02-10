// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Web;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using TodoListService.Models;

namespace TodoListClient.Controllers
{
    public class TodoListController : Controller
    {
        private readonly ITokenAcquisition _tokenAcquisition;
        private readonly IDownstreamWebApi _downstreamWebApi;
        private const string ServiceName = "TodoList";
        private const string Scope = "https://fabrikamb2c.onmicrosoft.com/tasks/read";
        private const string Susi = "b2c_1_susi";
        private const string EditProfile = "b2c_1_edit_profile";
        private const string Claims = "Claims";


        public TodoListController(IDownstreamWebApi downstreamWebApi, ITokenAcquisition tokenAcquisition)
        {
            _downstreamWebApi = downstreamWebApi;
            _tokenAcquisition = tokenAcquisition;
        }

        // GET: TodoList
        //[AuthorizeForScopes(ScopeKeySection = "TodoList:TodoListScope")]
        [AuthorizeForScopes(
            ScopeKeySection = "TodoList:Scopes", UserFlow = Susi)] // Must be the same user flow as used in `GetAccessTokenForUserAsync()`
        public async Task<ActionResult> Index()
        {
            var value = await _downstreamWebApi.GetForUserAsync<IEnumerable<Todo>>(ServiceName, "api/todolist");
            return View(value);
        }

        [AuthorizeForScopes(Scopes = new string[] { Scope }, UserFlow = Susi)] // Must be the same user flow as used in `GetAccessTokenForUserAsync()`
        public async Task<ActionResult> ClaimsSusi()
        {
            // We get a token, but we don't use it. It's only to trigger the user flow
            await _tokenAcquisition.GetAccessTokenForUserAsync(
                new string[] { Scope },
                userFlow: Susi);
            return View(Claims, null);
        }

        [AuthorizeForScopes(Scopes = new string[] { Scope }, UserFlow = EditProfile)] // Must be the same user flow as used in `GetAccessTokenForUserAsync()`
        public async Task<ActionResult> ClaimsEditProfile()
        {
            // We get a token, but we don't use it. It's only to trigger the user flow
            await _tokenAcquisition.GetAccessTokenForUserAsync(
                new string[] { Scope },
                userFlow: EditProfile);
            return View(Claims, null);
        }


        // GET: TodoList/Details/5
        public async Task<ActionResult> Details(int id)
        {
            var value = await _downstreamWebApi.GetForUserAsync<Todo>(
                ServiceName,
                $"api/todolist/{id}");
            return View(value);
        }

        // GET: TodoList/Create
        public ActionResult Create()
        {
            Todo todo = new Todo() { Owner = HttpContext.User.Identity.Name };
            return View(todo);
        }

        // POST: TodoList/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create([Bind("Title,Owner")] Todo todo)
        {
            await _downstreamWebApi.PostForUserAsync<Todo, Todo>(ServiceName, "api/todolist", todo);
            return RedirectToAction("Index");
        }

        // GET: TodoList/Edit/5
        public async Task<ActionResult> Edit(int id)
        {
            Todo todo = await _downstreamWebApi.GetForUserAsync<Todo>(
                 ServiceName,
                 $"api/todolist/{id}");

            if (todo == null)
            {
                return NotFound();
            }

            return View(todo);
        }

        // POST: TodoList/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(int id, [Bind("Id,Title,Owner")] Todo todo)
        {
            await _downstreamWebApi.CallWebApiForUserAsync<Todo, Todo>(
                ServiceName,
                todo,
                options =>
                {
                    options.HttpMethod = HttpMethod.Patch;
                    options.RelativePath = $"api/todolist/{todo.Id}";
                });
            return RedirectToAction("Index");
        }

        // GET: TodoList/Delete/5
        public async Task<ActionResult> Delete(int id)
        {
            Todo todo = await _downstreamWebApi.GetForUserAsync<Todo>(
                ServiceName,
                $"api/todolist/{id}");

            if (todo == null)
            {
                return NotFound();
            }

            return View(todo);
        }

        // POST: TodoList/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Delete(int id, [Bind("Id,Title,Owner")] Todo todo)
        {
            await _downstreamWebApi.CallWebApiForUserAsync(
                ServiceName,
                options =>
                {
                    options.HttpMethod = HttpMethod.Delete;
                    options.RelativePath = $"api/todolist/{id}";
                });
            return RedirectToAction("Index");
        }
    }
}

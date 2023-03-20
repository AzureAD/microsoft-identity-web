// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Abstractions;
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
        private readonly IDownstreamApi _downstreamApi;
        private const string ServiceName = "TodoList";
        private const string Scope = "https://fabrikamb2c.onmicrosoft.com/tasks/read";
        private const string Susi = "b2c_1_susi";
        private const string EditProfile = "b2c_1_edit_profile";
        private const string Claims = "Claims";


        public TodoListController(IDownstreamApi downstreamWebApi, ITokenAcquisition tokenAcquisition)
        {
            _downstreamApi = downstreamWebApi;
            _tokenAcquisition = tokenAcquisition;
        }

        // GET: TodoList
        //[AuthorizeForScopes(ScopeKeySection = "TodoList:TodoListScope")]
        [HttpGet]
        [AuthorizeForScopes(
            ScopeKeySection = "TodoList:Scopes", UserFlow = Susi)] // Must be the same user flow as used in `GetAccessTokenForUserAsync()`
        public async Task<ActionResult> Index()
        {
            var value = await _downstreamApi.GetForUserAsync<IEnumerable<Todo>>(ServiceName,
                options => options.RelativePath = "api/todolist");
            return View(value);
        }

        [HttpGet]
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
        [HttpGet]
        public async Task<ActionResult> Details(int id)
        {
            var value = await _downstreamApi.GetForUserAsync<Todo>(
                ServiceName,
                options => options.RelativePath = $"api/todolist/{id}");
            return View(value);
        }

        // GET: TodoList/Create
        [HttpGet]
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
            await _downstreamApi.PostForUserAsync<Todo, Todo>(ServiceName, todo, options => options.RelativePath = "api/todolist");
            return RedirectToAction("Index");
        }

        // GET: TodoList/Edit/5
        [HttpGet]
        public async Task<ActionResult> Edit(int id)
        {
            Todo todo = await _downstreamApi.GetForUserAsync<Todo>(
                 ServiceName,
                 options => options.RelativePath = $"api/todolist/{id}");

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
            await _downstreamApi.PatchForUserAsync<Todo, Todo>(
                ServiceName,
                todo,
                options =>
             {
                 options.RelativePath = $"api/todolist/{todo.Id}";
             });
            return RedirectToAction("Index");
        }

        // GET: TodoList/Delete/5
        [HttpGet]
        public async Task<ActionResult> Delete(int id)
        {
            Todo todo = await _downstreamApi.GetForUserAsync<Todo>(
                ServiceName,
                options => options.RelativePath = $"api/todolist/{id}");

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
            await _downstreamApi.DeleteForUserAsync(
                ServiceName,
                todo,
                options =>
             {
                 options.RelativePath = $"api/todolist/{id}";
             });
            return RedirectToAction("Index");
        }
    }
}

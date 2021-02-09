// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.Resource;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using TodoListService.Models;

namespace TodoListClient.Controllers
{
    [Authorize]
    [AuthorizeForScopes(ScopeKeySection = "TodoList:Scopes")]
    public class TodoListController : Controller
    {
        private IDownstreamWebApi _downstreamWebApi;
        private const string ServiceName = "TodoList";

        public TodoListController(IDownstreamWebApi downstreamWebApi)
        {
            _downstreamWebApi = downstreamWebApi;
        }

        // GET: TodoList
        public async Task<ActionResult> Index()
        {
            var value = await _downstreamWebApi.GetForUserAsync<IEnumerable<Todo>>(ServiceName, "api/todolist");

            return View(value);
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

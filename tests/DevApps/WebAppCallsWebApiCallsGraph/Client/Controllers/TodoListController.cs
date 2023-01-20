// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Web;
using System.Collections.Generic;
using System.Threading.Tasks;
using TodoListService.Models;

namespace TodoListClient.Controllers
{
    [Authorize]
    [AuthorizeForScopes(ScopeKeySection = "TodoList:Scopes")]
    public class TodoListController : Controller
    {
        private IDownstreamApi _downstreamApi;
        private const string ServiceName = "TodoList";

        public TodoListController(IDownstreamApi downstreamApi)
        {
            _downstreamApi = downstreamApi;
        }

        // GET: api/todolist
        public async Task<ActionResult> Index()
        {
            var value = await _downstreamApi.GetForUserAsync<IEnumerable<Todo>>(
                ServiceName,
                options => options.RelativePath = "api/todolist");

            return View(value);
        }

        // GET: api/todolist/5
        public async Task<ActionResult> Details(int id)
        {
            var value = await _downstreamApi.GetForUserAsync<Todo>(
                ServiceName,
                options => options.RelativePath = $"api/todolist/{id}");
            return View(value);
        }

        // Create and present to the user (no service call)
        public ActionResult Create()
        {
            Todo todo = new Todo() { Owner = HttpContext.User.Identity.Name };
            return View(todo);
        }

        // POST: api/todolist
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create([Bind("Title,Owner")] Todo todo)
        {
            await _downstreamApi.PostForUserAsync<Todo, Todo>(
                ServiceName,
                todo,
                options => options.RelativePath = "api/todolist");
            return RedirectToAction("Index");
        }

        // Get the content of the TODO of ID id to present it to the user for edition
        // GET: api/todolist/5
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

        // Patch: api/todolist/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(int id, [Bind("Id,Title,Owner")] Todo todo)
        {
            await _downstreamApi.PatchForUserAsync<Todo, Todo>(
                ServiceName,
                todo,
                options => options.RelativePath = $"api/todolist/{todo.Id}");
 
            return RedirectToAction("Index");
        }

        // Get the content of the TODO of ID to present it to the user for deletion
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
                options => options.RelativePath = $"api/todolist/{id}");
            return RedirectToAction("Index");
        }
    }
}

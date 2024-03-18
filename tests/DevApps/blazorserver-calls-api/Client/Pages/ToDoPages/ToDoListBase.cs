using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Http;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Web;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using ToDoListModel;

namespace blazorserver_client.Pages.ToDoPages
{

    public class ToDoListBase : ComponentBase
    {

        [Inject]
        MicrosoftIdentityConsentAndConditionalAccessHandler ConsentHandler { get; set; }

        [Inject]
        IDownstreamApi _downstreamApi { get; set; }

        protected IEnumerable<ToDo> toDoList = new List<ToDo>();

        protected ToDo toDo = new ToDo();

        protected override async Task OnInitializedAsync()
        {
            await GetToDoListService();
        }

        /// <summary>
        /// Gets all todo list items.
        /// </summary>
        /// <returns></returns>
        [AuthorizeForScopes(ScopeKeySection = "TodoList:TodoListScope")]
        private async Task GetToDoListService()
        {
            try
            {
                toDoList = await _downstreamApi.GetForUserAsync<IEnumerable<ToDo>>("TodoList");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);

                // Process the exception from a user challenge
                ConsentHandler.HandleException(ex);
            }
        }

        /// <summary>
        /// Deletes the selected item then retrieves the todo list.
        /// </summary>
        /// <param name="Id"></param>
        /// <returns></returns>
        protected async Task DeleteItem(int Id)
        {
            await _downstreamApi.DeleteForUserAsync<ToDo>(
                "TodoList", toDo,
                options => options.RelativePath = $"api/todolist/{Id}");
            await GetToDoListService();
        }
    }
}

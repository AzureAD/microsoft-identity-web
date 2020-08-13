// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Web;
using TodoListService.Models;

namespace TodoListClient.Services
{
    public static class TodoListServiceExtensions
    {
        public static MicrosoftIdentityAppCallsWebApiAuthenticationBuilder AddTodoListService(
            this MicrosoftIdentityAppCallsWebApiAuthenticationBuilder builder, 
            IConfiguration configuration)
        {
            builder.AddDownstreamApiService(TodoListService.ServiceName, configuration);
            // https://docs.microsoft.com/en-us/dotnet/standard/microservices-architecture/implement-resilient-applications/use-httpclientfactory-to-implement-resilient-http-requests
            builder.Services.AddHttpClient<ITodoListService, TodoListService>();
            return builder;
        }
    }

    /// <summary></summary>
    /// <seealso cref="TodoListClient.Services.ITodoListService" />
    public class TodoListService : ITodoListService
    {
        private readonly IDownstreamWebApi _downstreamWebApi;
        public const string ServiceName = "TodoList";

        public TodoListService(IDownstreamWebApi downstreamWebApi)
        {
            _downstreamWebApi = downstreamWebApi;
        }

        public async Task<Todo> AddAsync(Todo todo)
        {
            return await _downstreamWebApi.CallWebApiForUserAsync<Todo, Todo>(
                ServiceName,
                todo,
                options =>
                {
                    options.HttpMethod = HttpMethod.Post;
                    options.RelativePath = "api/todolist";
                });
        }

        public async Task DeleteAsync(int id)
        {
            await _downstreamWebApi.CallWebApiForUserAsync(
                ServiceName,
                options =>
                {
                    options.HttpMethod = HttpMethod.Delete;
                    options.RelativePath = $"api/todolist/{id}";
                });
        }

        public async Task<Todo> EditAsync(Todo todo)
        {
            return await _downstreamWebApi.CallWebApiForUserAsync<Todo, Todo>(
                ServiceName,
                todo,
                options =>
                {
                    options.HttpMethod = HttpMethod.Patch;
                    options.RelativePath = $"api/todolist/{todo.Id}";
                });
        }

        public async Task<IEnumerable<Todo>> GetAsync()
        {
            return await _downstreamWebApi.CallWebApiForUserAsync<object, IEnumerable <Todo>>(
                ServiceName,
                null,
                options =>
                {
                    options.RelativePath = $"api/todolist";
                });
        }

        public async Task<Todo> GetAsync(int id)
        {
            return await _downstreamWebApi.CallWebApiForUserAsync<object, Todo>(
                ServiceName,
                null,
                options =>
                {
                    options.HttpMethod = HttpMethod.Get;
                    options.RelativePath = $"api/todolist/{id}";
                });
        }
    }
}

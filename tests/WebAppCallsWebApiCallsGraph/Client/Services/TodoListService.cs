// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Web;
using TodoListService.Models;

namespace TodoListClient.Services
{
    public static class TodoListServiceExtensions
    {
        public static void AddTodoListService(this IServiceCollection services, IConfiguration configuration)
        {
            // https://docs.microsoft.com/en-us/dotnet/standard/microservices-architecture/implement-resilient-applications/use-httpclientfactory-to-implement-resilient-http-requests
            services.AddHttpClient<ITodoListService, TodoListService>();
        }
    }

    /// <summary></summary>
    /// <seealso cref="TodoListClient.Services.ITodoListService" />
    public class TodoListService : ITodoListService
    {
        private readonly IHttpContextAccessor _contextAccessor;
        private readonly HttpClient _httpClient;
        private readonly string _TodoListScope = string.Empty;
        private readonly string _TodoListBaseAddress = string.Empty;
        private readonly ITokenAcquisition _tokenAcquisition;
        private readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        public TodoListService(ITokenAcquisition tokenAcquisition, HttpClient httpClient, IConfiguration configuration, IHttpContextAccessor contextAccessor)
        {
            _httpClient = httpClient;
            _tokenAcquisition = tokenAcquisition;
            _contextAccessor = contextAccessor;
            _TodoListScope = configuration["TodoList:TodoListScope"];
            _TodoListBaseAddress = configuration["TodoList:TodoListBaseAddress"];
        }

        public async Task<Todo> AddAsync(Todo todo)
        {
            var httpRequestMessage = await PrepareAuthenticatedClient(
              $"{ _TodoListBaseAddress}/api/todolist",
              HttpMethod.Post);

            var jsonRequest = JsonSerializer.Serialize(todo);
            var jsoncontent = new StringContent(jsonRequest, Encoding.UTF8, "application/json");
            httpRequestMessage.Content = jsoncontent;

            var response = await _httpClient.SendAsync(httpRequestMessage);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                var content = await response.Content.ReadAsStringAsync();
                todo = JsonSerializer.Deserialize<Todo>(content, _jsonOptions);

                return todo;
            }

            throw new HttpRequestException($"Invalid status code in the HttpResponseMessage: {response.StatusCode}.");
        }

        public async Task DeleteAsync(int id)
        {
            var httpRequestMessage = await PrepareAuthenticatedClient(
               $"{ _TodoListBaseAddress}/api/todolist/{id}",
               HttpMethod.Delete);

            var response = await _httpClient.SendAsync(httpRequestMessage);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                return;
            }

            throw new HttpRequestException($"Invalid status code in the HttpResponseMessage: {response.StatusCode}.");
        }

        public async Task<Todo> EditAsync(Todo todo)
        {
            var httpRequestMessage = await PrepareAuthenticatedClient(
                $"{ _TodoListBaseAddress}/api/todolist/{todo.Id}", 
                HttpMethod.Patch);

            var jsonRequest = JsonSerializer.Serialize(todo);
            var jsoncontent = new StringContent(jsonRequest, Encoding.UTF8, "application/json-patch+json");

            httpRequestMessage.Content = jsoncontent;
            var response = await _httpClient.SendAsync(httpRequestMessage);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                var content = await response.Content.ReadAsStringAsync();
                todo = JsonSerializer.Deserialize<Todo>(content, _jsonOptions);

                return todo;
            }

            throw new HttpRequestException($"Invalid status code in the HttpResponseMessage: {response.StatusCode}.");
        }

        public async Task<IEnumerable<Todo>> GetAsync()
        {
            var httpRequestMessage = await PrepareAuthenticatedClient(
                $"{ _TodoListBaseAddress}/api/todolist",
                HttpMethod.Get);
            var response = await _httpClient.SendAsync(httpRequestMessage);
            var content = await response.Content.ReadAsStringAsync();   

            if (response.StatusCode == HttpStatusCode.OK)
            {
                IEnumerable<Todo> todolist = JsonSerializer.Deserialize<IEnumerable<Todo>>(content, _jsonOptions);
                return todolist;
            }
            throw new HttpRequestException($"Invalid status code in the HttpResponseMessage: {response.StatusCode}. Cause: {content}");
        }

        private async Task<HttpRequestMessage> PrepareAuthenticatedClient(
            string url, 
            HttpMethod httpMethod)
        {
            var accessToken = await _tokenAcquisition.GetAccessTokenForUserAsync(new[] { _TodoListScope });
            Debug.WriteLine($"access token-{accessToken}");
            HttpRequestMessage httpRequestMessage = new HttpRequestMessage(httpMethod, url);
            httpRequestMessage.Headers.Add("Authorization", $"bearer {accessToken}");
            httpRequestMessage.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            return httpRequestMessage;
        }

        public async Task<Todo> GetAsync(int id)
        {
            var httpRequestMessage = await PrepareAuthenticatedClient(
                $"{ _TodoListBaseAddress}/api/todolist/{id}",
                HttpMethod.Get);
            var response = await _httpClient.SendAsync(httpRequestMessage);
            if (response.StatusCode == HttpStatusCode.OK)
            {
                var content = await response.Content.ReadAsStringAsync();
                Todo todo = JsonSerializer.Deserialize<Todo>(content, _jsonOptions);

                return todo;
            }

            throw new HttpRequestException($"Invalid status code in the HttpResponseMessage: {response.StatusCode}.");
        }
    }
}

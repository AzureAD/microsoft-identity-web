// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Threading.Tasks;
using TodoListService.Models;

namespace TodoListClient.Services
{
    public interface ITodoListService
    {
        Task<IEnumerable<Todo>> GetAsync(string userFlow);

        Task<Todo> GetAsync(int id);

        Task DeleteAsync(int id);

        Task<Todo> AddAsync(Todo todo);

        Task<Todo> EditAsync(Todo todo);
    }
}

using System;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Http;

namespace TodoListService
{
    public interface ILongRunningProcessContextFactory
    {
        string CreateKey(HttpContext http);
        LongRunningProcessContext UseKey(HttpContext httpContext, string key);
    }
}

using System;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Http;

namespace TodoListService
{
    public class LongRunningProcessContext : IDisposable
    {
        JwtSecurityToken _savedToken;
        HttpContext _httpContext;

        internal LongRunningProcessContext(HttpContext httpContext, JwtSecurityToken tokenToUse)
        {
            _httpContext = httpContext;
            _savedToken = httpContext.Items["JwtSecurityTokenUsedToCallWebAPI"] as JwtSecurityToken;
            httpContext.Items["JwtSecurityTokenUsedToCallWebAPI"] = tokenToUse;
        }

        public void Dispose()
        {
            _httpContext.Items["JwtSecurityTokenUsedToCallWebAPI"] = _savedToken;
        }
    }
}

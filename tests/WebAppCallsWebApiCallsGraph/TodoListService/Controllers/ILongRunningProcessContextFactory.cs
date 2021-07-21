using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace TodoListService
{
    public interface ILongRunningProcessContextFactory
    {
        Task<string> CreateKey(HttpContext http, string keyHint = null);
        Task<LongRunningProcessContext> UseKey(HttpContext httpContext, string key);
    }
}

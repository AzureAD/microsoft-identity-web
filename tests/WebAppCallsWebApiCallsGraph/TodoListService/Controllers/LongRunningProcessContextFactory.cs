using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Distributed;

namespace TodoListService
{
    /// <summary>
    /// This is a naive implementation of a cache to save user assertions used
    /// to get an OBO token, in the case of long running processes which want to 
    /// call OBO and get the token refreshed.
    /// 
    /// TODO: document this better.
    /// Could use a IDistributed cache
    /// Needs to add 
    ///  services.AddSingleton<ILongRunningProcessAssertionCache, LongRunningProcessAssertionCache>();
    /// in the Startup.cs
    /// </summary>
    public class LongRunningProcessContextFactory : ILongRunningProcessContextFactory
    {
        /// <summary>
        /// Get a key associated with the current incoming token
        /// </summary>
        /// <param name="httpContext">Http context in which the controller action is running</param>
        /// <param name="keyHint">Hint for the key you want to use.</param>
        /// <returns>A unique string repesenting the incoming token to the web API. This
        /// key will be used in the future to retrieve the incoming token even if it has expired therefore
        /// enabling getting an OBO token.</returns>
        public async Task<string> CreateKey(HttpContext httpContext, string keyHint = null)
        {
            JwtSecurityToken token = httpContext.Items["JwtSecurityTokenUsedToCallWebAPI"] as JwtSecurityToken;
            string key = keyHint ?? Guid.NewGuid().ToString();

            IDistributedCache distributedCache = httpContext.RequestServices.GetService(typeof(IDistributedCache)) as IDistributedCache;
            if (distributedCache != null)
            {
                await distributedCache.SetStringAsync(key, token.RawData);
            }
            else
            {
                privateAssertionOfKey.TryAdd(key, token);
            }
            return key;
        }

        /// <summary>
        /// Get a long running process context from a key
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public async Task<LongRunningProcessContext> UseKey(HttpContext httpContext, string key)
        {

            IDistributedCache distributedCache = httpContext.RequestServices.GetService(typeof(IDistributedCache)) as IDistributedCache;
            JwtSecurityToken token;
            if (distributedCache != null)
            {
                string rawToken = await distributedCache.GetStringAsync(key);
                token = new JwtSecurityToken(rawToken);
            }
            else
            {
                token = privateAssertionOfKey[key];
            }
            return new LongRunningProcessContext(httpContext, token);
        }
        
        // Fallback: in memory
        private IDictionary<string, JwtSecurityToken> privateAssertionOfKey = new Dictionary<string, JwtSecurityToken>();
    }
}

## How to get an OBO token in an event handler or a long running process

### Principle

Sometimes your web API will do long running processes on behalf of the user (think of OneDrive which creates albums for you). To achieve that, the idea is :
1. Initiate the long running process by setting the `LongRunningWebApiSessionKey` property of `TokenAcquisitionOptions` while acquire a token with the user methods of `ITokenAcquisition` or `IDownstreamWebApi`. You can set it to:
  - TokenAcquisitionOptions.LongRunningWebApiSessionKeyAuto ("AllocateForMe") if you want Microsoft.Identity.Web to allocate a session key for you.
  - Your own string that you can associate with the user, or the request, or something else (like an identifier for Microsoft Graph web hooks).

2. When the call is done, and you have your token, if you had set `LongRunningWebApiSessionKey` to `TokenAcquisitionOptions.LongRunningWebApiSessionKeyAuto`, retrieve back the key provided by Microsoft.Identity.Web (really MSAL.NET), and store it for later.
 
3. Later (for instance when you are called back from a timer, of a web hook or ...), use the long running process key like in 1. to acquire a token. Token acquisitions achieved with a non-null long running process key will have a refresh token, that enables the web API to call downstream APIs even after the token used to call the web API has expired.


### Example

`Controllers/HomeController.cs`

```CSharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.Resource;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using TodoListService.Models;

namespace TodoListService.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [RequiredScope(RequiredScopesConfigurationKey = "AzureAd:Scopes")]
    public class TodoListController : Controller
    {
        private readonly ITokenAcquisition _tokenAcquisition; // do not remove
        // The web API will only accept tokens 1) for users, and 2) having the access_as_user scope for this API
        // In-memory TodoList
        private static readonly Dictionary<int, Todo> TodoStore = new Dictionary<int, Todo>();
  
        public TodoListController(
            IHttpContextAccessor contextAccessor,
            ITokenAcquisition tokenAcquisition)
        {
            _tokenAcquisition = tokenAcquisition;

            // Pre-populate with sample data
            if (TodoStore.Count == 0)
            {
                TodoStore.Add(1, new Todo() { Id = 1, Owner = $"{contextAccessor.HttpContext.User.Identity.Name}", Title = "Pick up groceries" });
                TodoStore.Add(2, new Todo() { Id = 2, Owner = $"{contextAccessor.HttpContext.User.Identity.Name}", Title = "Finish invoice report" });
            }
        }

        // GET: api/values
        // [RequiredScope("access_as_user")]
        [HttpGet]
        public async Task<IEnumerable<Todo>> GetAsync()
        {
            string owner = User.GetDisplayName();

            // Below is for testing multi-tenants (Normal OBO calls)
            var result = await _tokenAcquisition.GetAccessTokenForUserAsync(new string[] { "user.read" }).ConfigureAwait(false); // for testing OBO

            var result2 = await _tokenAcquisition.GetAccessTokenForUserAsync(new string[] { "user.read.all" },
                tokenAcquisitionOptions: new TokenAcquisitionOptions { ForceRefresh = true }).ConfigureAwait(false); // for testing OBO

            // Initiates a long running process
            RegisterPeriodicCallbackForLongProcessing();

            await Task.FromResult(0); // fix CS1998 while the lines about the 2 tokens are commented out.
            return TodoStore.Values.Where(x => x.Owner == owner);
        }

```

The content of `RegisterPeriodicCallbackForLongProcessing` is the following. It uses the factory to store the token, and pass-in a key to the long running process (here simulated by a timer)

```CSharp
    /// <summary>
        /// This methods the processing of user data where the web API periodically checks the user
        /// date (think of OneDrive producing albums)
        /// </summary>
        private async Task RegisterPeriodicCallbackForLongProcessing(string keyHint)
        {
            TokenAcquisitionOptions tokenAcquisitionOptions = new TokenAcquisitionOptions()
            {
                LongRunningWebApiSessionKey = keyHint ?? TokenAcquisitionOptions.LongRunningWebApiSessionKeyAuto
            };

            _= await _tokenAcquisition.GetAuthenticationResultForUserAsync(new string[] { "user.read" }, 
                    tokenAcquisitionOptions: tokenAcquisitionOptions);
            string key = tokenAcquisitionOptions.LongRunningWebApiSessionKey;

            // Build the URL to the callback controller, based on the request.
            var request = HttpContext.Request;
            string endpointPath = request.Path.Value.Replace("todolist", "callback", StringComparison.OrdinalIgnoreCase);
            string url = $"{request.Scheme}://{request.Host}{endpointPath}?key={key}";

            // Setup a timer so that the API calls back the callback every 10 mins.
            Timer timer = new Timer(async (state) =>
            {
                HttpClient httpClient = new HttpClient();
                
                var message = await httpClient.GetAsync(url);
            }, null, 1000, 1000 * 60 * 1);  // Callback every minute
        }
```


Here is the second controller: `Controllers/CallbackController.cs`, which uses the long running process key. in `ITokenAcquisition`, `IDownstreamWebApi` or the Graph service client to get a token even long after the initial token was acquired.

```CSharp
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Web;

namespace TodoListService.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    public class CallbackController : Controller
    {
        private readonly ITokenAcquisition _tokenAcquisition; 
        private ILogger _logger;

        public CallbackController(
            IHttpContextAccessor contextAccessor,
            ITokenAcquisition tokenAcquisition,
            ILogger<CallbackController> logger)
        {
            _tokenAcquisition = tokenAcquisition;
            _logger = logger;
        }


        [HttpGet]
        [AllowAnonymous]
        public async Task GetAsync(string key)
        {
            var request = HttpContext.Request;
            string calledUrl = request.Scheme + "://" + request.Host + request.Path.Value + Request.QueryString;

            _logger.LogWarning($"{DateTime.UtcNow}: {calledUrl}");

                TokenAcquisitionOptions tokenAcquisitionOptions = new TokenAcquisitionOptions()
                {
                    LongRunningWebApiSessionKey = key
                };
                var result = await _tokenAcquisition.GetAuthenticationResultForUserAsync(
                    new string[] { "user.read" }, tokenAcquisitionOptions: tokenAcquisitionOptions)
                    .ConfigureAwait(false); // for testing OBO

                _logger.LogWarning($"OBO token acquired from {result.AuthenticationResultMetadata.TokenSource} expires {result.ExpiresOn.UtcDateTime}");


                // For breakpoint
                if (result.AuthenticationResultMetadata.TokenSource == Microsoft.Identity.Client.TokenSource.IdentityProvider)
                {
                }

        }
    }
}
```

### Sample

For a full sample, see https://github.com/AzureAD/microsoft-identity-web/tree/master/tests/DevApps/WebAppCallsWebApiCallsGraph/TodoListService/Controllers
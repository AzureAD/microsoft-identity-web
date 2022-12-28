using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using Microsoft.Graph;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Web;
using Todo = OwinWebApi.Models.Todo;

namespace OwinWebApi.Controllers
{
    [Authorize]
    public class TodoListController : ApiController
    {
        private static readonly Dictionary<int, Todo> todoStore = new Dictionary<int, Todo>();

        // GET api/values
        public async Task<IEnumerable<Todo>> Get()
        {
            // EITHER - Example calling Graph
            // graphServiceClient won't be null if you added
            // services.AddMicrosoftGraph() in the Startup.auth.cs
            GraphServiceClient graphServiceClient = this.GetGraphServiceClient();
            var me = await graphServiceClient.Me.Request().GetAsync();

            // OR - Example calling a downstream directly with the IDownstreamRestApi helper (uses the
            // authorization header provider, encapsulates MSAL.NET)
            // downstreamRestApi won't be null if you added services.AddMicrosoftGraph()
            // in the Startup.auth.cs
            IDownstreamRestApi downstreamRestApi = this.GetDownstreamRestApi();
            var result = await downstreamRestApi.CallRestApiForUserAsync("DownstreamAPI");

            // OR - Get an authorization header (uses the token acquirer)
            IAuthorizationHeaderProvider authorizationHeaderProvider =
                    this.GetAuthorizationHeaderProvider();
            string authorizationHeader = await authorizationHeaderProvider.CreateAuthorizationHeaderForUserAsync(
                    new[] { "user.read" },
                    new AuthorizationHeaderProviderOptions
                    {
                        BaseUrl = "https://graph.microsoft.com/v1.0/me"
                    });

            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Add("Authorization", authorizationHeader);
            HttpResponseMessage response = await client.GetAsync("https://graph.microsoft.com/v1.0/me");

            // OR - Get a token if an SDK needs it (uses MSAL.NET)
            ITokenAcquirerFactory tokenAcquirerFactory = TokenAcquirerFactory.GetDefaultInstance();
            ITokenAcquirer acquirer = tokenAcquirerFactory.GetTokenAcquirer()!;
            AcquireTokenResult tokenResult = await acquirer.GetTokenForUserAsync(
               new[] { "user.read" });
            string accessToken = tokenResult.AccessToken!;

            // return the item
            string? owner = (HttpContext.Current.User as ClaimsPrincipal)?.GetDisplayName();
            return todoStore.Values.Where(x => x.Owner == owner);

        }

        [HttpGet]
        public Todo Get(int id)
        {
            return todoStore.Values.FirstOrDefault(t => t.Id == id);
        }

        // [RequiredScope("Weather.Write")]
        [HttpDelete]
        public void Delete(int id)
        {
            todoStore.Remove(id);
        }

        // POST api/values
        // [RequiredScope("access_as_user")]
        [HttpPost]
        public IHttpActionResult Post([FromBody] Todo todo)
        {
            var firstTodo = todoStore.Values.OrderByDescending(x => x.Id).FirstOrDefault();
            int id = firstTodo == null ? 0 : firstTodo.Id + 1;

            Todo todonew = new Todo() { Id = id, Owner = (HttpContext.Current.User as ClaimsPrincipal)?.GetDisplayName(), Title = todo.Title };
            todoStore.Add(id, todonew);

            return Ok(todo);
        }

        // PATCH api/values
        // [RequiredScope("access_as_user")]
        [HttpPatch]
        public IHttpActionResult Patch(int id, [FromBody] Todo todo)
        {
            if (id != todo.Id)
            {
                return NotFound();
            }

            if (todoStore.Values.FirstOrDefault(x => x.Id == id) == null)
            {
                return NotFound();
            }

            todoStore.Remove(id);
            todoStore.Add(id, todo);

            return Ok(todo);
        }

    }
}

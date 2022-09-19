using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using Microsoft.Graph;
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
            // Example calling Graph
            GraphServiceClient? graphServiceClient = this.GetGraphServiceClient();
            var me = await graphServiceClient?.Me?.Request()?.GetAsync();

            // Example getting a token to call a downstream web API
            ITokenAcquirer tokenAcquirer = this.GetTokenAcquirer();
            var result = await tokenAcquirer.GetTokenForUserAsync(new[] { "user.read" });

            // return the item
            string owner = (HttpContext.Current.User as ClaimsPrincipal)?.GetDisplayName();
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

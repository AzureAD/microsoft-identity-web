using System.Security.Claims;
using System.Threading.Tasks;
using System.Web.Mvc;
using Microsoft.Graph;
using Microsoft.Identity.Web;

namespace OwinWebApp.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        public async Task<ActionResult> Index()
        {
            GraphServiceClient graphServiceClient = HttpContext.GetGraphServiceClient();
            var me = await graphServiceClient.Me.Request().GetAsync();


            // Example getting a token to call a downstream web API
            ITokenAcquirer tokenAcquirer = TokenAcquirerFactory.GetDefaultInstance().GetTokenAcquirer();
            var result = await tokenAcquirer.GetTokenForUserAsync(new[] { "user.read" });

            // return the item
            string owner = (HttpContext.User as ClaimsPrincipal).GetDisplayName();
            ViewBag.Title = owner;
            return View();
        }

        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }
    }
}

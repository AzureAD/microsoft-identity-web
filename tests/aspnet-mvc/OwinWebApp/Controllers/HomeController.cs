using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Microsoft.Graph;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Client;
using Microsoft.Identity.Web;
using Microsoft.Owin.Security.OpenIdConnect;

namespace OwinWebApp.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        public async Task<ActionResult> Index()
        {
            try
            {
                GraphServiceClient graphServiceClient = this.GetGraphServiceClient();
                var me = await graphServiceClient.Me.Request().GetAsync();


                // Example getting a token to call a downstream web API
                ITokenAcquirer tokenAcquirer = TokenAcquirerFactory.GetDefaultInstance().GetTokenAcquirer();
                var result = await tokenAcquirer.GetTokenForUserAsync(new[] { "user.read" });

                // return the item
                string owner = (HttpContext.User as ClaimsPrincipal).GetDisplayName();
                ViewBag.Title = owner;
                return View();
            }
            catch (ServiceException graphEx) when (graphEx.InnerException is MicrosoftIdentityWebChallengeUserException)
            {
                HttpContext.GetOwinContext().Authentication.Challenge(OpenIdConnectAuthenticationDefaults.AuthenticationType);
                return View();
            }
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

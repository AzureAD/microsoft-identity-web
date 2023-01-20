using System.Net.Http;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Microsoft.Graph;
using Microsoft.Identity.Abstractions;
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
                // EITHER - Example calling Graph
                GraphServiceClient graphServiceClient = this.GetGraphServiceClient();
                var me = await graphServiceClient.Me?.Request().GetAsync();

                // OR - Example calling a downstream directly with the IDownstreamApi helper (uses the
                // authorization header provider, encapsulates MSAL.NET)
                IDownstreamApi downstreamApi = this.GetDownstreamApi();
                var result = await downstreamApi.CallApiForUserAsync("DownstreamAPI1");

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
                ITokenAcquirer acquirer = tokenAcquirerFactory.GetTokenAcquirer();
                AcquireTokenResult tokenResult = await acquirer.GetTokenForUserAsync(
                   new[] { "user.read" });
                string accessToken = tokenResult.AccessToken;
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

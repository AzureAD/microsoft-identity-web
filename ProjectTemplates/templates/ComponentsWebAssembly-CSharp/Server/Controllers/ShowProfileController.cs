using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Graph;
using Microsoft.Identity.Web.Resource;

namespace AspNetCoreMicrosoftIdentityWebProjectTemplates.templates.ComponentsWebAssembly_CSharp.Server.Controllers
{
    [Authorize]
    [ApiController]
    [Route("[controller]")]
    [RequiredScope("access_as_user", IsReusable = true)] // The web API will only accept tokens 1) for users, and 2) having the "access_as_user" scope for this API
    public class ShowProfileController : Controller
    {
        private readonly GraphServiceClient _graphServiceClient;

        public ShowProfileController(GraphServiceClient graphServiceClient)
        {
            _graphServiceClient = graphServiceClient;
        }

        [HttpGet]
        public async Task<string> Get()
        {
            var user = await _graphServiceClient.Me.Request().GetAsync();

            return user.DisplayName;
        }
    }
}

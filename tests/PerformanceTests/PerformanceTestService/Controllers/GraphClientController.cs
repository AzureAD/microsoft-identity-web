using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Graph;

namespace PerformanceTestService.Controllers
{
    [Route("[controller]")]
    [ApiController]
    [Authorize]
    public class GraphClientController : Controller
    {
        private readonly GraphServiceClient _graphServiceClient;

        public GraphClientController(GraphServiceClient graphServiceClient)
        {
            _graphServiceClient = graphServiceClient;
        }

        [HttpGet]
        public string Index()
        {
            return "Success.";
        }
    }
}

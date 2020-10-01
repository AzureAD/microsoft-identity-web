using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace PerformanceTestService.Controllers
{
    [Route("[controller]")]
    [ApiController]
    [Authorize]
    public class EmptyController : Controller
    {
        [HttpGet]
        public string Index()
        {
            return "Success.";
        }
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Web;

namespace PerformanceTestService.Controllers
{
    [Route("[controller]")]
    [ApiController]
    [Authorize]
    public class TokenAcquisitionController : Controller
    {
        private readonly ITokenAcquisition _tokenAcquisition;

        public TokenAcquisitionController(ITokenAcquisition tokenAcquisition)
        {
            _tokenAcquisition = tokenAcquisition;
        }

        [HttpGet]
        public string Index()
        {
            return "Success.";
        }
    }
}

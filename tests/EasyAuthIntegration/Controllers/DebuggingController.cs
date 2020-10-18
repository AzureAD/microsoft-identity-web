using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApplication1.Controllers
{
    [AllowAnonymous]
    public class DebuggingController : Controller
    {
        public IActionResult Index()
        {
            ViewData["HttpContext"] = HttpContext;
            return View();
        }
    }
}

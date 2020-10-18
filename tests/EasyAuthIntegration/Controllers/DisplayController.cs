using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApplication1.Controllers
{
    [AllowAnonymous]
    public class DisplayController : Controller
    {
        public IActionResult Index()
        {
            ViewData["HttpContext"] = HttpContext;
            return View();
        }
    }
}

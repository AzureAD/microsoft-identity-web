using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace IntegrationTestService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TestController : ControllerBase
    {
        public string Index()
        {
            var result = "Result";
            return result;
        }
        
        [Authorize]
        public string IndexAuth()
        {
            var result = "Result";
            return result;
        }
    }
}

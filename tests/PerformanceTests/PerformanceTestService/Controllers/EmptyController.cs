// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Web.Test.Common;

namespace PerformanceTestService.Controllers
{
    [Route("[controller]")]
    [ApiController]
    [Authorize]
    public class EmptyController : Controller
    {
        [HttpGet(TestConstants.EmptyGetEmpty)]
        public string GetEmpty()
        {
            return "Success.";
        }
    }
}

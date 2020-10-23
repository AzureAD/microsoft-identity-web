// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Graph;
using Microsoft.Identity.Web.Test.Common;

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

        [HttpGet(TestConstants.GraphClientGetEmpty)]
        public string GetEmpty()
        {
            return "Success.";
        }
    }
}

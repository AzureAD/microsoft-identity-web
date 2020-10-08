// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.Test.Common;

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

        [HttpGet(TestConstants.TokenAcquisitionGetEmpty)]
        public string GetEmpty()
        {
            return "Success.";
        }
    }
}

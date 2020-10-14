// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.Test.Common;

namespace PerformanceTestService.Controllers
{
    [ApiController]
    [Authorize]
    [Route("SecurePage")]
    public class WeatherForecastController : ControllerBase
    {
        private readonly ITokenAcquisition _tokenAcquisition;

        public WeatherForecastController(
            ITokenAcquisition tokenAcquisition)
        {
            _tokenAcquisition = tokenAcquisition;
        }

        [HttpGet(TestConstants.SecurePageGetEmpty)]
        public string GetEmpty()
        {
            return "Success.";
        }

        [HttpGet(TestConstants.SecurePageGetTokenForUserAsync)]
        public async Task<string> GetTokenForUserAsync()
        {
            return await _tokenAcquisition.GetAccessTokenForUserAsync(
                TestConstants.s_userReadScope).ConfigureAwait(false);
        }

        [HttpGet(TestConstants.SecurePageGetTokenForAppAsync)]
        public async Task<string> GetTokenForAppAsync()
        {
            return await _tokenAcquisition.GetAccessTokenForAppAsync(
                TestConstants.s_scopeForApp).ConfigureAwait(false);
        }
    }
}

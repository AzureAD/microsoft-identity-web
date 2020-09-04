// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Graph;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.Resource;
using Microsoft.Identity.Web.Test.Common;

namespace IntegrationTestService.Controllers
{
    [ApiController]
    [Microsoft.AspNetCore.Authorization.Authorize]
    [Route("SecurePage")]
    public class WeatherForecastController : ControllerBase
    {
        private IDownstreamWebApi _downstreamWebApi;
        private readonly ITokenAcquisition _tokenAcquisition;
        private readonly GraphServiceClient _graphServiceClient;
        // The Web API will only accept tokens 1) for users, and 2) having the access_as_user scope for this API
        static readonly string[] scopeRequiredByApi = new string[] { "user_impersonation" };

        public WeatherForecastController(
            IDownstreamWebApi downstreamWebApi,
            ITokenAcquisition tokenAcquisition,
            GraphServiceClient graphServiceClient)
        {
            _downstreamWebApi = downstreamWebApi;
            _tokenAcquisition = tokenAcquisition;
            _graphServiceClient = graphServiceClient;
        }

        [HttpGet(TestConstants.SecurePageGetTokenAsync)]
        public async Task<string> GetTokenAsync()
        {
            return await _tokenAcquisition.GetAccessTokenForUserAsync(
                TestConstants.s_userReadScope).ConfigureAwait(false);
        }

        [HttpGet(TestConstants.SecurePageCallDownstreamWebApi)]
        public async Task<HttpResponseMessage> CallDownstreamWebApiAsync()
        {
            HttpContext.VerifyUserHasAnyAcceptedScope(scopeRequiredByApi);
            return await _downstreamWebApi.CallWebApiForUserAsync(TestConstants.SectionNameCalledApi);
        }

        [HttpGet(TestConstants.SecurePageCallDownstreamWebApiGeneric)]
        public async Task<string> CallDownstreamWebApiGenericAsync()
        {
            HttpContext.VerifyUserHasAnyAcceptedScope(scopeRequiredByApi);
            var user = await _downstreamWebApi.CallWebApiForUserAsync<string, User>(
                TestConstants.SectionNameCalledApi,
                null,
                options => { options.RelativePath = "me"; });
            return user.DisplayName;
        }

        [HttpGet(TestConstants.SecurePageCallMicrosoftGraph)]
        public async Task<string> CallMicrosoftGraphAsync()
        {
            HttpContext.VerifyUserHasAnyAcceptedScope(scopeRequiredByApi);
            var user = await _graphServiceClient.Me.Request().GetAsync();
            return user.DisplayName;
        }
    }
}

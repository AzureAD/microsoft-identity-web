// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Graph;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.Resource;
using Microsoft.Identity.Web.Test.Common;

namespace IntegrationTestService.Controllers
{
    [ApiController]
    [Route("SecurePage")]
    [RequiredScope("user_impersonation")]
    public class WeatherForecastController : ControllerBase
    {
        private readonly IDownstreamRestApi _downstreamWebApi;
        private readonly ITokenAcquisition _tokenAcquisition;
        private readonly GraphServiceClient _graphServiceClient;

        public WeatherForecastController(
            IDownstreamRestApi downstreamWebApi,
            ITokenAcquisition tokenAcquisition,
            GraphServiceClient graphServiceClient)
        {
            _downstreamWebApi = downstreamWebApi;
            _tokenAcquisition = tokenAcquisition;
            _graphServiceClient = graphServiceClient;
        }

        [HttpGet(TestConstants.SecurePageGetTokenForUserAsync)]
        public async Task<string> GetTokenAsync()
        {
            return await _tokenAcquisition.GetAccessTokenForUserAsync(
                TestConstants.s_userReadScope).ConfigureAwait(false);
        }

        [HttpGet(TestConstants.SecurePageCallDownstreamWebApi)]
        public async Task<HttpResponseMessage> CallDownstreamWebApiAsync()
        {
            return await _downstreamWebApi.CallRestApiForUserAsync(
            TestConstants.SectionNameCalledApi);
        }

        [HttpGet(TestConstants.SecurePageCallDownstreamWebApiGeneric)]
        public async Task<string?> CallDownstreamWebApiGenericAsync()
        {
            var user = await _downstreamWebApi.GetForUserAsync<UserInfo>(
                TestConstants.SectionNameCalledApi,
                options =>
                 {
                     options.RelativePath = "me";
                 });
            return user?.DisplayName;
        }

        [HttpGet(TestConstants.SecurePageCallMicrosoftGraph)]
        public async Task<string> CallMicrosoftGraphAsync()
        {
            var user = await _graphServiceClient.Me.Request().GetAsync();
            return user.DisplayName;
        }

        [HttpGet(TestConstants.SecurePageCallDownstreamWebApiGenericWithTokenAcquisitionOptions)]
        public async Task<string?> CallDownstreamWebApiGenericWithTokenAcquisitionOptionsAsync()
        {
            var user = await _downstreamWebApi.GetForUserAsync<UserInfo>(
                TestConstants.SectionNameCalledApi,
                options =>
                 {
                     options.RelativePath = "me";
                     options.AcquireTokenOptions.CorrelationId = TestConstants.s_correlationId;
                    /*options.TokenAcquisitionOptions.ExtraQueryParameters = new Dictionary<string, string>()
                    { { "slice", "testslice" } };*/ // doesn't work w/build automation
                    options.AcquireTokenOptions.ForceRefresh = true;
                 });
            return user?.DisplayName;
        }
    }
}

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Net.Http;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Graph;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.Resource;
using Microsoft.Identity.Web.Test.Common;

namespace IntegrationTestService.Controllers
{
    [ApiController]
    [Authorize(AuthenticationSchemes = TestConstants.CustomJwtScheme2)]
    [Route("SecurePage2")]
    [RequiredScope("user_impersonation")]
    public partial class WeatherForecast2Controller : ControllerBase
    {
        private readonly IDownstreamApi _downstreamApi;
        private readonly ITokenAcquisition _tokenAcquisition;
        private readonly GraphServiceClient _graphServiceClient;

        public WeatherForecast2Controller(
            IDownstreamApi downstreamApi,
            ITokenAcquisition tokenAcquisition,
            GraphServiceClient graphServiceClient)
        {
            _downstreamApi = downstreamApi;
            _tokenAcquisition = tokenAcquisition;
            _graphServiceClient = graphServiceClient;
        }

        [HttpGet(TestConstants.SecurePage2GetTokenForUserAsync)]
        public async Task<string> GetTokenAsync()
        {
            return await _tokenAcquisition.GetAccessTokenForUserAsync(
                TestConstants.s_userReadScope,
                authenticationScheme: TestConstants.CustomJwtScheme2).ConfigureAwait(false);
        }

        [HttpGet(TestConstants.SecurePage2CallDownstreamWebApi)]
        public async Task<HttpResponseMessage> CallDownstreamWebApiAsync()
        {
            return await _downstreamApi.CallApiForUserAsync(
                TestConstants.SectionNameCalledApi,
                options => options.AcquireTokenOptions.AuthenticationOptionsName = TestConstants.CustomJwtScheme2);
        }

        [HttpGet(TestConstants.SecurePage2CallDownstreamWebApiGeneric)]
        public async Task<string?> CallDownstreamWebApiGenericAsync()
        {
            var user = await _downstreamApi.GetForUserAsync<UserInfo>(
                TestConstants.SectionNameCalledApi,
                options =>
                {
                    options.RelativePath = "me";
                    options.AcquireTokenOptions.AuthenticationOptionsName = TestConstants.CustomJwtScheme2;
                });
            return user?.DisplayName;
        }

        [JsonSerializable(typeof(UserInfo))]
        internal partial class UserInfoJsonContext : JsonSerializerContext
        {
        }

#if NET8_0_OR_GREATER
        [HttpGet(TestConstants.SecurePage2CallDownstreamWebApiGenericAotInternal)]
        public async Task<string?> CallDownstreamWebApiGenericAsyncAotInternalAsync()
        {
            var user = await _downstreamApi.GetForUserAsync<UserInfo>(
                TestConstants.SectionNameCalledApi,
                UserInfoJsonContext.Default.UserInfo,
                options =>
                {
                    options.RelativePath = "me";
                    options.AcquireTokenOptions.AuthenticationOptionsName = TestConstants.CustomJwtScheme2;
                });
            return user?.DisplayName;
        }
#endif

        [HttpGet(TestConstants.SecurePage2CallMicrosoftGraph)]
        public async Task<string> CallMicrosoftGraphAsync()
        {
            var user = await _graphServiceClient.Me.Request()
                .WithAuthenticationScheme(TestConstants.CustomJwtScheme2).GetAsync();
            return user.DisplayName;
        }

        [HttpGet(TestConstants.SecurePage2CallDownstreamWebApiGenericWithTokenAcquisitionOptions)]
        public async Task<string?> CallDownstreamWebApiGenericWithTokenAcquisitionOptionsAsync()
        {
            var user = await _downstreamApi.GetForUserAsync<UserInfo>(
                TestConstants.SectionNameCalledApi,
                options =>
                {
                    options.RelativePath = "me";
                    options.AcquireTokenOptions.AuthenticationOptionsName = TestConstants.CustomJwtScheme2;
                    options.AcquireTokenOptions.CorrelationId = TestConstants.s_correlationId;
                    /*options.TokenAcquisitionOptions.ExtraQueryParameters = new Dictionary<string, string>()
                    { { "slice", "testslice" } };*/ // doesn't work w/build automation
                    options.AcquireTokenOptions.ForceRefresh = true;
                });
            return user?.DisplayName;
        }
    }
}

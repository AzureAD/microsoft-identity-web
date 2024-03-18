// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.CookiePolicy;
using Microsoft.AspNetCore.Http;
using Microsoft.Identity.Web.Test.Common.TestHelpers;
using Xunit;

namespace Microsoft.Identity.Web.Test
{
    public class CookiePolicyOptionsExtensionsTests
    {
        private string _cookieName = "cookieName";
        private string _cookieValue = "cookieValue";
        private CookiePolicyOptions _cookiePolicyOptions;
        private HttpContext _httpContext;

        public CookiePolicyOptionsExtensionsTests()
        {
            _cookiePolicyOptions = new CookiePolicyOptions()
            {
                MinimumSameSitePolicy = SameSiteMode.Strict,
            };
            _httpContext = HttpContextUtilities.CreateHttpContext();
        }

        [Theory]
        [InlineData(SameSiteMode.None, SameSiteMode.Unspecified, "Mozilla/5.0 (iPhone; CPU iPhone OS 12_2 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Mobile/15E148")] // Allow SameSite None
        [InlineData(SameSiteMode.None, SameSiteMode.None, "Mozilla/5.0 (iPhone; CPU iPhone OS 13_1_2 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/13.0.1 Mobile/15E148 Safari/604.1")] // Disallow SameSite None
        [InlineData(SameSiteMode.Strict, SameSiteMode.Strict, "Mozilla/5.0 (iPhone; CPU iPhone OS 12_2 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Mobile/15E148")]
        public void HandleSameSiteCookieCompatibility_Default_ExecutesSuccessfully(SameSiteMode initialSameSiteMode, SameSiteMode expectedSameSiteMode, string userAgent)
        {
            _httpContext.Request.Headers.Append(Constants.UserAgent, userAgent);
            var appendCookieOptions = new CookieOptions() { SameSite = initialSameSiteMode };
            var deleteCookieOptions = new CookieOptions() { SameSite = initialSameSiteMode };
            var appendCookieContext = new AppendCookieContext(_httpContext, appendCookieOptions, _cookieName, _cookieValue);
            var deleteCookieContext = new DeleteCookieContext(_httpContext, deleteCookieOptions, _cookieName);

            _cookiePolicyOptions.HandleSameSiteCookieCompatibility();

            Assert.Equal(SameSiteMode.Unspecified, _cookiePolicyOptions.MinimumSameSitePolicy);
            Assert.NotNull(_cookiePolicyOptions);
            Assert.NotNull(_cookiePolicyOptions.OnAppendCookie);
            _cookiePolicyOptions.OnAppendCookie(appendCookieContext);
            Assert.Equal(expectedSameSiteMode, appendCookieOptions.SameSite);

            Assert.NotNull(_cookiePolicyOptions.OnDeleteCookie);
            _cookiePolicyOptions.OnDeleteCookie(deleteCookieContext);
            Assert.Equal(expectedSameSiteMode, deleteCookieOptions.SameSite);
        }

        [Theory]
        [InlineData(SameSiteMode.None, SameSiteMode.Unspecified, true, "Mozilla/5.0 (iPhone; CPU iPhone OS 12_2 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Mobile/15E148")] // Allow SameSite None
        [InlineData(SameSiteMode.None, SameSiteMode.None, true, "Mozilla/5.0 (iPhone; CPU iPhone OS 13_1_2 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/13.0.1 Mobile/15E148 Safari/604.1")] // Disallow SameSite None
        [InlineData(SameSiteMode.Strict, SameSiteMode.Strict, false, "Mozilla/5.0 (iPhone; CPU iPhone OS 12_2 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Mobile/15E148")]
        public void HandleSameSiteCookieCompatibility_CustomFilter_ExecutesSuccessfully(SameSiteMode initialSameSiteMode, SameSiteMode expectedSameSiteMode, bool expectedEventCalled, string userAgent)
        {
            _httpContext.Request.Headers.Append(Constants.UserAgent, userAgent);
            var appendCookieOptions = new CookieOptions() { SameSite = initialSameSiteMode };
            var deleteCookieOptions = new CookieOptions() { SameSite = initialSameSiteMode };
            var appendCookieContext = new AppendCookieContext(_httpContext, appendCookieOptions, _cookieName, _cookieValue);
            var deleteCookieContext = new DeleteCookieContext(_httpContext, deleteCookieOptions, _cookieName);
            var appendEventCalled = false;
            var deleteEventCalled = false;

            _cookiePolicyOptions.HandleSameSiteCookieCompatibility((userAgent) =>
            {
                appendEventCalled = true;
                return CookiePolicyOptionsExtensions.DisallowsSameSiteNone(userAgent);
            });

            Assert.Equal(SameSiteMode.Unspecified, _cookiePolicyOptions.MinimumSameSitePolicy);

            Assert.NotNull(_cookiePolicyOptions.OnAppendCookie);
            _cookiePolicyOptions.OnAppendCookie(appendCookieContext);
            Assert.Equal(expectedSameSiteMode, appendCookieOptions.SameSite);
            Assert.Equal(expectedEventCalled, appendEventCalled);

            _cookiePolicyOptions.HandleSameSiteCookieCompatibility((userAgent) =>
            {
                deleteEventCalled = true;
                return CookiePolicyOptionsExtensions.DisallowsSameSiteNone(userAgent);
            });

            Assert.NotNull(_cookiePolicyOptions.OnDeleteCookie);
            _cookiePolicyOptions.OnDeleteCookie(deleteCookieContext);
            Assert.Equal(expectedSameSiteMode, deleteCookieOptions.SameSite);
            Assert.Equal(expectedEventCalled, deleteEventCalled);
        }

        [Theory]
        [InlineData(true, "Mozilla/5.0 (iPhone; CPU iPhone OS 12_2 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Mobile/15E148")]
        [InlineData(true, "Mozilla/5.0 (iPhone; CPU iPhone OS 12_2 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/12.1 Mobile/15E148 Safari/604.1")]
        [InlineData(true, "Mozilla/5.0 (iPad; CPU OS 12_2 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Mobile/15E148")]
        [InlineData(true, "Mozilla/5.0 (iPad; CPU OS 12_2 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/12.1 Mobile/15E148 Safari/604.1")]
        [InlineData(true, "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_14_6) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/13.0.3 Safari/605.1.15")]
        [InlineData(true, "Mozilla/5.0 (Windows NT 6.1; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/60.0.3112.90 Safari/537.36")]
        [InlineData(true, "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/58.0.3029.110 Safari/537.36")]
        [InlineData(true, "Mozilla/5.0 (Linux; U; Android 6.0.1; zh-CN; F5121 Build/34.0.A.1.247) AppleWebKit/537.36 (KHTML, like Gecko) Version/4.0 Chrome/40.0.2214.89 UCBrowser/11.5.1.944 Mobile Safari/537.36")]
        [InlineData(true, "Mozilla/5.0 (Linux; U; Android 6.0; zh-CN; Redmi Note 4 Build/MRA58K) AppleWebKit/537.36 (KHTML, like Gecko) Version/4.0 Chrome/57.0.2987.108 UCBrowser/12.8.2.1062 Mobile Safari/537.36")]
        [InlineData(true, "Mozilla/5.0 (Linux; U; Android 6.0; en-US; CAM-UL00 Build/HONORCAM-UL00) AppleWebKit/537.36 (KHTML, like Gecko) Version/4.0 Chrome/57.0.2987.108 UCBrowser/12.13.1.1189 Mobile Safari/537.36")]
        [InlineData(false, "Mozilla/5.0 (iPhone; CPU iPhone OS 13_1_2 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/13.0.1 Mobile/15E148 Safari/604.1")]
        [InlineData(false, "Mozilla/5.0 (iPhone; CPU iPhone OS 11_4 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/11.0 Mobile/15E148 Safari/604.1")]
        [InlineData(false, "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/80.0.3987.149 Safari/537.36")]
        [InlineData(false, "Mozilla/5.0 (iPhone; CPU iPhone OS 13_2 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) CriOS/81.0.4044.69 Mobile/15E148 Safari/604.1")]
        [InlineData(false, "Mozilla/5.0 (iPad; CPU OS 13_2 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) CriOS/81.0.4044.69 Mobile/15E148 Safari/604.1")]
        [InlineData(false, "Mozilla/5.0 (iPad; CPU iPhone OS 13_2_3 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/13.0.3 Mobile/15E148 Safari/604.1")]
        [InlineData(false, "Mozilla/5.0 (iPhone; CPU iPhone OS 13_2_3 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/13.0.3 Mobile/15E148 Safari/604.1")]
        [InlineData(false, "Mozilla/5.0 (Windows NT 6.2; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/42.0.2311.90 Safari/537.36")]
        [InlineData(false, "Mozilla/5.0 (Linux; U; Android 7.0; en-US; Redmi Note 4 Build/NRD90M) AppleWebKit/537.36 (KHTML, like Gecko) Version/4.0 Chrome/57.0.2987.108 UCBrowser/13.1.2.1293 Mobile Safari/537.36")]
        [InlineData(false, "Mozilla/5.0 (Linux; U; Android 6.0; en-US; CAM-UL00 Build/HONORCAM-UL00) AppleWebKit/537.36 (KHTML, like Gecko) Version/4.0 Chrome/57.0.2987.108 UCBrowser/12.14.5.1189 Mobile Safari/537.36")]
        [InlineData(false, "Mozilla/5.0 (Linux; U; Android 6.0; en-US; CAM-UL00 Build/HONORCAM-UL00) AppleWebKit/537.36 (KHTML, like Gecko) Version/4.0 Chrome/57.0.2987.108 UCBrowser/12.13.3.1189 Mobile Safari/537.36")]
        [InlineData(false, "Invalid user agent")]
        public void DisallowsSameSiteNone_VariousUserAgents_ExecutesSuccessfully(bool expectedResult, string userAgent)
        {
            var actualResult = CookiePolicyOptionsExtensions.DisallowsSameSiteNone(userAgent);

            Assert.Equal(expectedResult, actualResult);
        }
    }
}

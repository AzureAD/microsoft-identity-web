// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Http;
using NSubstitute;

namespace Microsoft.Identity.Web.Test.Common.Mocks
{
    public static class MockHttpContextAccessor
    {
        public static IHttpContextAccessor CreateMockHttpContextAccessor()
        {
            var mockHttpContextAccessor = Substitute.For<IHttpContextAccessor>();
            mockHttpContextAccessor.HttpContext = new DefaultHttpContext();
            mockHttpContextAccessor.HttpContext.Request.Scheme = "https";
            mockHttpContextAccessor.HttpContext.Request.Host = new HostString("IdentityDotNetSDKAutomation");
            mockHttpContextAccessor.HttpContext.Request.PathBase = "/";

            return mockHttpContextAccessor;
        }
    }
}

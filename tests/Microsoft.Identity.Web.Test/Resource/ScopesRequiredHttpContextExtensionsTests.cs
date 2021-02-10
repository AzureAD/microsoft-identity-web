// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.Identity.Web.Resource;
using Microsoft.Identity.Web.Test.Common.TestHelpers;
using Xunit;

namespace Microsoft.Identity.Web.Test.Resource
{
    public class ScopesRequiredHttpContextExtensionsTests
    {
        [Fact]
        [Obsolete("Method is obsolete")]
        public void VerifyUserHasAnyAcceptedScope_NullParameters_ThrowsException()
        {
            HttpContext httpContext = null;
            Assert.Throws<ArgumentNullException>(() => httpContext.VerifyUserHasAnyAcceptedScope(string.Empty));

            httpContext = HttpContextUtilities.CreateHttpContext();

            Assert.Throws<ArgumentNullException>("acceptedScopes", () => httpContext.VerifyUserHasAnyAcceptedScope(null));
        }

        [Fact]
        [Obsolete("Method is obsolete")]
        public void VerifyUserHasAnyAcceptedScope_NoClaims_ThrowsException()
        {
            var acceptedScopes = new[] { "acceptedScope1", "acceptedScope2" };
            var expectedStatusCode = (int)HttpStatusCode.Unauthorized;

            var httpContext = HttpContextUtilities.CreateHttpContext();
            Assert.Throws<UnauthorizedAccessException>(() => httpContext.VerifyUserHasAnyAcceptedScope(acceptedScopes));

            Assert.Equal(expectedStatusCode, httpContext.Response.StatusCode);
        }

        [Fact]
        [Obsolete("Method is obsolete")]
        public void VerifyUserHasAnyAcceptedScope_NoAcceptedScopes_ThrowsException()
        {
            var acceptedScopes = new[] { "acceptedScope1", "acceptedScope2" };
            var actualScopes = new[] { "acceptedScope3", "acceptedScope4" };
            var expectedStatusCode = (int)HttpStatusCode.Forbidden;

            var httpContext = HttpContextUtilities.CreateHttpContext(actualScopes, new string[] { });
            Assert.Throws<UnauthorizedAccessException>(() => httpContext.VerifyUserHasAnyAcceptedScope(acceptedScopes));

            Assert.Equal(expectedStatusCode, httpContext.Response.StatusCode);
        }

        [Fact]
        [Obsolete("Method is obsolete")]
        public void VerifyUserHasAnyAcceptedScope_MatchesAcceptedScopes_ExecutesSuccessfully()
        {
            var httpContext = HttpContextUtilities.CreateHttpContext(new[] { "acceptedScope1" }, new string[] { });
            httpContext.VerifyUserHasAnyAcceptedScope("acceptedScope1");

            httpContext = HttpContextUtilities.CreateHttpContext(new[] { "acceptedScope1 acceptedScope2" }, new string[] { });
            httpContext.VerifyUserHasAnyAcceptedScope("acceptedScope2");

            httpContext = HttpContextUtilities.CreateHttpContext(new[] { "acceptedScope2" }, new string[] { });
            httpContext.VerifyUserHasAnyAcceptedScope("acceptedScope1", "acceptedScope2");

            httpContext = HttpContextUtilities.CreateHttpContext(new[] { "acceptedScope2 acceptedScope1" }, new string[] { });
            httpContext.VerifyUserHasAnyAcceptedScope("acceptedScope1", "acceptedScope2");
        }
    }
}

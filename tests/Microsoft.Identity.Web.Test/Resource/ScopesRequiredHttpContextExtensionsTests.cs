// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Net;
using System.Net.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Identity.Web.Resource;
using Microsoft.Identity.Web.Test.Common.TestHelpers;
using Xunit;

namespace Microsoft.Identity.Web.Test.Resource
{
    public class ScopesRequiredHttpContextExtensionsTests
    {
        [Fact]
        public void VerifyUserHasAnyAcceptedScope_NullParameters_ThrowsException()
        {
            HttpContext httpContext = null;

            Assert.Throws<NullReferenceException>(() => httpContext.VerifyUserHasAnyAcceptedScope(string.Empty));

            httpContext = HttpContextUtilities.CreateHttpContext();

            Assert.Throws<ArgumentNullException>("acceptedScopes", () => httpContext.VerifyUserHasAnyAcceptedScope(null));
        }

        [Fact]
        public void VerifyUserHasAnyAcceptedScope_NoClaims_ThrowsException()
        {
            var acceptedScopes = new[] { "acceptedScope1", "acceptedScope2" };
            var expectedErrorMessage = $"The 'scope' claim does not contain scopes '{string.Join(",", acceptedScopes)}' or was not found";
            var expectedStatusCode = (int)HttpStatusCode.Unauthorized;

            var httpContext = HttpContextUtilities.CreateHttpContext();

            var exception = Assert.Throws<HttpRequestException>(() => httpContext.VerifyUserHasAnyAcceptedScope(acceptedScopes));
            Assert.Equal(expectedStatusCode, httpContext.Response.StatusCode);
            Assert.Equal(expectedErrorMessage, exception.Message);
        }

        [Fact]
        public void VerifyUserHasAnyAcceptedScope_NoAcceptedScopes_ThrowsException()
        {
            var acceptedScopes = new[] { "acceptedScope1", "acceptedScope2" };
            var actualScopes = new[] { "acceptedScope3", "acceptedScope4" };
            var expectedErrorMessage = $"The 'scope' claim does not contain scopes '{string.Join(",", acceptedScopes)}' or was not found";
            var expectedStatusCode = (int)HttpStatusCode.Unauthorized;

            var httpContext = HttpContextUtilities.CreateHttpContext(actualScopes);

            var exception = Assert.Throws<HttpRequestException>(() => httpContext.VerifyUserHasAnyAcceptedScope(acceptedScopes));
            Assert.Equal(expectedStatusCode, httpContext.Response.StatusCode);
            Assert.Equal(expectedErrorMessage, exception.Message);

            httpContext = HttpContextUtilities.CreateHttpContext(new[] { "acceptedScope3", "acceptedScope4" });

            exception = Assert.Throws<HttpRequestException>(() => httpContext.VerifyUserHasAnyAcceptedScope(acceptedScopes));
            Assert.Equal(expectedStatusCode, httpContext.Response.StatusCode);
            Assert.Equal(expectedErrorMessage, exception.Message);
        }

        [Fact]
        public void VerifyUserHasAnyAcceptedScope_MatchesAcceptedScopes_ExecutesSuccessfully()
        {
            var httpContext = HttpContextUtilities.CreateHttpContext(new[] { "acceptedScope1" });
            httpContext.VerifyUserHasAnyAcceptedScope("acceptedScope1");

            httpContext = HttpContextUtilities.CreateHttpContext(new[] { "acceptedScope1 acceptedScope2" });
            httpContext.VerifyUserHasAnyAcceptedScope("acceptedScope2");

            httpContext = HttpContextUtilities.CreateHttpContext(new[] { "acceptedScope2" });
            httpContext.VerifyUserHasAnyAcceptedScope("acceptedScope1", "acceptedScope2");

            httpContext = HttpContextUtilities.CreateHttpContext(new[] { "acceptedScope2 acceptedScope1" });
            httpContext.VerifyUserHasAnyAcceptedScope("acceptedScope1", "acceptedScope2");
        }
    }
}

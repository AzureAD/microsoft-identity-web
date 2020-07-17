// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Globalization;
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
        public void VerifyUserHasAnyAcceptedScope_NullParameters_ThrowsException()
        {
            HttpContext httpContext = null;

            Assert.Throws<ArgumentNullException>(() => httpContext.VerifyUserHasAnyAcceptedScope(string.Empty));

            httpContext = HttpContextUtilities.CreateHttpContext();

            Assert.Throws<ArgumentNullException>("acceptedScopes", () => httpContext.VerifyUserHasAnyAcceptedScope(null));
        }

        [Fact]
        public void VerifyUserHasAnyAcceptedScope_NoClaims_ThrowsException()
        {
            var acceptedScopes = new[] { "acceptedScope1", "acceptedScope2" };
            var expectedStatusCode = (int)HttpStatusCode.Unauthorized;

            var httpContext = HttpContextUtilities.CreateHttpContext();
            httpContext.VerifyUserHasAnyAcceptedScope(acceptedScopes);

            HttpResponse response = httpContext.Response;
            Assert.Equal(expectedStatusCode, response.StatusCode);
        }

        [Fact]
        public void VerifyUserHasAnyAcceptedScope_NoAcceptedScopes_ThrowsException()
        {
            var acceptedScopes = new[] { "acceptedScope1", "acceptedScope2" };
            var actualScopes = new[] { "acceptedScope3", "acceptedScope4" };
            var expectedErrorMessage = string.Format(CultureInfo.InvariantCulture, IDWebErrorMessage.MissingScopes, string.Join(",", acceptedScopes));
            var expectedStatusCode = (int)HttpStatusCode.Forbidden;

            var httpContext = HttpContextUtilities.CreateHttpContext(actualScopes);
            httpContext.VerifyUserHasAnyAcceptedScope(acceptedScopes);

            HttpResponse response = httpContext.Response;
            Assert.Equal(expectedStatusCode, response.StatusCode);
            Assert.Equal(expectedErrorMessage, GetBody(response));

            httpContext = HttpContextUtilities.CreateHttpContext(new[] { "acceptedScope3", "acceptedScope4" });
            httpContext.VerifyUserHasAnyAcceptedScope(acceptedScopes);
            response = httpContext.Response;
            Assert.Equal(expectedStatusCode, response.StatusCode);
            Assert.Equal(expectedErrorMessage, GetBody(response));
        }

        private static string GetBody(HttpResponse response)
        {
            byte[] buffer = new byte[response.Body.Length];
            response.Body.Seek(0, System.IO.SeekOrigin.Begin);
            response.Body.Read(buffer, 0, buffer.Length);
            string body = System.Text.Encoding.Default.GetString(buffer);
            return body;
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

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Globalization;
using System.Net;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Identity.Web.Resource;
using Microsoft.Identity.Web.Test.Common.TestHelpers;
using Xunit;

namespace Microsoft.Identity.Web.Test.Resource
{
    public class RolesRequiredHttpContextExtensionsTests
    {
        [Fact]
        public void VerifyUserHasAnyAcceptedScope_NullParameters_ThrowsException()
        {
            HttpContext httpContext = null;

            Assert.Throws<ArgumentNullException>(() => httpContext.ValidateAppRole(string.Empty));

            httpContext = HttpContextUtilities.CreateHttpContext();

            Assert.Throws<ArgumentNullException>("acceptedRoles", () => httpContext.ValidateAppRole(null));
        }

        [Fact]
        public void VerifyAppHasAnyAcceptedRoles_NoClaims_ThrowsException()
        {
            var acceptedRoles = new[] { "access_as_application", "access_as_application_for_write" };
            var expectedStatusCode = (int)HttpStatusCode.Unauthorized;

            var httpContext = HttpContextUtilities.CreateHttpContext();
            httpContext.ValidateAppRole(acceptedRoles);

            HttpResponse response = httpContext.Response;
            Assert.Equal(expectedStatusCode, response.StatusCode);
        }

        [Fact]
        public void VerifyAppHasAnyAcceptedRole_NoAcceptedRoles_ThrowsException()
        {
            var acceptedRoles = new[] { "access_as_application", "access_as_application_for_write" };
            var actualRoles = new[] { "access_as_application_for_read_all_directory", "access_as_application_for_read" };
            var expectedErrorMessage = string.Format(CultureInfo.InvariantCulture, IDWebErrorMessage.MissingRoles, string.Join(",", acceptedRoles));
            var expectedStatusCode = (int)HttpStatusCode.Forbidden;

            var httpContext = HttpContextUtilities.CreateHttpContext(actualRoles);
            httpContext.ValidateAppRole(acceptedRoles);

            HttpResponse response = httpContext.Response;
            Assert.Equal(expectedStatusCode, response.StatusCode);
            Assert.Equal(expectedErrorMessage, GetBody(response));

            httpContext = HttpContextUtilities.CreateHttpContext(actualRoles);
            httpContext.ValidateAppRole(acceptedRoles);
            response = httpContext.Response;
            Assert.Equal(expectedStatusCode, response.StatusCode);
            Assert.Equal(expectedErrorMessage, GetBody(response));
        }

        [Fact]
        public void VerifyAppHasAnyAcceptedRole_MatchesAcceptedRoles_ExecutesSuccessfully()
        {
            var httpContext = HttpContextUtilities.CreateHttpContext(new[] { "acceptedRole1" });
            httpContext.ValidateAppRole("acceptedRole1");

            httpContext = HttpContextUtilities.CreateHttpContext(new[] { "acceptedRole1 acceptedRole2" });
            httpContext.ValidateAppRole("acceptedRole2");

            httpContext = HttpContextUtilities.CreateHttpContext(new[] { "acceptedRole2" });
            httpContext.ValidateAppRole("acceptedRole1", "acceptedRole2");

            httpContext = HttpContextUtilities.CreateHttpContext(new[] { "acceptedRole2 acceptedRole1" });
            httpContext.ValidateAppRole("acceptedRole1", "acceptedRole2");
        }

        private static string GetBody(HttpResponse response)
        {
            byte[] buffer = new byte[response.Body.Length];
            response.Body.Seek(0, System.IO.SeekOrigin.Begin);
            response.Body.Read(buffer, 0, buffer.Length);
            string body = Encoding.Default.GetString(buffer);
            return body;
        }
    }
}

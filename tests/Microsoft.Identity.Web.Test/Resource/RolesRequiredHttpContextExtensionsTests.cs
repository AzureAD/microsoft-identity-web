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

            Assert.Throws<UnauthorizedAccessException>(() => httpContext.ValidateAppRole(acceptedRoles));
            Assert.Equal(expectedStatusCode, httpContext.Response.StatusCode);
        }

        [Fact]
        public void VerifyAppHasAnyAcceptedRole_NoAcceptedRoles_ThrowsException()
        {
            var acceptedRoles = new[] { "access_as_application", "access_as_application_for_write" };
            var actualRoles = new[] { "access_as_application_for_read_all_directory", "access_as_application_for_read" };
            var expectedErrorMessage = string.Format(CultureInfo.InvariantCulture, IDWebErrorMessage.MissingRoles, string.Join(", ", acceptedRoles));
            var expectedStatusCode = (int)HttpStatusCode.Forbidden;

            var httpContext = HttpContextUtilities.CreateHttpContext(new string[] { }, actualRoles);
            Assert.Throws<UnauthorizedAccessException>(() => httpContext.ValidateAppRole(acceptedRoles));

            Assert.Equal(expectedStatusCode, httpContext.Response.StatusCode);
        }

        [Fact]
        public void VerifyAppHasAnyAcceptedRole_MatchesAcceptedRoles_ExecutesSuccessfully()
        {
            var httpContext = HttpContextUtilities.CreateHttpContext(new string[] { }, new[] { "acceptedRole1" });
            httpContext.ValidateAppRole("acceptedRole1");

            httpContext = HttpContextUtilities.CreateHttpContext(new string[] { }, new[] { "acceptedRole1 acceptedRole2" });
            httpContext.ValidateAppRole("acceptedRole2");

            httpContext = HttpContextUtilities.CreateHttpContext(new string[] { }, new[] { "acceptedRole2" });
            httpContext.ValidateAppRole("acceptedRole1", "acceptedRole2");

            httpContext = HttpContextUtilities.CreateHttpContext(new string[] { }, new[] { "acceptedRole2 acceptedRole1" });
            httpContext.ValidateAppRole("acceptedRole1", "acceptedRole2");
        }
    }
}

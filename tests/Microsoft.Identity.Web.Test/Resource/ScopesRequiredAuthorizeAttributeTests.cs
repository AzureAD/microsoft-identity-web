// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Identity.Web.Resource;
using Microsoft.Identity.Web.Test.Common.TestHelpers;
using Xunit;

namespace Microsoft.Identity.Web.Test.Resource
{
    [RequiredScope(RequiredScopesConfigurationKey="AzureAd:Scopes")]
    [RequiredScope(ScopeOne)]
    public class ScopesRequiredAuthorizeAttributeTests
    {
        public const string ScopeOne = "scope1";
        public const string ScopeTwo = "scope2";

        [Theory]
        [InlineData(ScopeOne)]
        [InlineData(ScopeTwo)]
        public void VerifyUserHasAnyAcceptedScope_OneScopeMatches(string requestedScope)
        {
            string[] expectedScopes = new string[] { ScopeOne, ScopeTwo };
            RequiredScopeFilter requiredScopeFilter = new RequiredScopeFilter(new string[] { requestedScope });
            var httpContext = HttpContextUtilities.CreateHttpContext(expectedScopes, new string[] { });

            requiredScopeFilter.OnAuthorization(CreateRequiredContext(httpContext));
            Assert.Equal(new string[] { requestedScope }, requiredScopeFilter._acceptedScopes);
            Assert.Equal(new string[] { requestedScope }, requiredScopeFilter._effectiveAcceptedScopes);
        }

        [Fact]
        public void VerifyUserHasAnyAcceptedScope()
        {
            string[] expectedScopes = new string[] { ScopeOne };
            RequiredScopeFilter requiredScopeFilter = new RequiredScopeFilter(expectedScopes);
            var httpContext = HttpContextUtilities.CreateHttpContext(expectedScopes, new string[] { });

            requiredScopeFilter.OnAuthorization(CreateRequiredContext(httpContext));
            Assert.Equal(expectedScopes, requiredScopeFilter._acceptedScopes);
            Assert.Equal(expectedScopes, requiredScopeFilter._effectiveAcceptedScopes);
        }

        [Fact]
        public void VerifyUserHasAnyAcceptedScope_WithMismatchScope_Throws()
        {
            string[] expectedScopes = new string[] { ScopeOne };
            RequiredScopeFilter requiredScopeFilter = new RequiredScopeFilter(expectedScopes);
            var httpContext = HttpContextUtilities.CreateHttpContext(new string[] { ScopeTwo }, new string[] { });

            string message = string.Format(
                        CultureInfo.InvariantCulture,
                        IDWebErrorMessage.MissingScopes,
                        string.Join(",", expectedScopes));
            var exc = Assert.Throws<UnauthorizedAccessException>(() => requiredScopeFilter.OnAuthorization(CreateRequiredContext(httpContext)));
            Assert.Equal(message, exc.Message);
        }

        [Fact]
        public void VerifyUserHasAnyAcceptedScope_WithNoUserContext_Throws()
        {
            string[] expectedScopes = new string[] { ScopeOne };
            RequiredScopeFilter requiredScopeFilter = new RequiredScopeFilter(expectedScopes);
            var httpContext = HttpContextUtilities.CreateHttpContext();

            var exc = Assert.Throws<UnauthorizedAccessException>(() => requiredScopeFilter.OnAuthorization(CreateRequiredContext(httpContext)));
            Assert.Equal(IDWebErrorMessage.UnauthenticatedUser, exc.Message);
        }

        [Fact]
        public void VerifyUserHasAnyAcceptedScope_RequiredScopesMissing_Throws()
        {
            RequiredScopeFilter requiredScopeFilter = new RequiredScopeFilter(null);
            var httpContext = HttpContextUtilities.CreateHttpContext(new string[] { }, new string[] { });

            Assert.Throws<ArgumentNullException>(() => requiredScopeFilter.OnAuthorization(CreateRequiredContext(httpContext)));
        }

        [Fact]
        public void VerifyUserHasAnyAcceptedScope_RequiredScopesEmptyString_Throws()
        {
            RequiredScopeFilter requiredScopeFilter = new RequiredScopeFilter(new string[] { });
            var httpContext = HttpContextUtilities.CreateHttpContext(new string[] { }, new string[] { });

            Assert.Throws<ArgumentNullException>(() => requiredScopeFilter.OnAuthorization(CreateRequiredContext(httpContext)));
        }

        private AuthorizationFilterContext CreateRequiredContext(HttpContext httpContext)
        {
            ActionContext actionContext = new ActionContext();
            actionContext.HttpContext = httpContext;
            actionContext.RouteData = new AspNetCore.Routing.RouteData();
            actionContext.ActionDescriptor = new ActionDescriptor();
            return new AuthorizationFilterContext(actionContext, new List<IFilterMetadata>());
        }
    }
}

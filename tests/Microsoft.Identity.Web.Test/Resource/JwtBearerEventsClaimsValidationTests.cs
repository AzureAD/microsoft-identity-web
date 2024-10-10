// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.Identity.Web.Test.Common.TestHelpers;
using Xunit;

namespace Microsoft.Identity.Web.Test.Resource
{
    public abstract class JwtBearerEventsClaimsValidationTests
    {
        protected HttpContext _httpContext;
        protected JwtBearerOptions _jwtOptions;
        protected JwtBearerEvents _jwtEvents;
        protected AuthenticationScheme _authScheme;
        protected TokenValidatedContext _tokenContext;

        protected JwtBearerEventsClaimsValidationTests()
        {
            _httpContext = CreateHttpContext();
            _jwtOptions = new JwtBearerOptions();
            _jwtEvents = CreateJwtBearerEvents();
            _authScheme = new AuthenticationScheme(JwtBearerDefaults.AuthenticationScheme, JwtBearerDefaults.AuthenticationScheme, typeof(JwtBearerHandler));
            _tokenContext = new TokenValidatedContext(_httpContext, _authScheme, _jwtOptions)
            {
                Principal = _httpContext.User,
            };
            _tokenContext.Success();
        }

        protected abstract HttpContext CreateHttpContext();

        protected virtual JwtBearerEvents CreateJwtBearerEvents()
        {
            var events = new JwtBearerEvents();
            MicrosoftIdentityWebApiAuthenticationBuilderExtensions.ChainOnTokenValidatedEventForClaimsValidation(events, JwtBearerDefaults.AuthenticationScheme);
            return events;
        }
    }

    public class TokenValidated_MissingScopesAndRoles : JwtBearerEventsClaimsValidationTests
    {
        protected override HttpContext CreateHttpContext()
        {
            return HttpContextUtilities.CreateHttpContext();
        }

        [Fact]
        public async Task TokenValidated_MissingScopesAndRoles_AuthenticationFailsAsync()
        {
            Assert.True(_tokenContext.Result.Succeeded);
            await _jwtEvents.TokenValidated(_tokenContext);
            Assert.False(_tokenContext.Result.Succeeded);
        }
    }

    public class TokenValidated_WithScopesAndRoles : JwtBearerEventsClaimsValidationTests
    {
        protected override HttpContext CreateHttpContext()
        {
            return HttpContextUtilities.CreateHttpContext(new[] { "scope" }, new[] { "role" });
        }

        [Fact]
        public async Task TokenValidated_WithScopesAndRoles_AuthenticationSucceedsAsync()
        {
            Assert.True(_tokenContext.Result.Succeeded);
            await _jwtEvents.TokenValidated(_tokenContext);
            Assert.True(_tokenContext.Result.Succeeded);
        }
    }
}

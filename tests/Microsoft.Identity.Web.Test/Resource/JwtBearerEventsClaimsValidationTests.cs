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

        protected abstract JwtBearerEvents CreateJwtBearerEvents();
    }

    public class TokenValidated_MissingScopesAndRoles : JwtBearerEventsClaimsValidationTests
    {
        protected override HttpContext CreateHttpContext()
        {
            return HttpContextUtilities.CreateHttpContext();
        }

        protected override JwtBearerEvents CreateJwtBearerEvents()
        {
            var events = new JwtBearerEvents();
            MicrosoftIdentityWebApiAuthenticationBuilderExtensions.ChainOnTokenValidatedEventForClaimsValidation(events, JwtBearerDefaults.AuthenticationScheme, false);
            return events;
        }

        [Fact]
        public async Task TokenValidated_MissingScopesAndRoles_AuthenticationFails()
        {
            Assert.True(_tokenContext.Result.Succeeded);
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _jwtEvents.TokenValidated(_tokenContext)).ConfigureAwait(false);
        }
    }

    public class TokenValidated_WithScopesAndRoles : JwtBearerEventsClaimsValidationTests
    {
        protected override HttpContext CreateHttpContext()
        {
            return HttpContextUtilities.CreateHttpContext(new[] { "scope" }, new[] { "role" });
        }

        protected override JwtBearerEvents CreateJwtBearerEvents()
        {
            var events = new JwtBearerEvents();
            MicrosoftIdentityWebApiAuthenticationBuilderExtensions.ChainOnTokenValidatedEventForClaimsValidation(events, JwtBearerDefaults.AuthenticationScheme, false);
            return events;
        }

        [Fact]
        public async Task TokenValidated_WithScopesAndRoles_AuthenticationSucceeds()
        {
            Assert.True(_tokenContext.Result.Succeeded);
            await _jwtEvents.TokenValidated(_tokenContext).ConfigureAwait(false);
            Assert.True(_tokenContext.Result.Succeeded);
        }
    }

    public class TokenValidated_WithACLAuthorization : JwtBearerEventsClaimsValidationTests
    {
        protected override HttpContext CreateHttpContext()
        {
            return HttpContextUtilities.CreateHttpContext();
        }

        protected override JwtBearerEvents CreateJwtBearerEvents()
        {
            var events = new JwtBearerEvents();
            MicrosoftIdentityWebApiAuthenticationBuilderExtensions.ChainOnTokenValidatedEventForClaimsValidation(events, JwtBearerDefaults.AuthenticationScheme, true);
            return events;
        }

        [Fact]
        public async Task TokenValidated_WithACLAuthorization_AuthenticationSucceeds()
        {
            Assert.True(_tokenContext.Result.Succeeded);
            await _jwtEvents.TokenValidated(_tokenContext).ConfigureAwait(false);
            Assert.True(_tokenContext.Result.Succeeded);
        }
    }
}

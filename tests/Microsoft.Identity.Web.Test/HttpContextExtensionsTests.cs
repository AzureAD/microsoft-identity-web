// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Microsoft.Identity.Web.Test
{
    public class HttpContextExtensionsTests
    {
        private const string WebApiTokenKeyName = "JwtSecurityTokenUsedToCallWebAPI";
        
        [Fact]
        public void StoreTokenUsedToCallWebAPI()
        {
            var httpContext = MakeHttpContext();
            var token = new JwtSecurityToken();

            httpContext.StoreTokenUsedToCallWebAPI(token);

            Assert.Same(token, httpContext.Items[WebApiTokenKeyName]);
        }

        [Fact]
        public void GetTokenUsedToCallWebAPI()
        {
            var httpContext = MakeHttpContext();
            var token = new JwtSecurityToken();

            Assert.Null(httpContext.GetTokenUsedToCallWebAPI());

            httpContext.StoreTokenUsedToCallWebAPI(token);

            Assert.Same(token, httpContext.GetTokenUsedToCallWebAPI());
        }

        private HttpContext MakeHttpContext()
        {
            var services = new ServiceCollection()
                .AddOptions()
                .AddHttpContextAccessor()
                .BuildServiceProvider();
            var contextFactory = new DefaultHttpContextFactory(services);

            return contextFactory.Create(new FeatureCollection());
        }
    }
}

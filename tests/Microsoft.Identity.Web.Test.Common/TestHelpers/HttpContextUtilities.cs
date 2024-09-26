// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.IO;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace Microsoft.Identity.Web.Test.Common.TestHelpers
{
    public static class HttpContextUtilities
    {
        public static HttpContext CreateHttpContext()
        {
            var services = new ServiceCollection()
                .AddOptions()
                .AddHttpContextAccessor()
                .BuildServiceProvider();
            var contextFactory = new DefaultHttpContextFactory(services);
            var featureCollection = new FeatureCollection();
            featureCollection.Set<IHttpResponseFeature>(new HttpResponseFeature());
            featureCollection.Set<IHttpRequestFeature>(new HttpRequestFeature());
            featureCollection.Set<IHttpResponseBodyFeature>(new StreamResponseBodyFeature(new MemoryStream()));

            HttpContext httpContext = contextFactory.Create(featureCollection);
            
            return httpContext;
        }

        public static HttpContext CreateHttpContext(
            string[] userScopes,
            string[] userRoles)
        {
            var httpContext = CreateHttpContext();

            httpContext.User = new ClaimsPrincipal(
                new CaseSensitiveClaimsIdentity(new Claim[]
                {
                    new Claim(ClaimConstants.Scope, string.Join(' ', userScopes)),
                    new Claim(ClaimConstants.Roles, string.Join(' ', userRoles)),
                }));

            return httpContext;
        }
    }
}

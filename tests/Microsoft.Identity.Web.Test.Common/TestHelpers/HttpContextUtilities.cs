// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.DependencyInjection;

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

            return contextFactory.Create(featureCollection);
        }

        public static HttpContext CreateHttpContext(string[] userScopes)
        {
            var httpContext = CreateHttpContext();

            httpContext.User = new ClaimsPrincipal(
                new ClaimsIdentity(new Claim[]
                {
                    new Claim(ClaimConstants.Scope, string.Join(' ', userScopes)),
                }));

            return httpContext;
        }
    }
}
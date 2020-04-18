// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.IdentityModel.Tokens.Jwt;
using Microsoft.Identity.Web.Test.Common.TestHelpers;
using Xunit;

namespace Microsoft.Identity.Web.Test
{
    public class HttpContextExtensionsTests
    {
        private const string WebApiTokenKeyName = "JwtSecurityTokenUsedToCallWebAPI";

        [Fact]
        public void StoreTokenUsedToCallWebAPI()
        {
            var httpContext = HttpContextUtilities.CreateHttpContext();
            var token = new JwtSecurityToken();

            httpContext.StoreTokenUsedToCallWebAPI(token);

            Assert.Same(token, httpContext.Items[WebApiTokenKeyName]);
        }

        [Fact]
        public void GetTokenUsedToCallWebAPI()
        {
            var httpContext = HttpContextUtilities.CreateHttpContext();
            var token = new JwtSecurityToken();

            Assert.Null(httpContext.GetTokenUsedToCallWebAPI());

            httpContext.StoreTokenUsedToCallWebAPI(token);

            Assert.Same(token, httpContext.GetTokenUsedToCallWebAPI());
        }
    }
}

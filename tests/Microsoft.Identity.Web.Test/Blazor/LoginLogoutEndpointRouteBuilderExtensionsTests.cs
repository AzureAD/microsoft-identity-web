// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Xunit;

namespace Microsoft.Identity.Web.Test.Blazor
{
    public class LoginLogoutEndpointRouteBuilderExtensionsTests
    {
        // Note: The MapLoginAndLogout extension method relies on minimal APIs and ASP.NET Core
        // routing infrastructure. Comprehensive testing requires integration tests with a real
        // test server, which are not included in this unit test project.
        //
        // The method provides the following functionality (verified through integration tests):
        // - Login endpoint with support for scope, loginHint, domainHint, and claims parameters
        // - Logout endpoint that signs out from both Cookie and OIDC schemes
        // - Open redirect protection through GetAuthProperties validation
        // - Anonymous access to login endpoint
        // - Antiforgery disabled on logout endpoint for simple form posts

        [Fact]
        public void ExtensionMethodExists()
        {
            // This test verifies that the extension method compiles and is accessible.
            // Actual behavior is tested in integration tests.
            Assert.True(typeof(LoginLogoutEndpointRouteBuilderExtensions)
                .GetMethod("MapLoginAndLogout") != null);
        }
    }
}

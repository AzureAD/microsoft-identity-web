// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Authorization;
using Xunit;

namespace Microsoft.Identity.Web.Test.Blazor
{
    public class LoginLogoutEndpointRouteBuilderExtensionsTests
    {
        // -----------------------------------------------------------------------------
        // F3 regression guards — GetAuthProperties open-redirect hardening.
        //
        // The ReturnUrl flowed from cross-origin form POSTs into
        // AuthenticationProperties.RedirectUri, which is followed as-is by the
        // ASP.NET Core sign-out handler. The previous implementation:
        //   - accepted "/\host" (framework IsLocalUrl rejects this)
        //   - silently coerced absolute URLs via `new Uri(...).PathAndQuery`
        //   - prepended '/' to bare strings like "evil.example"
        // These tests pin the hardened local-URL-only semantics. Anything not a
        // strictly-local path ("/" or "/path...") must be coerced to "/".
        // -----------------------------------------------------------------------------

        [Theory]
        // Local paths — preserved.
        [InlineData("/", "/")]
        [InlineData("/home", "/home")]
        [InlineData("/home?query=1", "/home?query=1")]
        [InlineData("/a/b/c", "/a/b/c")]
        // Null/empty — fall back to "/".
        [InlineData(null, "/")]
        [InlineData("", "/")]
        // Protocol-relative — blocked.
        [InlineData("//evil.example", "/")]
        [InlineData("//evil.example/path", "/")]
        // Slash-backslash bypass of naive validators — blocked.
        [InlineData("/\\evil.example", "/")]
        [InlineData("/\\\\evil.example", "/")]
        // Absolute URLs — blocked.
        [InlineData("https://evil.example/", "/")]
        [InlineData("http://evil.example/", "/")]
        [InlineData("javascript:alert(1)", "/")]
        // Bare hostnames / non-slash-prefixed — blocked.
        [InlineData("evil.example", "/")]
        [InlineData("home", "/")]
        public void GetAuthProperties_CoercesNonLocalReturnUrls(string? input, string expected)
        {
            var props = LoginLogoutEndpointRouteBuilderExtensions.GetAuthProperties(input);
            Assert.Equal(expected, props.RedirectUri);
        }

        [Fact]
        public void ExtensionMethodExists()
        {
            Assert.NotNull(typeof(LoginLogoutEndpointRouteBuilderExtensions)
                .GetMethod("MapLoginAndLogout"));
        }
    }
}

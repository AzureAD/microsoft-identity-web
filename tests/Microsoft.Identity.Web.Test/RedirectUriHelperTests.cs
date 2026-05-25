// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Xunit;

namespace Microsoft.Identity.Web.Test
{
    public class RedirectUriHelperTests
    {
        [Theory]
        // Local paths — accepted.
        [InlineData("/", true)]
        [InlineData("/home", true)]
        [InlineData("/home?query=1", true)]
        [InlineData("/a/b/c", true)]
        // Null/empty — rejected.
        [InlineData(null, false)]
        [InlineData("", false)]
        // Protocol-relative — rejected.
        [InlineData("//some.example", false)]
        [InlineData("//some.example/path", false)]
        // Slash-backslash — rejected.
        [InlineData("/\\some.example", false)]
        [InlineData("/\\\\some.example", false)]
        // Absolute URLs — rejected.
        [InlineData("https://some.example/", false)]
        [InlineData("http://some.example/", false)]
        [InlineData("javascript:alert(1)", false)]
        // Bare hostnames / non-slash-prefixed — rejected.
        [InlineData("some.example", false)]
        [InlineData("home", false)]
        // Percent-encoded slash/backslash — rejected (reverse proxies may decode these).
        [InlineData("/%2Fsome.example", false)]
        [InlineData("/%2fsome.example", false)]
        [InlineData("/%5Csome.example", false)]
        [InlineData("/%5csome.example", false)]
        [InlineData("/%2f%2fsome.example/x", false)]
        [InlineData("/%2F%5Csome.example", false)]
        public void IsLocalUrl_ValidatesCorrectly(string? input, bool expected)
        {
            Assert.Equal(expected, RedirectUriHelper.IsLocalUrl(input));
        }

        [Theory]
        [InlineData("/%2Fsome.example", true)]
        [InlineData("/%2fsome.example", true)]
        [InlineData("/%5Csome.example", true)]
        [InlineData("/%5csome.example", true)]
        [InlineData("/%2f%2fsome.example/x", true)]
        [InlineData("/%2F%5Csome.example", true)]
        [InlineData("/home", false)]
        [InlineData("/a/b/c", false)]
        [InlineData("/", false)]
        public void HasPercentEncodedSlashPrefix_DetectsEncodedSlashes(string input, bool expected)
        {
            Assert.Equal(expected, RedirectUriHelper.HasPercentEncodedSlashPrefix(input));
        }
    }
}
